using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal;

namespace Chatter.Rest.Hal.Converters;

/// <summary>
/// JSON converter for serializing and deserializing link object collections within a link relation.
/// </summary>
public class LinkObjectCollectionConverter : JsonConverter<LinkObjectCollection>
{
	private readonly HalJsonOptions? _halJsonOptions;

	/// <summary>
	/// Initializes a new instance with no explicit options; uses <see cref="HalJsonOptions.Default"/> at write time.
	/// </summary>
	public LinkObjectCollectionConverter() { }

	/// <summary>
	/// Initializes a new instance with the specified <see cref="HalJsonOptions"/>.
	/// </summary>
	/// <param name="options">The HAL JSON options to use during serialization.</param>
	public LinkObjectCollectionConverter(HalJsonOptions options) => _halJsonOptions = options;

	/// <summary>
	/// Reads a LinkObjectCollection from JSON, handling both single objects and arrays.
	/// </summary>
	/// <param name="reader">The JSON reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">Serializer options.</param>
	/// <returns>The deserialized LinkObjectCollection.</returns>
	public override LinkObjectCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true });

		var linkObjects = new LinkObjectCollection();

		if (node is JsonObject jo)
		{
			CreateAndAddLinkObject(options, linkObjects, jo);
		}

		if (node is JsonArray ja)
		{
			foreach (var loNode in ja)
			{
				CreateAndAddLinkObject(options, linkObjects, loNode);
			}
		}

		return linkObjects;
	}

	/// <summary>
	/// Creates a link object from a JSON node and adds it to the collection.
	/// </summary>
	/// <param name="options">Serializer options.</param>
	/// <param name="linkObjects">The link object collection to populate.</param>
	/// <param name="node">The JSON node containing link object data.</param>
	private static void CreateAndAddLinkObject(JsonSerializerOptions options, LinkObjectCollection linkObjects, JsonNode? node)
	{
		if (node == null) return;

		if (node is JsonValue jv)
		{
			try
			{
				var href = jv.GetValue<string>();
				if (!string.IsNullOrWhiteSpace(href))
				{
					linkObjects.Add(new LinkObject(href));
				}
			}
			catch
			{
				// ignore non-string values
			}

			return;
		}

		var linkObject = node.Deserialize<LinkObject>(options);
		if (linkObject != null)
		{
			linkObjects.Add(linkObject);
		}
	}

	/// <summary>
	/// Writes a LinkObjectCollection to JSON, serializing as a single object or array based on count.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="linkObjects">The LinkObjectCollection to serialize.</param>
	/// <param name="options">Serializer options.</param>
	public override void Write(Utf8JsonWriter writer, LinkObjectCollection linkObjects, JsonSerializerOptions options)
	{
		bool forceArray = (_halJsonOptions ?? HalJsonOptions.Default).AlwaysUseArrayForLinks;
		if (!forceArray && linkObjects.Count == 1)
		{
			JsonSerializer.Serialize(writer, linkObjects[0], options);
		}
		else
		{
			writer.WriteStartArray();
			foreach (var linkObject in linkObjects)
			{
				JsonSerializer.Serialize(writer, linkObject, options);
			}
			writer.WriteEndArray();
		}
	}
}
