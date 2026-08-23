using System;
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
	/// Builds the <see cref="JsonNodeOptions"/> used when parsing HAL JSON into a node tree,
	/// honoring the caller's <see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/> setting.
	/// </summary>
	/// <remarks>
	/// Converters must never force case-insensitive matching: the HAL reserved names are literal
	/// and case-sensitive, and forcing insensitivity collides legal state properties that differ
	/// only by case (for example <c>{"Name":"a","name":"b"}</c>).
	/// </remarks>
	internal static JsonNodeOptions NodeOptions(JsonSerializerOptions? options)
		=> new() { PropertyNameCaseInsensitive = options?.PropertyNameCaseInsensitive ?? false };

	/// <summary>
	/// Parses the JSON at the current reader position into a node tree.
	/// </summary>
	/// <remarks>
	/// Two guarantees the raw <see cref="JsonNode.Parse(ref Utf8JsonReader, JsonNodeOptions?)"/>
	/// does not provide:
	/// <list type="bullet">
	/// <item>
	/// Duplicate property names are normalized last-wins, matching the behavior most JSON parsers
	/// exhibit for a construct RFC 8259 leaves undefined. <see cref="JsonNode"/> otherwise defers a
	/// duplicate-key <see cref="ArgumentException"/> to whenever the object is first materialized —
	/// often at property-access time, long after deserialization returned.
	/// </item>
	/// <item>
	/// Malformed JSON surfaces as <see cref="JsonException"/> from
	/// <see cref="JsonDocument.ParseValue(ref Utf8JsonReader)"/>.
	/// </item>
	/// </list>
	/// </remarks>
	/// <param name="reader">The reader positioned at the value to parse.</param>
	/// <param name="options">The caller's serializer options.</param>
	/// <returns>The parsed node, or <c>null</c> when the value is JSON null.</returns>
	/// <exception cref="JsonException">Thrown when the JSON is malformed or exceeds the configured maximum depth.</exception>
	internal static JsonNode? ParseNode(ref Utf8JsonReader reader, JsonSerializerOptions? options)
	{
		using var document = JsonDocument.ParseValue(ref reader);
		return ToNode(document.RootElement, NodeOptions(options));
	}

	private static JsonNode? ToNode(JsonElement element, JsonNodeOptions nodeOptions)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.Object:
			{
				var obj = new JsonObject(nodeOptions);
				foreach (var property in element.EnumerateObject())
				{
					// The indexer overwrites rather than throwing, which is what makes duplicate
					// property names resolve last-wins instead of deferring an ArgumentException.
					obj[property.Name] = ToNode(property.Value, nodeOptions);
				}
				return obj;
			}
			case JsonValueKind.Array:
			{
				var array = new JsonArray(nodeOptions);
				foreach (var item in element.EnumerateArray())
				{
					array.Add(ToNode(item, nodeOptions));
				}
				return array;
			}
			case JsonValueKind.Null:
			case JsonValueKind.Undefined:
				return null;
			default:
				// Clone detaches the value from the JsonDocument, which is disposed by ParseNode.
				return JsonValue.Create(element.Clone(), nodeOptions);
		}
	}

	/// <summary>
	/// Reads a HAL reserved property (<c>_links</c>/<c>_embedded</c>) using a literal, case-sensitive
	/// match regardless of the node tree's configured property-name comparer.
	/// </summary>
	/// <param name="obj">The object to read from.</param>
	/// <param name="name">The reserved property name.</param>
	/// <returns>The reserved property value, or <c>null</c> when the property is absent or JSON null.</returns>
	internal static JsonNode? GetReservedProperty(JsonObject obj, string name)
	{
		foreach (var kvp in obj)
		{
			if (string.Equals(kvp.Key, name, StringComparison.Ordinal))
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
