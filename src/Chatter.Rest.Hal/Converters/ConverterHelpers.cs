using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Chatter.Rest.Hal.Converters;

internal static class ConverterHelpers
{
	/// <summary>
	/// The HAL reserved property name for a resource's link collection.
	/// </summary>
	/// <remarks>
	/// Reserved names are literal and case-sensitive per
	/// <see href="https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-4.1"/>.
	/// </remarks>
	internal const string LinksProperty = "_links";

	/// <summary>
	/// The HAL reserved property name for a resource's embedded resource collection.
	/// </summary>
	/// <inheritdoc cref="LinksProperty"/>
	internal const string EmbeddedProperty = "_embedded";

	/// <summary>
	/// True when the caller registered a custom converter for <typeparamref name="T"/> in
	/// <see cref="JsonSerializerOptions.Converters"/>. Options-registered converters take precedence
	/// over attribute-wired ones, so the node-walking fast path is only valid when the selected
	/// converter is the built-in one; otherwise the nested part must dispatch through
	/// <see cref="JsonNode.Deserialize"/> so the custom converter runs.
	/// </summary>
	internal static bool HasCustomConverter<T>(JsonSerializerOptions options, Type builtInConverterType)
		=> options.GetConverter(typeof(T))?.GetType() != builtInConverterType;

	internal static bool IsJsonNull(JsonNode? node)
	{
		if (node is not JsonValue jv) return false;
#if NET8_0_OR_GREATER
		return jv.GetValueKind() == JsonValueKind.Null;
#else
		return jv.ToJsonString() == "null";
#endif
	}

	/// <summary>
	/// Parses the JSON at the current reader position into a node tree whose property names are
	/// compared ordinally, normalizing duplicate property names last-wins.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The tree is always built with an ordinal comparer, never a case-insensitive one. HAL's reserved
	/// names are literal, and a case-insensitive DOM would merge distinct JSON members — both
	/// <c>{"_LINKS":{…},"_links":{…}}</c> and legal state such as <c>{"Name":"a","name":"b"}</c> would
	/// silently lose a member. Case-insensitive matching, when the caller asks for it, is applied at
	/// lookup time by <see cref="GetProperty"/> and only to ordinary (non-reserved) property names.
	/// </para>
	/// <para>
	/// Duplicate property names are normalized last-wins, matching the behavior most JSON parsers
	/// exhibit for a construct RFC 8259 leaves undefined. <see cref="JsonNode"/> otherwise defers a
	/// duplicate-key <see cref="ArgumentException"/> to whenever the object is first materialized —
	/// often at property-access time, long after deserialization returned. Because duplicates are rare,
	/// they are detected with an iterative pre-scan over a copy of the reader (allocations bounded by
	/// nesting depth, native stack usage constant), so well-formed payloads take the ordinary lazy
	/// <see cref="JsonNode.Parse(ref Utf8JsonReader, JsonNodeOptions?)"/>
	/// path and only a payload that actually contains duplicates pays for a rebuild.
	/// </para>
	/// </remarks>
	/// <param name="reader">The reader positioned at the value to parse.</param>
	/// <returns>The parsed node, or <c>null</c> when the value is JSON null.</returns>
	/// <exception cref="JsonException">Thrown when the JSON is malformed or exceeds the configured maximum depth.</exception>
	internal static JsonNode? ParseNode(ref Utf8JsonReader reader)
	{
		// Utf8JsonReader is a struct, so the pre-scan runs on a copy and leaves the caller's reader
		// positioned exactly where it was.
		var probe = reader;
		if (!ContainsDuplicatePropertyNames(ref probe))
		{
			return JsonNode.Parse(ref reader);
		}

		// Detach the parsed document once, up front, so the rebuilt tree can reference its elements
		// directly. Cloning per scalar instead would allocate a separate backing document for every
		// value in the payload.
		JsonElement detached;
		using (var document = JsonDocument.ParseValue(ref reader))
		{
			detached = document.RootElement.Clone();
		}

		return ToNode(detached);
	}

	private static JsonNode? ToNode(JsonElement element)
	{
		// Iterative rebuild: recursing once per nesting level would tie the accepted depth to the
		// native stack instead of the reader's MaxDepth, and a caller who raises MaxDepth could
		// then hit an uncatchable StackOverflowException on otherwise permitted input.
		if (element.ValueKind is not JsonValueKind.Object and not JsonValueKind.Array)
		{
			return LeafToNode(element);
		}

		var root = CreateShell(element.ValueKind);
		var work = new Stack<(JsonElement Source, JsonNode Target)>();
		work.Push((element, root));

		while (work.Count > 0)
		{
			var (source, target) = work.Pop();
			if (source.ValueKind == JsonValueKind.Object)
			{
				var obj = (JsonObject)target;
				foreach (var property in source.EnumerateObject())
				{
					// The indexer overwrites rather than throwing, which is what makes duplicate
					// property names resolve last-wins instead of deferring an ArgumentException.
					// A shell replaced by a later duplicate is filled and discarded, which wastes
					// a little work on hostile input but never changes the resulting tree.
					if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
					{
						var shell = CreateShell(property.Value.ValueKind);
						obj[property.Name] = shell;
						work.Push((property.Value, shell));
					}
					else
					{
						obj[property.Name] = LeafToNode(property.Value);
					}
				}
			}
			else
			{
				var array = (JsonArray)target;
				foreach (var item in source.EnumerateArray())
				{
					if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
					{
						var shell = CreateShell(item.ValueKind);
						array.Add(shell);
						work.Push((item, shell));
					}
					else
					{
						array.Add(LeafToNode(item));
					}
				}
			}
		}

		return root;
	}

	private static JsonNode CreateShell(JsonValueKind kind)
		=> kind == JsonValueKind.Object ? new JsonObject() : new JsonArray();

	private static JsonNode? LeafToNode(JsonElement element)
		=> element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
			? null
			// element already belongs to the detached document, so it needs no further cloning.
			: JsonValue.Create(element);

	/// <summary>
	/// Scans a value for duplicate property names without allocating a node tree.
	/// </summary>
	/// <remarks>
	/// Deliberately conservative: it may report a duplicate that is not one (on a 64-bit hash
	/// collision, an escaped property name, or a name split across buffer segments). A false positive
	/// only selects the rebuild path, which produces the same tree for input without duplicates, so
	/// the result stays correct either way.
	/// </remarks>
	private static bool ContainsDuplicatePropertyNames(ref Utf8JsonReader reader)
	{
		if (reader.TokenType is not JsonTokenType.StartObject and not JsonTokenType.StartArray)
		{
			return false;
		}

		// Iterative scan: recursing once per nesting level (with a stackalloc per object frame)
		// would let a caller who raises JsonSerializerOptions.MaxDepth push otherwise permitted
		// duplicate-free input into an uncatchable StackOverflowException. Depth is instead
		// tracked on the heap; the reader itself keeps enforcing MaxDepth.
		// One HashSet per open-object depth, reused across siblings at that depth, keeps the scan
		// ~linear in document size with allocations bounded by nesting depth. The token type of
		// each End token already says which container closed, so plain counters suffice.
		var nameSets = new List<HashSet<long>>();
		var openContainers = 0;
		var openObjects = 0;

		void OpenObject()
		{
			if (nameSets.Count == openObjects) nameSets.Add(new HashSet<long>());
			else nameSets[openObjects].Clear();
			openObjects++;
		}

		openContainers = 1;
		if (reader.TokenType == JsonTokenType.StartObject) OpenObject();

		while (openContainers > 0 && reader.Read())
		{
			switch (reader.TokenType)
			{
				case JsonTokenType.StartObject:
					openContainers++;
					OpenObject();
					break;
				case JsonTokenType.StartArray:
					openContainers++;
					break;
				case JsonTokenType.EndObject:
					openContainers--;
					openObjects--;
					break;
				case JsonTokenType.EndArray:
					openContainers--;
					break;
				case JsonTokenType.PropertyName:
				{
					if (reader.HasValueSequence)
					{
						// The name is split across buffer segments, so ValueSpan is unusable here.
						return true;
					}

					var name = reader.ValueSpan;
					if (name.IndexOf(JsonEscape) >= 0)
					{
						// Escaped names would have to be unescaped before they could be compared.
						return true;
					}

					if (!nameSets[openObjects - 1].Add(Fnv1a64(name)))
					{
						return true;
					}

					break;
				}
			}
		}

		return false;
	}

	private const byte JsonEscape = (byte)'\\';

	private static long Fnv1a64(ReadOnlySpan<byte> data)
	{
		unchecked
		{
			var hash = (long)0xcbf29ce484222325;
			for (var i = 0; i < data.Length; i++)
			{
				hash ^= data[i];
				hash *= 0x100000001b3;
			}
			return hash;
		}
	}

	/// <summary>
	/// Reads a HAL reserved property (<c>_links</c>/<c>_embedded</c>) using a literal, case-sensitive
	/// match, as required by the HAL specification.
	/// </summary>
	/// <param name="obj">The object to read from.</param>
	/// <param name="name">The reserved property name.</param>
	/// <returns>The reserved property value, or <c>null</c> when the property is absent or JSON null.</returns>
	internal static JsonNode? GetReservedProperty(JsonObject obj, string name)
		=> obj.TryGetPropertyValue(name, out var value) ? value : null;

	/// <summary>
	/// Reads an ordinary (non-reserved) property, falling back to a case-insensitive match only when
	/// the caller set <see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/>.
	/// </summary>
	/// <param name="obj">The object to read from.</param>
	/// <param name="name">The property name.</param>
	/// <param name="options">The caller's serializer options.</param>
	/// <returns>The property value, or <c>null</c> when the property is absent or JSON null.</returns>
	internal static JsonNode? GetProperty(JsonObject obj, string name, JsonSerializerOptions? options)
	{
		if (obj.TryGetPropertyValue(name, out var value))
		{
			return value;
		}

		if (options?.PropertyNameCaseInsensitive != true)
		{
			return null;
		}

		foreach (var kvp in obj)
		{
			if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase))
			{
				return kvp.Value;
			}
		}

		return null;
	}

	/// <summary>
	/// Determines whether a property name is one of the HAL reserved names, using a literal,
	/// case-sensitive comparison.
	/// </summary>
	internal static bool IsReservedProperty(string name)
		=> string.Equals(name, LinksProperty, StringComparison.Ordinal)
		|| string.Equals(name, EmbeddedProperty, StringComparison.Ordinal);
}
