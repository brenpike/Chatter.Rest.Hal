using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

public class LinkObjectCollectionConverter : JsonConverter<LinkObjectCollection>
{
	public override LinkObjectCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true });

		var linkObjects = new LinkObjectCollection();

		if (node is JsonObject jo)
		{
			CreateAndAddLinkObject(options, linkObjects, jo);
		}

		if (node is JsonArray ja)
		{
			foreach (var loNode in ja)
			{
				CreateAndAddLinkObject(options, linkObjects, loNode);
			}
		}

		return linkObjects;
	}

	private static void CreateAndAddLinkObject(JsonSerializerOptions options, LinkObjectCollection linkObjects, JsonNode? node)
	{
		var linkObject = node.Deserialize<LinkObject>(options);
		if (linkObject != null)
		{
			linkObjects.Add(linkObject);
		}
	}

	public override void Write(Utf8JsonWriter writer, LinkObjectCollection linkObjects, JsonSerializerOptions options)
	{
		if (linkObjects.Count == 1)
		{
			JsonSerializer.Serialize(writer, linkObjects.First(), options);
		}
		else
		{
			writer.WriteStartArray();
			foreach (var linkObject in linkObjects)
			{
				JsonSerializer.Serialize(writer, linkObject, options);
			}
			writer.WriteEndArray();
		}
	}
}
