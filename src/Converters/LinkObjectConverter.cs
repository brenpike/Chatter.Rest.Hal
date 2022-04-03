using System.Text.Json.Nodes;

namespace Chatter.Rest.Hal.Converters;

public class LinkObjectConverter : JsonConverter<LinkObject>
{
    public override LinkObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true });

        if (node == null)
        {
            return null;
        }
		
        if (node[nameof(LinkObject.Href)] is null)
        {
            throw new JsonException($"No Href property found. Href is required.");
        }

        return new LinkObject(node[nameof(LinkObject.Href)]?.GetValue<string>()!)
        {
            Templated = node[nameof(LinkObject.Templated)]?.GetValue<bool>(),
            Type = node[nameof(LinkObject.Type)]?.GetValue<string>(),
            Deprecation = node[nameof(LinkObject.Deprecation)]?.GetValue<string>(),
            Name = node[nameof(LinkObject.Name)]?.GetValue<string>(),
            Title = node[nameof(LinkObject.Title)]?.GetValue<string>(),
            Profile = node[nameof(LinkObject.Profile)]?.GetValue<string>(),
            Hreflang = node[nameof(LinkObject.Hreflang)]?.GetValue<string>()
        };
    }

    public override void Write(Utf8JsonWriter writer, LinkObject li, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrWhiteSpace(li.Href))
        {
            writer.WritePropertyName(nameof(li.Href).ToLower()); //TODO: make this respect json options for casing, etc.
            writer.WriteStringValue(li.Href);
        }

        if (li.Templated.HasValue)
        {
            writer.WritePropertyName(nameof(li.Templated).ToLower());
            writer.WriteBooleanValue(li.Templated.Value);
        }

        if (!string.IsNullOrWhiteSpace(li.Type))
        {
            writer.WritePropertyName(nameof(li.Type).ToLower());
            writer.WriteStringValue(li.Type);
        }

        if (!string.IsNullOrWhiteSpace(li.Deprecation))
        {
            writer.WritePropertyName(nameof(li.Deprecation).ToLower());
            writer.WriteStringValue(li.Deprecation);
        }

        if (!string.IsNullOrWhiteSpace(li.Name))
        {
            writer.WritePropertyName(nameof(li.Name).ToLower());
            writer.WriteStringValue(li.Name);
        }

        if (!string.IsNullOrWhiteSpace(li.Title))
        {
            writer.WritePropertyName(nameof(li.Title).ToLower());
            writer.WriteStringValue(li.Title);
        }

        if (!string.IsNullOrWhiteSpace(li.Profile))
        {
            writer.WritePropertyName(nameof(li.Profile).ToLower());
            writer.WriteStringValue(li.Profile);
        }

        if (!string.IsNullOrWhiteSpace(li.Hreflang))
        {
            writer.WritePropertyName(nameof(li.Hreflang).ToLower());
            writer.WriteStringValue(li.Hreflang);
        }

        writer.WriteEndObject();
    }
}
