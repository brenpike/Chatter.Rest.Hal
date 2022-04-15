namespace Chatter.Rest.Hal;

[JsonConverter(typeof(ResourceConverter))]
public sealed record Resource : IHalPart
{
	private object? _stateCache = null;
	public Resource() { }
	public Resource(object? state) => StateImpl = state;

	internal object? StateImpl { get; init; }
	public LinkCollection Links { get; init; } = new();
	public EmbeddedResourceCollection EmbeddedResources { get; init; } = new();

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
