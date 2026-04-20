using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

/// <summary>
/// JSON converter for serializing and deserializing HAL resources, separating state from _links and _embedded.
/// </summary>
public class ResourceConverter : JsonConverter<Resource>
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
			var cloneObject = node?.Deserialize<JsonNode>()?.AsObject();
			cloneObject?.Remove("_links");
			cloneObject?.Remove("_embedded");
			return cloneObject;
		};

		return new Resource(node, jsonObjectCreator, linkCollectionCreator, embeddedCollectionCreator);
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

		if (value.StateObject != null)
		{
			var node = JsonSerializer.SerializeToNode(value.StateObject, options);//JsonNode.Parse(JsonSerializer.Serialize(value.StateObject, options));
			if (node != null)
			{
				foreach (var item in node.AsObject())
				{
					if (item.Key.Equals(nameof(Resource.Links)))
						continue;

					if (item.Key.Equals(nameof(Resource.Embedded)))
						continue;

					if (item.Value is not null || options.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingNull)
					{
						writer.WritePropertyName(item.Key);
						if (item.Value != null)
						{
							item.Value.WriteTo(writer, options);
						}
						else
						{
							writer.WriteNullValue();
						}
					}
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
