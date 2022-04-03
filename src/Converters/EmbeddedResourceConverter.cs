using System.Text.Json.Nodes;

namespace Chatter.Rest.Hal.Converters;

public class EmbeddedResourceConverter : JsonConverter<EmbeddedResource>
{
	public override EmbeddedResource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true })!;

		EmbeddedResource? embedded = null;

		if (node is not JsonObject)
		{
			throw new JsonException($"A single {nameof(EmbeddedResource)} was expect.");
		}

		var jsonObject = node.AsObject();

		if (jsonObject.Count != 1)
		{
			throw new JsonException($"A single {nameof(EmbeddedResource)} was expect.");
		}

		var kvp = jsonObject.First();

		if (kvp.Value is JsonObject val)
		{
			embedded = new EmbeddedResource(kvp.Key)!;
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

		return embedded;
	}

	public override void Write(Utf8JsonWriter writer, EmbeddedResource value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(value.Name);
		JsonSerializer.Serialize(writer, value.Resources, options);
		writer.WriteEndObject();
	}
}
