using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

/// <summary>
/// JSON converter for serializing and deserializing resource collections within embedded resources.
/// </summary>
public sealed class ResourceCollectionConverter : JsonConverter<ResourceCollection>
{
	/// <summary>
	/// Reads a ResourceCollection from JSON, handling both single resources and arrays.
	/// </summary>
	/// <param name="reader">The JSON reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">Serializer options.</param>
	/// <returns>The deserialized ResourceCollection.</returns>
	public override ResourceCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true });

		var resources = new ResourceCollection();

		if (node is JsonObject jo)
		{
			CreateAndAddResource(options, resources, jo);
		}

		if (node is JsonArray ja)
		{
			foreach (var resNode in ja)
			{
				CreateAndAddResource(options, resources, resNode);
			}
		}

		return resources;
	}

	/// <summary>
	/// Creates a resource from a JSON node and adds it to the collection.
	/// </summary>
	/// <param name="options">Serializer options.</param>
	/// <param name="resources">The resource collection to populate.</param>
	/// <param name="node">The JSON node containing resource data.</param>
	private static void CreateAndAddResource(JsonSerializerOptions options, ResourceCollection resources, JsonNode? node)
	{
		var resource = node.Deserialize<Resource>(options);
		if (resource != null)
		{
			resources.Add(resource);
		}
	}

	/// <summary>
	/// Writes a ResourceCollection to JSON, serializing as a single object or array based on count.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="resources">The ResourceCollection to serialize.</param>
	/// <param name="options">Serializer options.</param>
	public override void Write(Utf8JsonWriter writer, ResourceCollection resources, JsonSerializerOptions options)
	{
		if (resources.Count == 1)
		{
			JsonSerializer.Serialize(writer, resources[0], options);
		}
		else
		{
			writer.WriteStartArray();
			foreach (var resource in resources)
			{
				JsonSerializer.Serialize(writer, resource, options);
			}
			writer.WriteEndArray();
		}
	}
}
