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
	public override Resource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true })!;

		LinkCollection? linkCollectionCreator()
			=> node?["_links"]?.Deserialize<LinkCollection>(options);

		EmbeddedResourceCollection? embeddedCollectionCreator()
			=> node?["_embedded"]?.Deserialize<EmbeddedResourceCollection>(options);

		JsonObject? jsonObjectCreator()
		{
			if (node is not JsonObject sourceObj) return null;
			var result = new JsonObject();
			foreach (var kvp in sourceObj)
			{
				if (kvp.Key == "_links" || kvp.Key == "_embedded") continue;
#if NET8_0_OR_GREATER
				result.Add(kvp.Key, kvp.Value?.DeepClone());
#else
				result.Add(kvp.Key, kvp.Value?.Deserialize<JsonNode>());
#endif
			}
			return result;
		};

		return new Resource(node, jsonObjectCreator, linkCollectionCreator, embeddedCollectionCreator, options);
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
