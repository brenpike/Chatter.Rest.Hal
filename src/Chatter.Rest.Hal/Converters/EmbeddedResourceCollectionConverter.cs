using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

/// <summary>
/// JSON converter for serializing and deserializing embedded resource collections (_embedded).
/// </summary>
public sealed class EmbeddedResourceCollectionConverter : JsonConverter<EmbeddedResourceCollection>
{
	/// <summary>
	/// Reads an EmbeddedResourceCollection from JSON, parsing named embedded resources.
	/// </summary>
	/// <param name="reader">The JSON reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">Serializer options.</param>
	/// <returns>The deserialized EmbeddedResourceCollection.</returns>
	/// <exception cref="JsonException">Thrown when the JSON is not a valid HAL embedded resource collection.</exception>
	public override EmbeddedResourceCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = ConverterHelpers.ParseNode(ref reader);

		// Duplicate names are normalized last-wins before anything reaches the collection: HAL models
		// _embedded as a JSON object keyed by relation name, so a name can only appear once, and a
		// duplicate must not surface as an ArgumentException from the collection's name index.
		var ordered = new List<EmbeddedResource>();
		var namePositions = new Dictionary<string, int>(StringComparer.Ordinal);

		if (node is JsonObject jo)
		{
			CreateEmbeddedAndAddToCollection(options, jo, ordered, namePositions);
		}
		else if (node is JsonArray ja)
		{
			foreach (var item in ja)
			{
				if (item is null || ConverterHelpers.IsJsonNull(item))
				{
					continue;
				}

				if (item is not JsonObject itemObject)
				{
					throw new JsonException("Every element of a HAL embedded resource collection array must be a JSON object.");
				}

				CreateEmbeddedAndAddToCollection(options, itemObject, ordered, namePositions);
			}
		}
		else if (node is not null && !ConverterHelpers.IsJsonNull(node))
		{
			throw new JsonException("A HAL embedded resource collection must be a JSON object or an array of JSON objects.");
		}

		var embeddedResources = new EmbeddedResourceCollection();
		foreach (var embedded in ordered)
		{
			embeddedResources.Add(embedded);
		}

		return embeddedResources;
	}

	/// <summary>
	/// Creates embedded resources from a JSON object and adds them to the pending, duplicate-normalized list.
	/// </summary>
	/// <param name="options">Serializer options.</param>
	/// <param name="jo">The JSON object containing embedded resource data.</param>
	/// <param name="ordered">The embedded resources accumulated so far, in first-seen order.</param>
	/// <param name="namePositions">The position of each already-seen name within <paramref name="ordered"/>.</param>
	private static void CreateEmbeddedAndAddToCollection(JsonSerializerOptions options, JsonObject jo, List<EmbeddedResource> ordered, Dictionary<string, int> namePositions)
	{
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
			AddOrReplace(ordered, namePositions, embedded);
		}
	}

	/// <summary>
	/// Appends an embedded resource, or replaces the previously seen entry for the same name (last-wins)
	/// while keeping its original position.
	/// </summary>
	private static void AddOrReplace(List<EmbeddedResource> ordered, Dictionary<string, int> namePositions, EmbeddedResource embedded)
	{
		if (namePositions.TryGetValue(embedded.Name, out var position))
		{
			ordered[position] = embedded;
			return;
		}

		namePositions[embedded.Name] = ordered.Count;
		ordered.Add(embedded);
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
				JsonSerializer.Serialize(writer, embeddedvalue.Resources[0], options);
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
