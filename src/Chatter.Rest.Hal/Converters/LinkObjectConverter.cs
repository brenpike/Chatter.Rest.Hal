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
	private static readonly JsonEncodedText HrefProperty = JsonEncodedText.Encode("href");
	private static readonly JsonEncodedText TemplatedProperty = JsonEncodedText.Encode("templated");
	private static readonly JsonEncodedText TypeProperty = JsonEncodedText.Encode("type");
	private static readonly JsonEncodedText DeprecationProperty = JsonEncodedText.Encode("deprecation");
	private static readonly JsonEncodedText NameProperty = JsonEncodedText.Encode("name");
	private static readonly JsonEncodedText TitleProperty = JsonEncodedText.Encode("title");
	private static readonly JsonEncodedText ProfileProperty = JsonEncodedText.Encode("profile");
	private static readonly JsonEncodedText HreflangProperty = JsonEncodedText.Encode("hreflang");

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

        var hrefNode = node["href"];
        if (hrefNode is null)
        {
            // Href is required for a valid LinkObject. Be tolerant and return null for malformed input.
            return null;
        }

        var href = hrefNode.GetValue<string>();
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        return new LinkObject(href)
        {
            Templated = TryGetBooleanAsTrue(node["templated"]),
            Type = TryGetString(node["type"]),
            Deprecation = TryGetString(node["deprecation"]),
            Name = TryGetString(node["name"]),
            Title = TryGetString(node["title"]),
            Profile = TryGetString(node["profile"]),
            Hreflang = TryGetString(node["hreflang"])
        };
    }

	/// <summary>
	/// Attempts to parse a boolean value from a JSON node, returning true only for explicit boolean true values.
	/// </summary>
	/// <param name="node">The JSON node to parse.</param>
	/// <returns>true if the value is boolean true; otherwise, null.</returns>
	private static bool? TryGetBooleanAsTrue(JsonNode? node)
    {
        // Per HAL spec: templated should only be true if the value is boolean true
        // Any other value (including boolean false, strings, numbers, objects, arrays) should be treated as false (null)
        if (node is JsonValue jv && jv.TryGetValue<bool>(out var value))
            return value ? true : null;
        return null;
    }

	/// <summary>
	/// Attempts to parse a string value from a JSON node, rejecting non-string types.
	/// </summary>
	/// <param name="node">The JSON node to parse.</param>
	/// <returns>The string value, or null if the node is not a valid string.</returns>
	private static string? TryGetString(JsonNode? node)
    {
        // Only accept actual string values; reject numbers, booleans, objects, arrays
        if (node is JsonValue jv && jv.TryGetValue<string>(out var value))
            return value;
        return null;
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
            writer.WritePropertyName(HrefProperty);
            writer.WriteStringValue(linkObject.Href);
        }

        if (linkObject.Templated.HasValue)
        {
            writer.WritePropertyName(TemplatedProperty);
            writer.WriteBooleanValue(linkObject.Templated.Value);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Type))
        {
            writer.WritePropertyName(TypeProperty);
            writer.WriteStringValue(linkObject.Type);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Deprecation))
        {
            writer.WritePropertyName(DeprecationProperty);
            writer.WriteStringValue(linkObject.Deprecation);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Name))
        {
            writer.WritePropertyName(NameProperty);
            writer.WriteStringValue(linkObject.Name);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Title))
        {
            writer.WritePropertyName(TitleProperty);
            writer.WriteStringValue(linkObject.Title);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Profile))
        {
            writer.WritePropertyName(ProfileProperty);
            writer.WriteStringValue(linkObject.Profile);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Hreflang))
        {
            writer.WritePropertyName(HreflangProperty);
            writer.WriteStringValue(linkObject.Hreflang);
        }

        writer.WriteEndObject();
    }
}
