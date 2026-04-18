using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

public class LinkConverter : JsonConverter<Link>
{
    public override Link? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true });

        // Only accept a single-property object representing a HAL link entry (e.g. { "rel": { ... } })
        if (node is not JsonObject jsonObject)
        {
            // Not an object we can interpret as a Link  be tolerant and return null
            return null;
        }

        // A standalone Link JSON should be an object with exactly one property (the rel)
        if (jsonObject.Count != 1)
        {
            return null;
        }

        var kvp = jsonObject.First();

        // If rel is missing or invalid, return null rather than throwing to be tolerant of malformed input.
        if (string.IsNullOrWhiteSpace(kvp.Key))
        {
            return null;
        }

        var rel = kvp.Key;
        Link link = new Link(rel);

        // If value is null (e.g. "rel": null) create an empty Link with no LinkObjects.
        if (kvp.Value == null || kvp.Value.ToJsonString() == "null")
        {
            return link;
        }

        // If the value is an object, ensure it contains an href (required by HAL) before deserializing.
        if (kvp.Value is JsonObject obj)
        {
            if (obj["href"] == null || obj["href"].ToJsonString() == "null")
            {
                // Not a valid Link Object shape
                return null;
            }

            var lo = obj.Deserialize<LinkObject>(options);
            if (lo != null) link.LinkObjects.Add(lo);
            return link;
        }

        // If the value is an array, ensure it is an array of Link Objects (each must have href)
        if (kvp.Value is JsonArray ja)
        {
            foreach (var item in ja)
            {
                if (item is not JsonObject itemObj)
                {
                    return null;
                }
                if (itemObj["href"] == null || itemObj["href"].ToJsonString() == "null")
                {
                    return null;
                }
            }

            var loc = ja.Deserialize<LinkObjectCollection>(options);
            if (loc != null) link.LinkObjects = loc;
            return link;
        }

        // If the value is a primitive (commonly a string shorthand), treat it as an href value
        if (kvp.Value is JsonValue jv)
        {
            try
            {
                var href = jv.Deserialize<string?>();
                if (!string.IsNullOrWhiteSpace(href))
                {
                    link.LinkObjects.Add(new LinkObject(href));
                    return link;
                }
            }
            catch
            {
                // fallthrough to return null
            }
        }

        // Not a recognized HAL link shape be conservative and return null
        return null;
    }

    public override void Write(Utf8JsonWriter writer, Link value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(value.Rel);
        JsonSerializer.Serialize(writer, value.LinkObjects, options);
        writer.WriteEndObject();
    }
}
