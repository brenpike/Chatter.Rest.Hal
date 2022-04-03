using System.Text.Json.Nodes;

namespace Chatter.Rest.Hal.Converters;

public class LinkConverter : JsonConverter<Link>
{
	public override Link? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true })!;

		Link? link = null;

		if (node is not JsonObject)
		{
			throw new JsonException("Invalid json: a single");
		}

		var jsonObject = node.AsObject();

		if (jsonObject.Count != 1)
		{
			throw new JsonException($"A single {nameof(Link)} was expect.");
		}

		var kvp = jsonObject.First();

		if (kvp.Value is JsonObject)
		{
			link = new Link(kvp.Key)!;
			link.LinkObjects.Add(kvp.Value.Deserialize<LinkObject>(options)!);
		}

		if (kvp.Value is JsonArray ja)
		{
			var loc = ja.Deserialize<LinkObjectCollection>(options)!;
			link = new Link(kvp.Key)
			{
				LinkObjects = loc
			};
		}

		return link;
	}

	public override void Write(Utf8JsonWriter writer, Link value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(value.Rel);
		JsonSerializer.Serialize(writer, value.LinkObjects, options);
		writer.WriteEndObject();
	}
}
