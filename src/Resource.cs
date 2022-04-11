namespace Chatter.Rest.Hal;

[JsonConverter(typeof(ResourceConverter))]
public sealed record Resource : IHalPart
{
    public Resource() { }
    public Resource(object? state) => StateImpl = state;

    public object? StateImpl { get; init; }
    public LinkCollection Links { get; init; } = new ();
    public EmbeddedResourceCollection EmbeddedResources { get; init; } = new ();

    public T? State<T>() where T : class
    {
        if (StateImpl is JsonElement je)
        {
            try
            {
                return je.Deserialize<T>();
            }
            catch (JsonException)
            {
                return default;
            }
        }

        return StateImpl as T;
    }
}
