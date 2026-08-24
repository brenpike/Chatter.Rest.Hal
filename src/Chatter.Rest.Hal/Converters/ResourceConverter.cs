using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Converters;

/// <summary>
/// JSON converter for serializing and deserializing HAL resources, separating state from _links and _embedded.
/// </summary>
public sealed class ResourceConverter : JsonConverter<Resource>
{
	/// <summary>
	/// Reads a Resource from JSON, parsing state, _links, and _embedded properties.
	/// </summary>
	/// <param name="reader">The JSON reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">Serializer options.</param>
	/// <returns>The deserialized Resource.</returns>
	/// <exception cref="JsonException">
	/// Thrown when the JSON is not a Resource Object, or when its <c>_links</c>/<c>_embedded</c> members
	/// are structurally invalid.
	/// </exception>
	public override Resource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = ConverterHelpers.ParseNode(ref reader);

		// A HAL Resource Object is a JSON object. Rejecting anything else here — rather than letting
		// the lazy creators index a primitive or an array later — keeps the failure at the
		// deserialization call instead of deferring an InvalidOperationException to property access.
		if (node is not JsonObject resourceObject)
		{
			throw new JsonException("A HAL Resource must be a JSON object.");
		}

		return ReadFromNode(resourceObject, options);
	}

	/// <summary>
	/// Materializes a Resource directly from an already-parsed node. Nested converters call this
	/// instead of <see cref="JsonSerializer.Deserialize{TValue}(JsonNode, JsonSerializerOptions)"/>, which would re-serialize the subtree to UTF-8
	/// and re-parse it — repeating that once per ancestor turns a deeply nested <c>_embedded</c>
	/// chain into O(depth × size) work; walking the existing tree keeps the whole document at O(size).
	/// </summary>
	internal static Resource ReadFromNode(JsonObject resourceObject, JsonSerializerOptions options)
	{
		// The HAL reserved names are literal and case-sensitive, so they are matched ordinally
		// regardless of the caller's PropertyNameCaseInsensitive setting. This keeps the reserved
		// lookups consistent with the state stripping in jsonObjectCreator.
		// Both collections are materialized eagerly so a malformed _links/_embedded member fails
		// at the deserialization call, not on a later property read.
		var linksNode = ConverterHelpers.GetReservedProperty(resourceObject, ConverterHelpers.LinksProperty);
		var links = linksNode is null
			? null
			: ConverterHelpers.HasCustomConverter<LinkCollection>(options, typeof(LinkCollectionConverter))
				? linksNode.Deserialize<LinkCollection>(options)
				: LinkCollectionConverter.ReadFromNode(linksNode, options);
		var embeddedNode = ConverterHelpers.GetReservedProperty(resourceObject, ConverterHelpers.EmbeddedProperty);
		var embedded = embeddedNode is null
			? null
			: ConverterHelpers.HasCustomConverter<EmbeddedResourceCollection>(options, typeof(EmbeddedResourceCollectionConverter))
				? embeddedNode.Deserialize<EmbeddedResourceCollection>(options)
				: EmbeddedResourceCollectionConverter.ReadFromNode(embeddedNode, options);

		LinkCollection? linkCollectionCreator() => links;

		EmbeddedResourceCollection? embeddedCollectionCreator() => embedded;

		JsonObject? jsonObjectCreator()
		{
			var result = new JsonObject();
			foreach (var kvp in resourceObject)
			{
				if (ConverterHelpers.IsReservedProperty(kvp.Key)) continue;
#if NET8_0_OR_GREATER
				result.Add(kvp.Key, kvp.Value?.DeepClone());
#else
				result.Add(kvp.Key, kvp.Value?.Deserialize<JsonNode>());
#endif
			}
			return result;
		};

		return new Resource(resourceObject, jsonObjectCreator, linkCollectionCreator, embeddedCollectionCreator, options);
	}

	/// <summary>
	/// Writes a Resource to JSON, serializing state properties followed by _links and _embedded.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="value">The Resource to serialize.</param>
	/// <param name="options">Serializer options.</param>
	/// <exception cref="JsonException">
	/// Thrown when the resource's state serializes to something other than a JSON object (or JSON null),
	/// because a HAL Resource Object has no place to put a primitive or array state.
	/// </exception>
	public override void Write(Utf8JsonWriter writer, Resource value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		var writesLinks = value.Links != null && value.Links.Count > 0;
		var writesEmbedded = value.Embedded != null && value.Embedded.Count > 0;

		if (value.CachedState != null)
		{
			using var doc = CreateStateDocument(value.CachedState, options);
			if (doc != null)
			{
				WriteStateMembers(writer, doc.RootElement, options, writesLinks, writesEmbedded);
			}
		}

		if (writesLinks)
		{
			writer.WritePropertyName(ConverterHelpers.LinksProperty);
			JsonSerializer.Serialize(writer, value.Links, options);
		}

		if (writesEmbedded)
		{
			writer.WritePropertyName(ConverterHelpers.EmbeddedProperty);
			JsonSerializer.Serialize(writer, value.Embedded, options);
		}

		writer.WriteEndObject();
	}

	/// <summary>
	/// Serializes the state and validates its HAL shape.
	/// </summary>
	/// <returns>The parsed state document, or <c>null</c> when the state serializes to JSON null
	/// and therefore contributes no members.</returns>
	/// <exception cref="JsonException">Thrown when the state serializes to a primitive or an array,
	/// which has no representable HAL form.</exception>
	private static JsonDocument? CreateStateDocument(object cachedState, JsonSerializerOptions options)
	{
		var utf8Bytes = JsonSerializer.SerializeToUtf8Bytes(cachedState, options);
		var doc = JsonDocument.Parse(utf8Bytes);

		// A HAL Resource Object is a JSON object, so its state members can only be merged into the
		// envelope when the state itself serializes to an object. A state that serializes to a
		// primitive or an array has no representable HAL form; report that as a JsonException rather
		// than letting JsonElement.EnumerateObject leak an InvalidOperationException. State that
		// serializes to JSON null simply contributes no members.
		var kind = doc.RootElement.ValueKind;
		if (kind is not JsonValueKind.Object and not JsonValueKind.Null)
		{
			doc.Dispose();
			throw new JsonException(
				$"A HAL Resource's state must serialize to a JSON object, but it serialized to {kind}. "
				+ "Wrap the value in an object (or place it in an _embedded resource) to serialize it as HAL.");
		}

		if (kind == JsonValueKind.Null)
		{
			doc.Dispose();
			return null;
		}

		return doc;
	}

	/// <summary>
	/// Serializes exactly the state members <see cref="Write"/> would emit for
	/// <paramref name="value"/>, as a UTF-8 JSON object.
	/// </summary>
	/// <remarks>
	/// This is the single source of truth Resource equality derives its state key from: the same
	/// <see cref="CreateStateDocument"/> and <see cref="WriteStateMembers"/> code paths the writer
	/// uses produce the bytes, so equality can never drift from serialization when the writer's
	/// rules change.
	/// </remarks>
	/// <returns>The emitted members as a UTF-8 JSON object, or <c>null</c> when no members are
	/// emitted (absent state, or state serializing to JSON null).</returns>
	internal static byte[]? SerializeStateMembersToUtf8(Resource value, JsonSerializerOptions options)
	{
		var cachedState = value.CachedState;
		if (cachedState == null)
		{
			return null;
		}

		var writesLinks = value.Links != null && value.Links.Count > 0;
		var writesEmbedded = value.Embedded != null && value.Embedded.Count > 0;

		using var doc = CreateStateDocument(cachedState, options);
		if (doc == null)
		{
			return null;
		}

		using var buffer = new MemoryStream();
		using (var writer = new Utf8JsonWriter(buffer))
		{
			writer.WriteStartObject();
			WriteStateMembers(writer, doc.RootElement, options, writesLinks, writesEmbedded);
			writer.WriteEndObject();
		}

		return buffer.ToArray();
	}

	/// <summary>
	/// Writes the members of the serialized state object into the enclosing Resource Object.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="state">The serialized state; a JSON object, or JSON null for no members.</param>
	/// <param name="options">Serializer options.</param>
	/// <param name="writesLinks">Whether the resource writes its own <c>_links</c> member.</param>
	/// <param name="writesEmbedded">Whether the resource writes its own <c>_embedded</c> member.</param>
	private static void WriteStateMembers(Utf8JsonWriter writer, JsonElement state, JsonSerializerOptions options, bool writesLinks, bool writesEmbedded)
	{
		if (state.ValueKind != JsonValueKind.Object)
		{
			return;
		}

		foreach (var prop in state.EnumerateObject())
		{
			// HAL's reserved names are the literal strings "_links"/"_embedded"; a state property the
			// naming policy maps to "Links"/"Embedded" is ordinary user data and is always written.
			// A state property already carrying a reserved name is skipped only when the resource's
			// own collection is about to be written under that same name, because emitting both would
			// produce a duplicate JSON member, which RFC 8259 leaves undefined.
			if ((writesLinks && string.Equals(prop.Name, ConverterHelpers.LinksProperty, StringComparison.Ordinal))
				|| (writesEmbedded && string.Equals(prop.Name, ConverterHelpers.EmbeddedProperty, StringComparison.Ordinal)))
				continue;

			if (prop.Value.ValueKind != JsonValueKind.Null || options.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingNull)
			{
				prop.WriteTo(writer);
			}
		}
	}
}
