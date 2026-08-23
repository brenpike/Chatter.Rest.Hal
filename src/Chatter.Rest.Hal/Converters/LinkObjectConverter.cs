using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

/// <summary>
/// JSON converter for serializing and deserializing HAL link objects with href, templated, type, and other properties.
/// </summary>
public sealed class LinkObjectConverter : JsonConverter<LinkObject>
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
	/// <exception cref="JsonException">
	/// Thrown when the JSON is not a Link Object, or when its <c>href</c> property is present but not a string.
	/// </exception>
	public override LinkObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = ConverterHelpers.ParseNode(ref reader);

		if (node == null)
		{
			return null;
		}

		if (node is not JsonObject linkObjectNode)
		{
			throw new JsonException("A HAL Link Object must be a JSON object.");
		}

		// Link Object attribute names are ordinary property names, so they follow the caller's
		// PropertyNameCaseInsensitive setting. The node tree itself is always ordinal, so a document
		// carrying both "href" and "HREF" keeps them distinct and the exact name still wins.
		var hrefNode = ConverterHelpers.GetProperty(linkObjectNode, "href", options);
		if (hrefNode is null)
		{
			// Href is required for a valid LinkObject. Be tolerant and return null for malformed input.
			return null;
		}

		if (hrefNode is not JsonValue hrefValue || !hrefValue.TryGetValue<string>(out var href))
		{
			throw new JsonException("The 'href' property of a HAL Link Object must be a string.");
		}

		if (string.IsNullOrWhiteSpace(href))
		{
			return null;
		}

		return new LinkObject(href)
		{
			Templated = TryGetBooleanAsTrue(ConverterHelpers.GetProperty(linkObjectNode, "templated", options)),
			Type = TryGetString(ConverterHelpers.GetProperty(linkObjectNode, "type", options)),
			Deprecation = TryGetString(ConverterHelpers.GetProperty(linkObjectNode, "deprecation", options)),
			Name = TryGetString(ConverterHelpers.GetProperty(linkObjectNode, "name", options)),
			Title = TryGetString(ConverterHelpers.GetProperty(linkObjectNode, "title", options)),
			Profile = TryGetString(ConverterHelpers.GetProperty(linkObjectNode, "profile", options)),
			Hreflang = TryGetString(ConverterHelpers.GetProperty(linkObjectNode, "hreflang", options))
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
