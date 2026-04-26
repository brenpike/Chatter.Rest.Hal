using Chatter.Rest.Hal.Converters;
using System;
using System.Linq;
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
	/// If the state has already been deserialized and cached from a prior <see cref="State{T}()"/>
	/// call, the cached object is returned regardless of the supplied options.
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

				_stateObject = je.Deserialize<T>(options);
			}

			if (_stateObject == null)
			{
				var stateObject = _stateCreator();
				_stateObject = stateObject?.Deserialize<T>(options);
			}

			return (T?)_stateObject;
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
	/// The internal JSON node is cached on the first serialize call. Subsequent calls with different
	/// options apply those options only to deserialization, not to re-serialization of the node.
	/// </remarks>
	/// <typeparam name="T">The expected reference type to convert the Resource to.</typeparam>
	/// <param name="options">The <see cref="JsonSerializerOptions"/> to use, or <c>null</c> to use default options.</param>
	/// <returns>An object of <typeparamref name="T"/> or null if conversion fails.</returns>
	public T? As<T>(JsonSerializerOptions? options) where T : class
	{
		try
		{
			if (_resourceNode is null)
			{
				_resourceNode = JsonSerializer.SerializeToNode(this, options);
			}

			return _resourceNode?.Deserialize<T>(options);
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
}
