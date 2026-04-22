using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal;

namespace Chatter.Rest.Hal.Converters;

/// <summary>
/// JSON converter for serializing and deserializing HAL links with their link objects.
/// </summary>
public class LinkConverter : JsonConverter<Link>
{
	private readonly HalJsonOptions? _halJsonOptions;

	/// <summary>
	/// Initializes a new instance with no explicit options; uses <see cref="HalJsonOptions.Default"/> at write time.
	/// </summary>
	public LinkConverter() { }

	/// <summary>
	/// Initializes a new instance with the specified <see cref="HalJsonOptions"/>.
	/// </summary>
	/// <param name="options">The HAL JSON options to use during serialization.</param>
	public LinkConverter(HalJsonOptions options) => _halJsonOptions = options;

	/// <summary>
	/// Reads a Link from JSON, handling single objects, arrays, and string shorthands.
	/// </summary>
	/// <param name="reader">The JSON reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">Serializer options.</param>
	/// <returns>The deserialized Link, or null if the JSON is malformed.</returns>
	public override Link? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true });

        // Only accept a single-property object representing a HAL link entry (e.g. { "rel": { ... } })
        if (node is not JsonObject jsonObject)
        {
            // Not an object we can interpret as a Link  be tolerant and return null
            return null;
        }

        // A standalone Link JSON should be an object with exactly one property (the rel)
        if (jsonObject.Count != 1)
        {
            return null;
        }

        var kvp = jsonObject.First();

        // If rel is missing or invalid, return null rather than throwing to be tolerant of malformed input.
        if (string.IsNullOrWhiteSpace(kvp.Key))
        {
            return null;
        }

        var rel = kvp.Key;
        Link link = new Link(rel);

        // If value is null (e.g. "rel": null) create an empty Link with no LinkObjects.
        if (kvp.Value == null || IsJsonNull(kvp.Value))
        {
            return link;
        }

        // If the value is an object, ensure it contains an href (required by HAL) before deserializing.
        if (kvp.Value is JsonObject obj)
        {
            if (obj["href"] == null || IsJsonNull(obj["href"]))
            {
                // Not a valid Link Object shape
                return null;
            }

            var lo = obj.Deserialize<LinkObject>(options);
            if (lo != null) link.LinkObjects.Add(lo);
            return link;
        }

        // If the value is an array, ensure it is an array of Link Objects (each must have href)
        if (kvp.Value is JsonArray ja)
        {
            foreach (var item in ja)
            {
                if (item is not JsonObject itemObj)
                {
                    return null;
                }
                if (itemObj["href"] == null || IsJsonNull(itemObj["href"]))
                {
                    return null;
                }
            }

            var loc = ja.Deserialize<LinkObjectCollection>(options);
            if (loc != null) link.LinkObjects = loc;
            link.IsArray = true;
            return link;
        }

        // If the value is a primitive (commonly a string shorthand), treat it as an href value
        if (kvp.Value is JsonValue jv)
        {
            try
            {
                var href = jv.Deserialize<string?>();
                if (!string.IsNullOrWhiteSpace(href))
                {
                    link.LinkObjects.Add(new LinkObject(href));
                    return link;
                }
            }
            catch
            {
                // fallthrough to return null
            }
        }

        // Not a recognized HAL link shape be conservative and return null
        return null;
    }

	/// <summary>
	/// Writes a Link to JSON as an object with the relation as the property name.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="value">The Link to serialize.</param>
	/// <param name="options">Serializer options.</param>
	public override void Write(Utf8JsonWriter writer, Link value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(value.Rel);
		bool forceArray = (_halJsonOptions ?? HalJsonOptions.Default).AlwaysUseArrayForLinks || value.IsArray;
		if (!forceArray && value.LinkObjects.Count == 1)
		{
			JsonSerializer.Serialize(writer, value.LinkObjects[0], options);
		}
		else
		{
			writer.WriteStartArray();
			foreach (var lo in value.LinkObjects)
			{
				JsonSerializer.Serialize(writer, lo, options);
			}
			writer.WriteEndArray();
		}
		writer.WriteEndObject();
	}

	private static bool IsJsonNull(JsonNode? node)
	{
		if (node is not JsonValue jv) return false;
#if NET8_0_OR_GREATER
		return jv.GetValueKind() == JsonValueKind.Null;
#else
		return jv.ToJsonString() == "null";
#endif
	}
}
