using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

/// <summary>
/// JSON converter for serializing and deserializing individual embedded resources.
/// </summary>
public sealed class EmbeddedResourceConverter : JsonConverter<EmbeddedResource>
{
	/// <summary>
	/// Reads an EmbeddedResource from JSON, parsing the name and resource collection.
	/// </summary>
	/// <param name="reader">The JSON reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">Serializer options.</param>
	/// <returns>The deserialized EmbeddedResource, or null if the JSON is malformed.</returns>
	/// <exception cref="JsonException">Thrown when a single embedded resource object is expected but not found.</exception>
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

	/// <summary>
	/// Writes an EmbeddedResource to JSON with the name as the property key.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="value">The EmbeddedResource to serialize.</param>
	/// <param name="options">Serializer options.</param>
	public override void Write(Utf8JsonWriter writer, EmbeddedResource value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(value.Name);
		JsonSerializer.Serialize(writer, value.Resources, options);
		writer.WriteEndObject();
	}
}
