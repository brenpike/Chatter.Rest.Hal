using Chatter.Rest.Hal.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal;

/// <summary>
/// Represents a HAL resource which may contain state, links and embedded resources.
/// This type is used as the in-memory representation of a HAL document or an
/// individual embedded resource.
/// </summary>
[JsonConverter(typeof(ResourceConverter))]
public sealed record Resource : IHalPart
{
	/// <summary>
	/// The normative HAL media type as defined in Section 4 of the HAL specification.
	/// </summary>
	public const string MediaType = "application/hal+json";

	/// <summary>
	/// Hash code contribution used for a state that cannot be represented as JSON. Equality of such
	/// states falls back to reference identity, so every one of them must share a hash code.
	/// </summary>
	private const int UnrepresentableStateHashCode = -1;

	private JsonNode? _resourceNode = null;
	private object? _stateObject = null;
	private LinkCollection? _linksImpl = null;
	private EmbeddedResourceCollection? _embeddedImpl = null;
	private readonly Func<LinkCollection?> _linksCreator = () => new LinkCollection();
	private readonly Func<EmbeddedResourceCollection?> _embeddedCreator = () => new EmbeddedResourceCollection();
	private readonly Func<JsonObject?> _stateCreator = () => null;
	private readonly JsonSerializerOptions? _jsonOptions;

	/// <summary>
	/// Initializes a new empty instance of the <see cref="Resource"/> type.
	/// </summary>
	public Resource() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="Resource"/> type with the provided state object.
	/// </summary>
	/// <param name="state">An optional state object to associate with this Resource. May be null.</param>
	public Resource(object? state) => _stateObject = state;

	internal Resource(JsonNode? resourceNode,
					  Func<JsonObject?> stateCreator,
					  Func<LinkCollection?> linksCreator,
					  Func<EmbeddedResourceCollection?> embeddedCreator,
					  JsonSerializerOptions? jsonOptions = null)
	{
		_resourceNode = resourceNode;
		_stateCreator = stateCreator;
		_linksCreator = linksCreator;
		_embeddedCreator = embeddedCreator;
		_jsonOptions = jsonOptions;
	}

	internal object? StateObject
	{
		get => State<object>();
		set => _stateObject = value;
	}

	/// <summary>
	/// Gets the cached state object, bypassing the Link-guard logic in <see cref="State{T}"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="State{T}"/> contains defensive checks that prevent a <see cref="System.Text.Json.JsonElement"/>-typed
	/// state from being misinterpreted as a <see cref="Link"/> object during deserialization.
	/// This property skips those checks and is therefore only safe for the serialization (write) path,
	/// where the state is being written out rather than interpreted as a domain type.
	/// </remarks>
	internal object? CachedState
	{
		get
		{
			if (_stateObject == null)
				_stateObject = _stateCreator()?.Deserialize<object>();
			return _stateObject;
		}
	}

	/// <summary>
	/// Gets or sets the Links collection for this Resource. The collection contains
	/// link relations and their associated link objects. The getter lazily creates
	/// the collection if it does not already exist.
	/// </summary>
	public LinkCollection Links
	{
		get
		{
			if (_linksImpl == null)
			{
				_linksImpl = _linksCreator() ?? new LinkCollection();
			}
			return _linksImpl;
		}
		set => _linksImpl = value;
	}

	/// <summary>
	/// Gets or sets the Embedded resources collection for this Resource. The getter
	/// lazily creates the collection if it does not already exist.
	/// </summary>
	public EmbeddedResourceCollection Embedded
	{
		get
		{
			if (_embeddedImpl == null)
			{
				_embeddedImpl = _embeddedCreator() ?? new EmbeddedResourceCollection();
			}
			return _embeddedImpl;
		}
		set => _embeddedImpl = value;
	}

	/// <summary>
	/// Gets the strongly typed State of a Resource given a generic type parameter.
	/// Attempts to deserialize the underlying state into the requested type. If
	/// deserialization fails or the state is not present, null is returned.
	/// </summary>
	/// <typeparam name="T">The expected reference type of the Resource state.</typeparam>
	/// <returns>The Resource state of type <typeparamref name="T"/> or null if the state is missing or cannot be converted.</returns>
	/// <inheritdoc cref="State{T}(JsonSerializerOptions?)"/>
	public T? State<T>() where T : class => State<T>(_jsonOptions);

	/// <summary>
	/// Gets the strongly typed State of a Resource given a generic type parameter,
	/// using the provided <see cref="JsonSerializerOptions"/> for deserialization.
	/// </summary>
	/// <remarks>
	/// A state supplied to the constructor as an instance of <typeparamref name="T"/> is returned
	/// directly, so mutating it changes what the resource serializes and how it compares. Every
	/// other case — a parsed resource, or a constructor-supplied <see cref="JsonElement"/> — returns
	/// a detached projection materialized from the underlying JSON on each call: the projection
	/// never becomes the serialization source, so mutating the returned object affects neither
	/// <see cref="Equals(Resource?)"/> nor serialization, and the resource can be projected onto
	/// several state types.
	/// </remarks>
	/// <typeparam name="T">The expected reference type of the Resource state.</typeparam>
	/// <param name="options">The <see cref="JsonSerializerOptions"/> to use for deserialization, or <c>null</c> to use default options.</param>
	/// <returns>The Resource state of type <typeparamref name="T"/> or null if the state is missing or cannot be converted.</returns>
	public T? State<T>(JsonSerializerOptions? options) where T : class
	{
		try
		{
			if (_stateObject is JsonElement je)
			{
				if (typeof(T) == typeof(Link) && je.ValueKind == JsonValueKind.Object)
				{
					if (je.EnumerateObject().Count() != 1)
					{
						return null;
					}
				}

				// Detached projection: the element remains the state of record.
				return je.Deserialize<T>(options);
			}

			if (_stateObject is T cached)
			{
				return cached;
			}

			// Parsed resources materialize a detached projection from the original JSON on every
			// call. Keeping projections out of the serialization source means a mutated DTO can
			// never make Write and equality disagree.
			return _stateCreator()?.Deserialize<T>(options);
		}
		catch (Exception)
		{
			return null;
		}
	}

	/// <summary>
	/// Casts the <see cref="Resource"/> to a strongly typed object of type <typeparamref name="T"/>.
	/// This serializes the Resource to a JsonNode (if needed) and then attempts to
	/// deserialize that node into the requested type. Returns null if conversion fails.
	/// </summary>
	/// <typeparam name="T">The expected reference type to convert the Resource to.</typeparam>
	/// <returns>An object of <typeparamref name="T"/> or null if conversion fails.</returns>
	/// <inheritdoc cref="As{T}(JsonSerializerOptions?)"/>
	public T? As<T>() where T : class => As<T>(_jsonOptions);

	/// <summary>
	/// Casts the <see cref="Resource"/> to a strongly typed object of type <typeparamref name="T"/>
	/// using the provided <see cref="JsonSerializerOptions"/>.
	/// </summary>
	/// <remarks>
	/// A resource produced by <see cref="Parse(string, JsonSerializerOptions?)"/> converts from the
	/// JSON node it was parsed from, so the original document shape is preserved and the supplied
	/// options apply only to deserialization. A resource constructed in memory is serialized on every
	/// call, so links, embedded resources and state added after an earlier <see cref="As{T}()"/> call
	/// are reflected in the result.
	/// </remarks>
	/// <typeparam name="T">The expected reference type to convert the Resource to.</typeparam>
	/// <param name="options">The <see cref="JsonSerializerOptions"/> to use, or <c>null</c> to use default options.</param>
	/// <returns>An object of <typeparamref name="T"/> or null if conversion fails.</returns>
	public T? As<T>(JsonSerializerOptions? options) where T : class
	{
		try
		{
			// _resourceNode is only ever set for a parsed resource, where it is the source document.
			// A resource constructed in memory is mutable, so it is re-serialized on every call
			// rather than answering from a node captured by an earlier call.
			var node = _resourceNode ?? JsonSerializer.SerializeToNode(this, options);
			return node?.Deserialize<T>(options);
		}
		catch (Exception)
		{
			return null;
		}
	}

	/// <summary>
	/// Parses a HAL JSON string into a <see cref="Resource"/>.
	/// </summary>
	/// <remarks>
	/// HAL type converters are attribute-wired on all HAL domain types and are applied
	/// automatically. If <paramref name="options"/> includes a custom naming policy or
	/// additional converters for state types, they will be propagated to subsequent
	/// <see cref="State{T}(JsonSerializerOptions?)"/> calls.
	/// To control HAL-specific behavior (e.g., <c>AlwaysUseArrayForLinks</c>), register
	/// HAL converters explicitly via
	/// <see cref="JsonSerializerOptionsExtensions.AddHalConverters(JsonSerializerOptions, HalJsonOptions?)"/>
	/// before passing <paramref name="options"/> to this method.
	/// </remarks>
	/// <param name="json">The HAL JSON string to parse.</param>
	/// <param name="options">Optional <see cref="JsonSerializerOptions"/> to use for deserialization and
	/// subsequent state materialization. When <c>null</c>, attribute-wired converters and default options are used.</param>
	/// <returns>The deserialized <see cref="Resource"/>, or <c>null</c> if the JSON represents <c>null</c>.</returns>
	public static Resource? Parse(string json, JsonSerializerOptions? options = null)
		=> JsonSerializer.Deserialize<Resource>(json, options);

	/// <summary>
	/// Determines whether this Resource represents the same HAL content as another Resource.
	/// </summary>
	/// <remarks>
	/// Equality is defined over the HAL content of the resource: its links, its embedded resources
	/// and its state. The mutable internal caches populated by reading <see cref="Links"/>,
	/// <see cref="Embedded"/>, <see cref="State{T}()"/> and <see cref="As{T}()"/> are deliberately
	/// excluded, so reading a property never changes whether two resources compare equal, and never
	/// changes the value returned by <see cref="GetHashCode"/>. Mutating a resource (for example by
	/// adding a link) does change its content and therefore does change its hash code.
	/// </remarks>
	/// <param name="other">The Resource to compare with.</param>
	/// <returns>true if both resources hold the same links, embedded resources and state; otherwise, false.</returns>
	public bool Equals(Resource? other)
	{
		if (other is null)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		if (!Links.Equals(other.Links) || !Embedded.Equals(other.Embedded))
		{
			return false;
		}

		var hasKey = TryGetStateEqualityKey(out var stateKey);
		var otherHasKey = other.TryGetStateEqualityKey(out var otherStateKey);

		if (!hasKey || !otherHasKey)
		{
			// At least one state cannot be represented as JSON, so its content cannot be compared.
			// Falling back to the identity of the state object keeps two resources sharing one state
			// object equal without reporting two distinct unrepresentable states, or an
			// unrepresentable state and an absent one, as equal.
			return ReferenceEquals(_stateObject, other._stateObject);
		}

		return string.Equals(stateKey, otherStateKey, StringComparison.Ordinal);
	}

	/// <summary>
	/// Returns a hash code derived from the HAL content of the resource.
	/// </summary>
	/// <remarks>
	/// The mutable internal caches are excluded, so the hash code is stable across repeated reads of
	/// <see cref="Links"/>, <see cref="Embedded"/>, <see cref="State{T}()"/> and <see cref="As{T}()"/>.
	/// </remarks>
	/// <returns>A hash code for the resource.</returns>
	public override int GetHashCode()
	{
		unchecked
		{
			var hash = 17;
			hash = (hash * 31) + Links.GetHashCode();
			hash = (hash * 31) + Embedded.GetHashCode();
			hash = (hash * 31) + (TryGetStateEqualityKey(out var stateKey)
				? (stateKey?.GetHashCode() ?? 0)
				: UnrepresentableStateHashCode);
			return hash;
		}
	}

	/// <summary>
	/// Produces a canonical JSON representation of the resource state for equality purposes.
	/// </summary>
	/// <remarks>
	/// The key is derived from the actual serializer: the exact member set
	/// <see cref="ResourceConverter"/> emits for this resource (via
	/// <c>SerializeStateMembersToUtf8</c>, the same code path <c>Write</c> uses) is canonicalized
	/// and compared. There is deliberately no re-implementation of the writer's rules here — null
	/// omission, reserved-name suppression, or any future writer behavior — so equality can never
	/// drift from serialization: two resources are state-equal exactly when the writer emits the
	/// same members for both. A resource that emits no members (absent state, empty state, or state
	/// serializing to JSON null) keys as absent.
	/// <para>
	/// The key is canonical: object properties are ordered by name, because JSON object members are
	/// unordered, while array element order is preserved, because JSON array order is significant.
	/// This matches how <see cref="LinkCollection"/> and <see cref="EmbeddedResourceCollection"/>
	/// compare against <see cref="LinkObjectCollection"/> and <see cref="ResourceCollection"/>.
	/// </para>
	/// </remarks>
	/// <param name="key">When this method returns true, contains the canonical state key, which is
	/// null when the resource emits no state members.</param>
	/// <returns>true if the state could be serialized; false if it could not (including a state the
	/// writer would reject), in which case the state has no comparable content.</returns>
	private bool TryGetStateEqualityKey(out string? key)
	{
		try
		{
			var utf8 = ResourceConverter.SerializeStateMembersToUtf8(
				this, _jsonOptions ?? JsonSerializerOptions.Default);
			if (utf8 == null)
			{
				key = null;
				return true;
			}

			var stateNode = JsonNode.Parse(utf8);
			if (stateNode is JsonObject { Count: 0 })
			{
				// No members emitted: the same HAL document as an absent state.
				key = null;
				return true;
			}

			var builder = new StringBuilder();
			WriteCanonicalJson(stateNode, builder);
			key = builder.ToString();
			return true;
		}
		catch (Exception)
		{
			key = null;
			return false;
		}
	}

	/// <summary>
	/// Writes a JSON node into <paramref name="builder"/> with object properties ordered by name and
	/// array element order preserved, so that two nodes holding the same JSON content always produce
	/// the same text.
	/// </summary>
	/// <param name="node">The node to write. May be null.</param>
	/// <param name="builder">The builder to write into.</param>
	private static void WriteCanonicalJson(JsonNode? node, StringBuilder builder)
	{
		if (node is JsonObject jsonObject)
		{
			builder.Append('{');
			var first = true;
			foreach (var property in jsonObject.OrderBy(p => p.Key, StringComparer.Ordinal))
			{
				if (!first)
				{
					builder.Append(',');
				}
				first = false;
				builder.Append(JsonSerializer.Serialize(property.Key));
				builder.Append(':');
				WriteCanonicalJson(property.Value, builder);
			}
			builder.Append('}');
			return;
		}

		if (node is JsonArray jsonArray)
		{
			builder.Append('[');
			for (var i = 0; i < jsonArray.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(',');
				}
				WriteCanonicalJson(jsonArray[i], builder);
			}
			builder.Append(']');
			return;
		}

		builder.Append(node == null ? "null" : node.ToJsonString());
	}
}
