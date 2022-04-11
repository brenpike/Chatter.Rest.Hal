namespace Chatter.Rest.Hal.Converters;

public class ResourceConverter : JsonConverter<Resource>
{
	public override Resource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true })!;

		var linksNode = node["_links"];
		var linkCache = new LinkCollection();
		if (linksNode != null)
		{
			linkCache = linksNode.Deserialize<LinkCollection>(options)!;
		}

		var embResourcesNode = node["_embedded"]!;
		var erCache = new EmbeddedResourceCollection();
		if (embResourcesNode != null)
		{
			erCache = embResourcesNode.Deserialize<EmbeddedResourceCollection>(options)!;
		}

		node.AsObject().Remove("_links");
		node.AsObject().Remove("_embedded");

		return new Resource(node.Deserialize(typeof(object), options))
		{
			Links = linkCache,
			EmbeddedResources = erCache
		};
	}

	public override void Write(Utf8JsonWriter writer, Resource value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		if (value.StateImpl != null)
		{
			var node = JsonNode.Parse(JsonSerializer.Serialize(value.StateImpl, options));
			if (node != null)
			{
				foreach (var item in node.AsObject())
				{
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

		if (value.EmbeddedResources != null && value.EmbeddedResources.Count > 0)
		{
			writer.WritePropertyName("_embedded");
			JsonSerializer.Serialize(writer, value.EmbeddedResources, options);
		}

		writer.WriteEndObject();
	}
}
