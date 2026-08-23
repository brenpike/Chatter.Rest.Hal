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
	/// <returns>The deserialized EmbeddedResource, or null if the JSON object is empty.</returns>
	/// <exception cref="JsonException">
	/// Thrown when the JSON is not an object, or when it declares more than one relation name — a single
	/// <see cref="EmbeddedResource"/> holds exactly one name, so the extra relations have nowhere to go.
	/// Deserialize an <see cref="EmbeddedResourceCollection"/> to read a multi-relation <c>_embedded</c> object.
	/// </exception>
	public override EmbeddedResource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = ConverterHelpers.ParseNode(ref reader);

		if (node is not JsonObject jo)
		{
			throw new JsonException($"A single {nameof(EmbeddedResource)} was expected.");
		}

		if (jo.Count > 1)
		{
			throw new JsonException(
				$"A single {nameof(EmbeddedResource)} was expected, but the JSON object declares {jo.Count} relation names. "
				+ $"Deserialize an {nameof(EmbeddedResourceCollection)} to read more than one.");
		}

		var kvp = jo.FirstOrDefault();
		if (kvp.Equals(default(KeyValuePair<string, JsonNode?>)))
		{
			return null;
		}

		EmbeddedResource embedded = new EmbeddedResource(kvp.Key);

		// The nested parts are materialized from the retained node rather than re-serialized to UTF-8 and
		// re-parsed, which would walk the whole subtree a second time at every nesting level.
		if (kvp.Value is JsonObject val)
		{
			var res = ConverterHelpers.HasCustomConverter<Resource>(options, typeof(ResourceConverter))
				? val.Deserialize<Resource>(options)
				: ResourceConverter.ReadFromNode(val, options);
			if (res != null) embedded.Resources.Add(res);
		}

		if (kvp.Value is JsonArray ja)
		{
			var rc = ConverterHelpers.HasCustomConverter<ResourceCollection>(options, typeof(ResourceCollectionConverter))
				? ja.Deserialize<ResourceCollection>(options) ?? new ResourceCollection()
				: ResourceCollectionConverter.ReadFromNode(ja, options);
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
