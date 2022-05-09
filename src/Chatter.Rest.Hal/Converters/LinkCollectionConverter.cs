using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

public class LinkCollectionConverter : JsonConverter<LinkCollection>
{
	public override LinkCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true })!;

		var links = new LinkCollection();

		if (node is JsonObject jo)
		{
			CreateLinksAndAddToCollection(options, jo, links);
		}

		if (node is JsonArray ja)
		{
			foreach (var item in ja)
			{
				CreateLinksAndAddToCollection(options, item?.AsObject(), links);
			}
		}

		return links;
	}

	private static void CreateLinksAndAddToCollection(JsonSerializerOptions options, JsonObject? jo, LinkCollection links)
	{
		if (jo == null)
		{
			return;
		}

		foreach (var kvp in jo)
		{
			var link = new Link(kvp.Key)!;
			if (kvp.Value is JsonObject val)
			{
				link.LinkObjects.Add(val.Deserialize<LinkObject>(options)!);
			}

			if (kvp.Value is JsonArray ja)
			{
				var loc = ja.Deserialize<LinkObjectCollection>(options)!;
				link = new Link(kvp.Key)
				{
					LinkObjects = loc
				};
			}

			links.Add(link);
		}
	}

	public override void Write(Utf8JsonWriter writer, LinkCollection links, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		foreach (var link in links)
		{
			writer.WritePropertyName(link.Rel);
			if (link.LinkObjects.Count == 1)
			{
				JsonSerializer.Serialize(writer, link.LinkObjects.First(), options);
			}
			else
			{
				writer.WriteStartArray();
				foreach (var linkObject in link.LinkObjects)
				{
					JsonSerializer.Serialize(writer, linkObject, options);
				}
				writer.WriteEndArray();
			}
		}
		writer.WriteEndObject();
	}
}
