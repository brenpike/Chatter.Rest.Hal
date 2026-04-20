using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

/// <summary>
/// JSON converter for serializing and deserializing embedded resource collections (_embedded).
/// </summary>
public class EmbeddedResourceCollectionConverter : JsonConverter<EmbeddedResourceCollection>
{
	/// <summary>
	/// Reads an EmbeddedResourceCollection from JSON, parsing named embedded resources.
	/// </summary>
	/// <param name="reader">The JSON reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">Serializer options.</param>
	/// <returns>The deserialized EmbeddedResourceCollection.</returns>
	public override EmbeddedResourceCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true });

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

	/// <summary>
	/// Creates embedded resources from a JSON object and adds them to the collection.
	/// </summary>
	/// <param name="options">Serializer options.</param>
	/// <param name="jo">The JSON object containing embedded resource data.</param>
	/// <param name="embeddedResources">The embedded resource collection to populate.</param>
	private static void CreateEmbeddedAndAddToCollection(JsonSerializerOptions options, JsonObject? jo, EmbeddedResourceCollection embeddedResources)
    {
        if (jo == null)
        {
            return;
        }

        foreach (var kvp in jo)
        {
            var embedded = new EmbeddedResource(kvp.Key);
            if (kvp.Value is JsonObject val)
            {
                var res = val.Deserialize<Resource>(options);
                if (res != null) embedded.Resources.Add(res);
            }

            else if (kvp.Value is JsonArray ja)
            {
                var rc = ja.Deserialize<ResourceCollection>(options) ?? new ResourceCollection();
                embedded = new EmbeddedResource(kvp.Key)
                {
                    Resources = rc
                };
            }

            // If value is null or other JSON types, leave embedded with empty Resources
            embeddedResources.Add(embedded);
        }
    }

    /// <summary>
    /// Write embedded collection into JSON as either a single Resource Object or an array of Resource Objects
    /// (see <see href="https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-4.1.2"/>)
    /// </summary>
    public override void Write(Utf8JsonWriter writer, EmbeddedResourceCollection embeddedResources, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var embeddedvalue in embeddedResources)
        {
            writer.WritePropertyName(embeddedvalue.Name);
            // If there is only one resource in collection, write as Object (unless collection has been explicitly
            // flagged as a collection, in which case it should be written as an array even if only one element)
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
