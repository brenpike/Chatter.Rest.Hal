using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

public class EmbeddedResourceCollectionConverter : JsonConverter<EmbeddedResourceCollection>
{
	public override EmbeddedResourceCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true })!;

		var embeddedResources = new EmbeddedResourceCollection();
		
		if (node is JsonObject jo)
		{
			CreateEmbeddedAndAddToCollection(options, jo?.AsObject(), embeddedResources);
		}

		if (node is JsonArray ja)
		{
			foreach (var item in ja)
			{
				CreateEmbeddedAndAddToCollection(options, item?.AsObject(), embeddedResources);
			}
		}

		return embeddedResources;
	}

	private static void CreateEmbeddedAndAddToCollection(JsonSerializerOptions options, JsonObject? jo, EmbeddedResourceCollection embeddedResources)
	{
		if (jo == null)
		{
			return;
		}

		foreach (var kvp in jo)
		{
			var embedded = new EmbeddedResource(kvp.Key)!;
			if (kvp.Value is JsonObject val)
			{
				embedded.Resources.Add(val.Deserialize<Resource>(options)!);
			}

			if (kvp.Value is JsonArray ja)
			{
				var rc = ja.Deserialize<ResourceCollection>(options)!;
				embedded = new EmbeddedResource(kvp.Key)
				{
					Resources = rc
				};
			}

			embeddedResources.Add(embedded);
		}
	}

	public override void Write(Utf8JsonWriter writer, EmbeddedResourceCollection embeddedResources, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		foreach (var embeddedvalue in embeddedResources)
		{
			writer.WritePropertyName(embeddedvalue.Name);
			if (embeddedvalue.Resources.Count == 1 && !embeddedvalue.ForceWriteAsCollection)
			{
				JsonSerializer.Serialize(writer, embeddedvalue.Resources.First(), options);
			}
			else
			{
				writer.WriteStartArray();
				foreach (var resource in embeddedvalue.Resources)
				{
					JsonSerializer.Serialize(writer, resource, options);
				}
				writer.WriteEndArray();
			}
		}
		writer.WriteEndObject();
	}
}
