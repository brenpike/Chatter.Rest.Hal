using Chatter.Rest.Hal.Converters;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal;

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

	public Resource() { }
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
	/// Gets the strongly typed State of a Resource given a generic type parameter
	/// </summary>
	/// <typeparam name="T">The expected type of the Resource state</typeparam>
	/// <returns>The Resource state of type <typeparamref name="T"/> or null if the Resource state is not type <typeparamref name="T"/></returns>
	public T? State<T>() where T : class
	{
		try
		{
			if (_stateObject is JsonElement je)
			{
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
	/// Casts the <see cref="Resource"/> to a strongly typed object of type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">The expected type of the Resource</typeparam>
	/// <returns>An object of <typeparamref name="T"/> or null if the Resource is not type <typeparamref name="T"/></returns>
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
			return null;
		}
	}
}
