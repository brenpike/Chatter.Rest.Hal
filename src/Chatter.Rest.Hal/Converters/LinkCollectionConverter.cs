using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

/// <summary>
/// JSON converter for serializing and deserializing link collections (_links).
/// </summary>
public class LinkCollectionConverter : JsonConverter<LinkCollection>
{
	/// <summary>
	/// Reads a LinkCollection from JSON, parsing link relations and their associated link objects.
	/// </summary>
	/// <param name="reader">The JSON reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">Serializer options.</param>
	/// <returns>The deserialized LinkCollection.</returns>
	public override LinkCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true })!;

		var links = new LinkCollection();

		if (node is JsonObject jo)
		{
			CreateLinksAndAddToCollection(options, jo, links);
		}

		if (node is JsonArray ja)
		{
			foreach (var item in ja)
			{
				CreateLinksAndAddToCollection(options, item?.AsObject(), links);
			}
		}

		return links;
	}

	/// <summary>
	/// Creates links from a JSON object and adds them to the collection.
	/// </summary>
	/// <param name="options">Serializer options.</param>
	/// <param name="jo">The JSON object containing link data.</param>
	/// <param name="links">The link collection to populate.</param>
	private static void CreateLinksAndAddToCollection(JsonSerializerOptions options, JsonObject? jo, LinkCollection links)
	{
		if (jo == null)
		{
			return;
		}

		foreach (var kvp in jo)
		{
			if (string.IsNullOrWhiteSpace(kvp.Key))
			{
				continue;
			}
			var link = new Link(kvp.Key);
			// If the value is literally null ("rel": null) leave LinkObjects empty and add the link.
			if (kvp.Value == null || kvp.Value.ToJsonString() == "null")
			{
				links.Add(link);
				continue;			}
			// Support string shorthand ("rel": "/orders/123")
			if (kvp.Value is JsonValue jv)
			{
				var href = jv.GetValue<string?>();
				if (!string.IsNullOrWhiteSpace(href)) link.LinkObjects.Add(new LinkObject(href));
			}
			else if (kvp.Value is JsonObject val)
			{
				var lo = val.Deserialize<LinkObject>(options);
				if (lo != null) link.LinkObjects.Add(lo);
			}

			if (kvp.Value is JsonArray ja)
			{
				var loc = ja.Deserialize<LinkObjectCollection>(options);
				if (loc != null) link.LinkObjects = loc;
			}

			links.Add(link);
		}
	}

	/// <summary>
	/// Writes a LinkCollection to JSON, serializing each link as a property with its relation as the key.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="links">The LinkCollection to serialize.</param>
	/// <param name="options">Serializer options.</param>
	public override void Write(Utf8JsonWriter writer, LinkCollection links, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		foreach (var link in links)
		{
			writer.WritePropertyName(link.Rel);
			if (link.LinkObjects.Count == 1)
			{
				JsonSerializer.Serialize(writer, link.LinkObjects.First(), options);
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
