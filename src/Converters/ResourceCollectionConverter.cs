using System.Text.Json.Nodes;

namespace Chatter.Rest.Hal.Converters;

public class ResourceCollectionConverter : JsonConverter<ResourceCollection>
{
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

	private static void CreateAndAddResource(JsonSerializerOptions options, ResourceCollection resources, JsonNode? node)
	{
		var resource = node.Deserialize<Resource>(options);
		if (resource != null)
		{
			resources.Add(resource);
		}
	}

	public override void Write(Utf8JsonWriter writer, ResourceCollection resources, JsonSerializerOptions options)
	{
		if (resources.Count == 1)
		{
			JsonSerializer.Serialize(writer, resources.First(), options);
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
