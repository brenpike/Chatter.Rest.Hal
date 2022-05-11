using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

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

    public override void Write(Utf8JsonWriter writer, LinkObject linkObject, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrWhiteSpace(linkObject.Href))
        {
            writer.WritePropertyName(nameof(linkObject.Href).ToLower()); //TODO: make this respect json options for casing, etc.
            writer.WriteStringValue(linkObject.Href);
        }

        if (linkObject.Templated.HasValue)
        {
            writer.WritePropertyName(nameof(linkObject.Templated).ToLower());
            writer.WriteBooleanValue(linkObject.Templated.Value);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Type))
        {
            writer.WritePropertyName(nameof(linkObject.Type).ToLower());
            writer.WriteStringValue(linkObject.Type);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Deprecation))
        {
            writer.WritePropertyName(nameof(linkObject.Deprecation).ToLower());
            writer.WriteStringValue(linkObject.Deprecation);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Name))
        {
            writer.WritePropertyName(nameof(linkObject.Name).ToLower());
            writer.WriteStringValue(linkObject.Name);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Title))
        {
            writer.WritePropertyName(nameof(linkObject.Title).ToLower());
            writer.WriteStringValue(linkObject.Title);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Profile))
        {
            writer.WritePropertyName(nameof(linkObject.Profile).ToLower());
            writer.WriteStringValue(linkObject.Profile);
        }

        if (!string.IsNullOrWhiteSpace(linkObject.Hreflang))
        {
            writer.WritePropertyName(nameof(linkObject.Hreflang).ToLower());
            writer.WriteStringValue(linkObject.Hreflang);
        }

        writer.WriteEndObject();
    }
}
