using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

public class EmbeddedResourceConverter : JsonConverter<EmbeddedResource>
{
    public override EmbeddedResource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true });

        if (node is not JsonObject jo)
        {
            throw new JsonException($"A single {nameof(EmbeddedResource)} was expected.");
        }

        var kvp = jo.FirstOrDefault();
        if (kvp.Equals(default(KeyValuePair<string, JsonNode?>)))
        {
            return null;
        }

        EmbeddedResource embedded = new EmbeddedResource(kvp.Key);

        if (kvp.Value is JsonObject val)
        {
            var res = val.Deserialize<Resource>(options);
            if (res != null) embedded.Resources.Add(res);
        }

        if (kvp.Value is JsonArray ja)
        {
            var rc = ja.Deserialize<ResourceCollection>(options) ?? new ResourceCollection();
            embedded = new EmbeddedResource(kvp.Key)
            {
                Resources = rc
            };
        }

        // If value is null or other JSON types we return an embedded with empty resources
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
