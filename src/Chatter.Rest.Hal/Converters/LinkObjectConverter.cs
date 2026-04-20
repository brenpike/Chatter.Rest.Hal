using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

/// <summary>
/// JSON converter for serializing and deserializing HAL link objects with href, templated, type, and other properties.
/// </summary>
public class LinkObjectConverter : JsonConverter<LinkObject>
{
	/// <summary>
	/// Reads a LinkObject from JSON, validating required href and parsing optional properties.
	/// </summary>
	/// <param name="reader">The JSON reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">Serializer options.</param>
	/// <returns>The deserialized LinkObject, or null if href is missing or invalid.</returns>
	public override LinkObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true });

        if (node == null)
        {
            return null;
        }

        if (node[nameof(LinkObject.Href)] is null)
        {
            // Href is required for a valid LinkObject. Be tolerant and return null for malformed input.
            return null;
        }

        var href = node[nameof(LinkObject.Href)]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        return new LinkObject(href)
        {
            Templated = TryGetBooleanAsTrue(node[nameof(LinkObject.Templated)]),
            Type = TryGetString(node[nameof(LinkObject.Type)]),
            Deprecation = TryGetString(node[nameof(LinkObject.Deprecation)]),
            Name = TryGetString(node[nameof(LinkObject.Name)]),
            Title = TryGetString(node[nameof(LinkObject.Title)]),
            Profile = TryGetString(node[nameof(LinkObject.Profile)]),
            Hreflang = TryGetString(node[nameof(LinkObject.Hreflang)])
        };
    }

	/// <summary>
	/// Attempts to parse a boolean value from a JSON node, returning true only for explicit boolean true values.
	/// </summary>
	/// <param name="node">The JSON node to parse.</param>
	/// <returns>true if the value is boolean true; otherwise, null.</returns>
	private static bool? TryGetBooleanAsTrue(JsonNode? node)
    {
        if (node == null)
        {
            return null;
        }

        // Per HAL spec: templated should only be true if the value is boolean true
        // Any other value (including boolean false, strings, numbers, objects, arrays) should be treated as false (null)
        try
        {
            var value = node.GetValue<bool>();
            return value ? true : null;
        }
        catch
        {
            // Not a valid boolean - treat as false (null)
            return null;
        }
    }

	/// <summary>
	/// Attempts to parse a string value from a JSON node, rejecting non-string types.
	/// </summary>
	/// <param name="node">The JSON node to parse.</param>
	/// <returns>The string value, or null if the node is not a valid string.</returns>
	private static string? TryGetString(JsonNode? node)
    {
        if (node == null)
        {
            return null;
        }

        // Only accept actual string values; reject numbers, booleans, objects, arrays
        try
        {
            return node.GetValue<string>();
        }
        catch
        {
            // Not a valid string - return null
            return null;
        }
    }

	/// <summary>
	/// Writes a LinkObject to JSON, serializing all non-null properties.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="linkObject">The LinkObject to serialize.</param>
	/// <param name="options">Serializer options.</param>
	public override void Write(Utf8JsonWriter writer, LinkObject linkObject, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrWhiteSpace(linkObject.Href))
        {
            writer.WritePropertyName(nameof(linkObject.Href).ToLower()); //TODO: make this respect json options for casing, etc.
            writer.WriteStringValue(linkObject.Href);
        }

        if (linkObject.Templated.HasValue)
        {
            writer.WritePropertyName(nameof(linkObject.Templated).ToLower());
            writer.WriteBooleanValue(linkObject.Templated.Value);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Type))
        {
            writer.WritePropertyName(nameof(linkObject.Type).ToLower());
            writer.WriteStringValue(linkObject.Type);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Deprecation))
        {
            writer.WritePropertyName(nameof(linkObject.Deprecation).ToLower());
            writer.WriteStringValue(linkObject.Deprecation);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Name))
        {
            writer.WritePropertyName(nameof(linkObject.Name).ToLower());
            writer.WriteStringValue(linkObject.Name);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Title))
        {
            writer.WritePropertyName(nameof(linkObject.Title).ToLower());
            writer.WriteStringValue(linkObject.Title);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Profile))
        {
            writer.WritePropertyName(nameof(linkObject.Profile).ToLower());
            writer.WriteStringValue(linkObject.Profile);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Hreflang))
        {
            writer.WritePropertyName(nameof(linkObject.Hreflang).ToLower());
            writer.WriteStringValue(linkObject.Hreflang);
        }

        writer.WriteEndObject();
    }
}
