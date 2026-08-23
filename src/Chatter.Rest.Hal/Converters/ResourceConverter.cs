using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

/// <summary>
/// JSON converter for serializing and deserializing HAL resources, separating state from _links and _embedded.
/// </summary>
public sealed class ResourceConverter : JsonConverter<Resource>
{
	/// <summary>
	/// Reads a Resource from JSON, parsing state, _links, and _embedded properties.
	/// </summary>
	/// <param name="reader">The JSON reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">Serializer options.</param>
	/// <returns>The deserialized Resource.</returns>
	/// <exception cref="JsonException">
	/// Thrown when the JSON is not a Resource Object, or when its <c>_links</c>/<c>_embedded</c> members
	/// are structurally invalid.
	/// </exception>
	public override Resource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = ConverterHelpers.ParseNode(ref reader);

		// A HAL Resource Object is a JSON object. Rejecting anything else here — rather than letting
		// the lazy creators index a primitive or an array later — keeps the failure at the
		// deserialization call instead of deferring an InvalidOperationException to property access.
		if (node is not JsonObject resourceObject)
		{
			throw new JsonException("A HAL Resource must be a JSON object.");
		}

		return ReadFromNode(resourceObject, options);
	}

	/// <summary>
	/// Materializes a Resource directly from an already-parsed node. Nested converters call this
	/// instead of <see cref="JsonNode.Deserialize"/>, which would re-serialize the subtree to UTF-8
	/// and re-parse it — repeating that once per ancestor turns a deeply nested <c>_embedded</c>
	/// chain into O(depth × size) work; walking the existing tree keeps the whole document at O(size).
	/// </summary>
	internal static Resource ReadFromNode(JsonObject resourceObject, JsonSerializerOptions options)
	{
		// The HAL reserved names are literal and case-sensitive, so they are matched ordinally
		// regardless of the caller's PropertyNameCaseInsensitive setting. This keeps the reserved
		// lookups consistent with the state stripping in jsonObjectCreator.
		// Both collections are materialized eagerly so a malformed _links/_embedded member fails
		// at the deserialization call, not on a later property read.
		var linksNode = ConverterHelpers.GetReservedProperty(resourceObject, ConverterHelpers.LinksProperty);
		var links = linksNode is null
			? null
			: ConverterHelpers.HasCustomConverter<LinkCollection>(options, typeof(LinkCollectionConverter))
				? linksNode.Deserialize<LinkCollection>(options)
				: LinkCollectionConverter.ReadFromNode(linksNode, options);
		var embeddedNode = ConverterHelpers.GetReservedProperty(resourceObject, ConverterHelpers.EmbeddedProperty);
		var embedded = embeddedNode is null
			? null
			: ConverterHelpers.HasCustomConverter<EmbeddedResourceCollection>(options, typeof(EmbeddedResourceCollectionConverter))
				? embeddedNode.Deserialize<EmbeddedResourceCollection>(options)
				: EmbeddedResourceCollectionConverter.ReadFromNode(embeddedNode, options);

		LinkCollection? linkCollectionCreator() => links;

		EmbeddedResourceCollection? embeddedCollectionCreator() => embedded;

		JsonObject? jsonObjectCreator()
		{
			var result = new JsonObject();
			foreach (var kvp in resourceObject)
			{
				if (ConverterHelpers.IsReservedProperty(kvp.Key)) continue;
#if NET8_0_OR_GREATER
				result.Add(kvp.Key, kvp.Value?.DeepClone());
#else
				result.Add(kvp.Key, kvp.Value?.Deserialize<JsonNode>());
#endif
			}
			return result;
		};

		return new Resource(resourceObject, jsonObjectCreator, linkCollectionCreator, embeddedCollectionCreator, options);
	}

	/// <summary>
	/// Writes a Resource to JSON, serializing state properties followed by _links and _embedded.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="value">The Resource to serialize.</param>
	/// <param name="options">Serializer options.</param>
	public override void Write(Utf8JsonWriter writer, Resource value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		var writesLinks = value.Links != null && value.Links.Count > 0;
		var writesEmbedded = value.Embedded != null && value.Embedded.Count > 0;

		if (value.CachedState != null)
		{
			var utf8Bytes = JsonSerializer.SerializeToUtf8Bytes(value.CachedState, options);
			using var doc = JsonDocument.Parse(utf8Bytes);
			foreach (var prop in doc.RootElement.EnumerateObject())
			{
				// HAL's reserved names are the literal strings "_links"/"_embedded"; a state property the
				// naming policy maps to "Links"/"Embedded" is ordinary user data and is always written.
				// A state property already carrying a reserved name is skipped only when the resource's
				// own collection is about to be written under that same name, because emitting both would
				// produce a duplicate JSON member, which RFC 8259 leaves undefined.
				if ((writesLinks && string.Equals(prop.Name, ConverterHelpers.LinksProperty, StringComparison.Ordinal))
					|| (writesEmbedded && string.Equals(prop.Name, ConverterHelpers.EmbeddedProperty, StringComparison.Ordinal)))
					continue;

				if (prop.Value.ValueKind != JsonValueKind.Null || options.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingNull)
				{
					prop.WriteTo(writer);
				}
			}
		}

		if (writesLinks)
		{
			writer.WritePropertyName(ConverterHelpers.LinksProperty);
			JsonSerializer.Serialize(writer, value.Links, options);
		}

		if (writesEmbedded)
		{
			writer.WritePropertyName(ConverterHelpers.EmbeddedProperty);
			JsonSerializer.Serialize(writer, value.Embedded, options);
		}

		writer.WriteEndObject();
	}
}
