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
	private JsonNode? _resourceNode = null;
	private object? _stateObject = null;
	private LinkCollection? _linksImpl = null;
	private EmbeddedResourceCollection? _embeddedImpl = null;
	private readonly Func<LinkCollection?> _linksCreator = () => new LinkCollection();
	private readonly Func<EmbeddedResourceCollection?> _embeddedCreator = () => new EmbeddedResourceCollection();
	private readonly Func<JsonObject?> _stateCreator = () => null;

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
					  Func<EmbeddedResourceCollection?> embeddedCreator)
	{
		_resourceNode = resourceNode;
		_stateCreator = stateCreator;
		_linksCreator = linksCreator;
		_embeddedCreator = embeddedCreator;
	}

	internal object? StateObject
	{
		get => State<object>();
		set => _stateObject = value;
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
				_linksImpl = _linksCreator();
			}
			return _linksImpl ?? new LinkCollection();
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
				_embeddedImpl = _embeddedCreator();
			}
			return _embeddedImpl ?? new EmbeddedResourceCollection();
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
	public T? State<T>() where T : class
	{
		try
		{
			if (_stateObject is JsonElement je)
			{
				// Be conservative when attempting to deserialize JsonElement to a Link:
				// a generic object with multiple properties (e.g. a DTO) should not be
				// interpreted as a HAL link. If the requested type is Link and the JSON
				// object does not contain exactly one property (the rel), return null.
				if (typeof(T) == typeof(Link) && je.ValueKind == JsonValueKind.Object)
				{
					if (je.EnumerateObject().Count() != 1)
					{
						return null;
					}
				}

				_stateObject = je.Deserialize<T>();
			}

			if (_stateObject == null)
			{
				var stateObject = _stateCreator();
				_stateObject = stateObject?.Deserialize<T>();
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
	public T? As<T>() where T : class
	{
		try
		{
			if (_resourceNode is null)
			{
				_resourceNode = JsonSerializer.SerializeToNode(this);
			}

			return _resourceNode?.Deserialize<T>();
		}
		catch (Exception)
		{
			return null;		}
	}
}
