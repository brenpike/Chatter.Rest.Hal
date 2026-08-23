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
		var node = ConverterHelpers.ParseNode(ref reader);

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
				Resources = rc,
				// Preserve the incoming array shape, exactly as EmbeddedResourceCollectionConverter does.
				ForceWriteAsCollection = true
			};
		}

		// If value is null or other JSON types we return an embedded with empty resources
		return embedded;
	}

	/// <summary>
	/// Writes an EmbeddedResource to JSON with the name as the property key.
	/// </summary>
	/// <remarks>
	/// The single-resource/array decision matches
	/// <see cref="EmbeddedResourceCollectionConverter.Write"/> — including
	/// <see cref="EmbeddedResource.ForceWriteAsCollection"/> — so the same instance produces the same
	/// shape whether it is written standalone or as a member of an <c>_embedded</c> collection.
	/// </remarks>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="value">The EmbeddedResource to serialize.</param>
	/// <param name="options">Serializer options.</param>
	public override void Write(Utf8JsonWriter writer, EmbeddedResource value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(value.Name);
		if (value.Resources.Count == 1 && !value.ForceWriteAsCollection)
		{
			JsonSerializer.Serialize(writer, value.Resources[0], options);
		}
		else
		{
			writer.WriteStartArray();
			foreach (var resource in value.Resources)
			{
				JsonSerializer.Serialize(writer, resource, options);
			}
			writer.WriteEndArray();
		}
		writer.WriteEndObject();
	}
}
