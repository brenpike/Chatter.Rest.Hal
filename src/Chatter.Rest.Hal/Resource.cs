using System.Text.Json;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal;

[JsonConverter(typeof(ResourceConverter))]
public sealed record Resource : IHalPart
{
	private object? _stateCache = null;
	public Resource() { }
	public Resource(object? state) => StateImpl = state;

	internal object? StateImpl { get; set; }
	public LinkCollection Links { get; set; } = new();
	public EmbeddedResourceCollection EmbeddedResources { get; set; } = new();

	/// <summary>
	/// Gets the strongly typed State of a Resource given a generic type parameter
	/// </summary>
	/// <typeparam name="T">The expected type of the Resource state</typeparam>
	/// <returns>The Resource state of type <typeparamref name="T"/> or null if the Resource state is not type <typeparamref name="T"/></returns>
	public T? State<T>() where T : class
	{
		if (StateImpl is JsonElement je)
		{
			try
			{
				if (_stateCache == null)
				{
					_stateCache = je.Deserialize<T>();
				}
				return (T?)_stateCache;
			}
			catch (JsonException)
			{
				return null;
			}
		}

		return StateImpl as T;
	}
}
