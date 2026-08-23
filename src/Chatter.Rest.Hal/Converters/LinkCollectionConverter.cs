using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
namespace Chatter.Rest.Hal.Converters;

/// <summary>
/// JSON converter for serializing and deserializing link collections (_links).
/// </summary>
public sealed class LinkCollectionConverter : JsonConverter<LinkCollection>
{
	private readonly HalJsonOptions? _halJsonOptions;

	/// <summary>
	/// Initializes a new instance with no explicit options; uses <see cref="HalJsonOptions.Default"/> at write time.
	/// </summary>
	public LinkCollectionConverter() { }

	/// <summary>
	/// Initializes a new instance with the specified <see cref="HalJsonOptions"/>.
	/// </summary>
	/// <param name="options">The HAL JSON options to use during serialization.</param>
	public LinkCollectionConverter(HalJsonOptions options) => _halJsonOptions = options;

	/// <summary>
	/// Reads a LinkCollection from JSON, parsing link relations and their associated link objects.
	/// </summary>
	/// <param name="reader">The JSON reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">Serializer options.</param>
	/// <returns>The deserialized LinkCollection.</returns>
	/// <exception cref="JsonException">Thrown when the JSON is not a valid HAL link collection.</exception>
	public override LinkCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = ConverterHelpers.ParseNode(ref reader);
		return ReadFromNode(node, options);
	}

	/// <summary>
	/// Materializes a LinkCollection directly from an already-parsed node, so nested converters can
	/// reuse the existing tree instead of round-tripping the subtree through UTF-8 bytes.
	/// </summary>
	internal static LinkCollection ReadFromNode(JsonNode? node, JsonSerializerOptions options)
	{
		// Duplicate rels are normalized last-wins before anything reaches the collection: HAL models
		// _links as a JSON object keyed by rel, so a rel can only appear once, and a duplicate must
		// not surface as an ArgumentException from the collection's rel index.
		var ordered = new List<Link>();
		var relPositions = new Dictionary<string, int>(StringComparer.Ordinal);

		if (node is JsonObject jo)
		{
			CreateLinksAndAddToCollection(options, jo, ordered, relPositions);
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
					throw new JsonException("Every element of a HAL link collection array must be a JSON object.");
				}

				CreateLinksAndAddToCollection(options, itemObject, ordered, relPositions);
			}
		}
		else if (node is not null && !ConverterHelpers.IsJsonNull(node))
		{
			throw new JsonException("A HAL link collection must be a JSON object or an array of JSON objects.");
		}

		var links = new LinkCollection();
		foreach (var link in ordered)
		{
			links.Add(link);
		}

		return links;
	}

	/// <summary>
	/// Creates links from a JSON object and adds them to the pending, duplicate-normalized link list.
	/// </summary>
	/// <param name="options">Serializer options.</param>
	/// <param name="jo">The JSON object containing link data.</param>
	/// <param name="ordered">The links accumulated so far, in first-seen order.</param>
	/// <param name="relPositions">The position of each already-seen rel within <paramref name="ordered"/>.</param>
	private static void CreateLinksAndAddToCollection(JsonSerializerOptions options, JsonObject jo, List<Link> ordered, Dictionary<string, int> relPositions)
	{
		foreach (var kvp in jo)
		{
			if (string.IsNullOrWhiteSpace(kvp.Key))
			{
				continue;
			}

			var link = new Link(kvp.Key);
			// If the value is literally null ("rel": null) leave LinkObjects empty and add the link.
			if (kvp.Value == null || ConverterHelpers.IsJsonNull(kvp.Value))
			{
				AddOrReplace(ordered, relPositions, link);
				continue;
			}

			// Support string shorthand ("rel": "/orders/123")
			if (kvp.Value is JsonValue jv)
			{
				if (!jv.TryGetValue<string>(out var href))
				{
					throw new JsonException($"The value of link relation '{kvp.Key}' must be a Link Object, an array of Link Objects, or a string href.");
				}

				if (!string.IsNullOrWhiteSpace(href)) link.LinkObjects.Add(new LinkObject(href!));
			}
			else if (kvp.Value is JsonObject val)
			{
				var lo = val.Deserialize<LinkObject>(options);
				if (lo != null) link.LinkObjects.Add(lo);
			}
			else if (kvp.Value is JsonArray ja)
			{
				var loc = ja.Deserialize<LinkObjectCollection>(options);
				if (loc != null) link.LinkObjects = loc;
				link.IsArray = true;
			}

			AddOrReplace(ordered, relPositions, link);
		}
	}

	/// <summary>
	/// Appends a link, or replaces the previously seen link for the same rel (last-wins) while keeping
	/// its original position.
	/// </summary>
	private static void AddOrReplace(List<Link> ordered, Dictionary<string, int> relPositions, Link link)
	{
		if (relPositions.TryGetValue(link.Rel, out var position))
		{
			ordered[position] = link;
			return;
		}

		relPositions[link.Rel] = ordered.Count;
		ordered.Add(link);
	}

	/// <summary>
	/// Writes a LinkCollection to JSON, serializing each link as a property with its relation as the key.
	/// </summary>
	/// <remarks>
	/// A relation that carries no link objects — which is what tolerated input such as <c>"self": null</c>
	/// or an href-less <c>"self": {}</c> reads to — writes as the empty array <c>[]</c>. That is the only
	/// spec-conformant rendering available: HAL requires the value of a relation to be a Link Object or an
	/// array of Link Objects (draft-kelly-json-hal section 4.1.1), so <c>null</c> may never be written back,
	/// and dropping the relation would lose it entirely. The round trip therefore normalizes the shape
	/// rather than preserving it, and the relation itself survives.
	/// </remarks>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="links">The LinkCollection to serialize.</param>
	/// <param name="options">Serializer options.</param>
	public override void Write(Utf8JsonWriter writer, LinkCollection links, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		foreach (var link in links)
		{
			writer.WritePropertyName(link.Rel);
			bool forceArray = (_halJsonOptions ?? HalJsonOptions.Default).AlwaysUseArrayForLinks || link.IsArray;
			if (!forceArray && link.LinkObjects.Count == 1)
			{
				JsonSerializer.Serialize(writer, link.LinkObjects[0], options);
			}
			else
			{
				writer.WriteStartArray();
				foreach (var linkObject in link.LinkObjects)
				{
					JsonSerializer.Serialize(writer, linkObject, options);
				}
				writer.WriteEndArray();
			}
		}
		writer.WriteEndObject();
	}

}
