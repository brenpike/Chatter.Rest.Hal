using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal;

[JsonConverter(typeof(EmbeddedResourceConverter))]
public sealed record EmbeddedResource
{
	public EmbeddedResource(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
		}
		Name = name;
	}

	public string Name { get; init; }
	public ResourceCollection Resources { get; init; } = new();
}
