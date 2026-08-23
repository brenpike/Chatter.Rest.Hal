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

		// The HAL reserved names are literal and case-sensitive, so they are matched ordinally
		// regardless of the caller's PropertyNameCaseInsensitive setting. This keeps the reserved
		// lookups consistent with the state stripping in jsonObjectCreator.
		// Both collections are materialized eagerly for the same reason as the check above: a
		// malformed _links/_embedded member must fail here, not on a later property read.
		var links = ConverterHelpers.GetReservedProperty(resourceObject, ConverterHelpers.LinksProperty)
			?.Deserialize<LinkCollection>(options);
		var embedded = ConverterHelpers.GetReservedProperty(resourceObject, ConverterHelpers.EmbeddedProperty)
			?.Deserialize<EmbeddedResourceCollection>(options);

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

		if (value.CachedState != null)
		{
			var linksName = options.PropertyNamingPolicy?.ConvertName(nameof(Resource.Links)) ?? nameof(Resource.Links);
			var embeddedName = options.PropertyNamingPolicy?.ConvertName(nameof(Resource.Embedded)) ?? nameof(Resource.Embedded);
			var utf8Bytes = JsonSerializer.SerializeToUtf8Bytes(value.CachedState, options);
			using var doc = JsonDocument.Parse(utf8Bytes);
			foreach (var prop in doc.RootElement.EnumerateObject())
			{
				if (prop.Name == linksName || prop.Name == embeddedName)
					continue;

				if (prop.Value.ValueKind != JsonValueKind.Null || options.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingNull)
				{
					prop.WriteTo(writer);
				}
			}
		}

		if (value.Links != null && value.Links.Count > 0)
		{
			writer.WritePropertyName("_links");
			JsonSerializer.Serialize(writer, value.Links, options);
		}

		if (value.Embedded != null && value.Embedded.Count > 0)
		{
			writer.WritePropertyName("_embedded");
			JsonSerializer.Serialize(writer, value.Embedded, options);
		}

		writer.WriteEndObject();
	}
}
