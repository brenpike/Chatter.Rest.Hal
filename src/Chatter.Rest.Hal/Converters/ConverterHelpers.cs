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
	/// Number of property-name hashes held on the stack before spilling to the heap.
	/// </summary>
	private const int InlinePropertyNameCapacity = 16;

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
	/// they are detected with an allocation-free pre-scan over a copy of the reader, so well-formed
	/// payloads take the ordinary lazy <see cref="JsonNode.Parse(ref Utf8JsonReader, JsonNodeOptions?)"/>
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
		switch (element.ValueKind)
		{
			case JsonValueKind.Object:
			{
				var obj = new JsonObject();
				foreach (var property in element.EnumerateObject())
				{
					// The indexer overwrites rather than throwing, which is what makes duplicate
					// property names resolve last-wins instead of deferring an ArgumentException.
					obj[property.Name] = ToNode(property.Value);
				}
				return obj;
			}
			case JsonValueKind.Array:
			{
				var array = new JsonArray();
				foreach (var item in element.EnumerateArray())
				{
					array.Add(ToNode(item));
				}
				return array;
			}
			case JsonValueKind.Null:
			case JsonValueKind.Undefined:
				return null;
			default:
				// element already belongs to the detached document, so it needs no further cloning.
				return JsonValue.Create(element);
		}
	}

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
		switch (reader.TokenType)
		{
			case JsonTokenType.StartObject:
				return ObjectContainsDuplicatePropertyNames(ref reader);
			case JsonTokenType.StartArray:
				while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
				{
					if (ContainsDuplicatePropertyNames(ref reader)) return true;
				}
				return false;
			default:
				return false;
		}
	}

	private static bool ObjectContainsDuplicatePropertyNames(ref Utf8JsonReader reader)
	{
		Span<long> inlineHashes = stackalloc long[InlinePropertyNameCapacity];
		var count = 0;
		List<long>? overflowHashes = null;

		while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
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

			var hash = Fnv1a64(name);
			var limit = count < InlinePropertyNameCapacity ? count : InlinePropertyNameCapacity;
			for (var i = 0; i < limit; i++)
			{
				if (inlineHashes[i] == hash) return true;
			}

			if (overflowHashes != null)
			{
				for (var i = 0; i < overflowHashes.Count; i++)
				{
					if (overflowHashes[i] == hash) return true;
				}
			}

			if (count < InlinePropertyNameCapacity)
			{
				inlineHashes[count] = hash;
			}
			else
			{
				(overflowHashes ??= new List<long>()).Add(hash);
			}

			count++;

			reader.Read();
			if (ContainsDuplicatePropertyNames(ref reader)) return true;
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
