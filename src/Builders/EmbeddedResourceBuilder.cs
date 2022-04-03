namespace Chatter.Rest.Hal.Builders;

public class EmbeddedResourceBuilder : IBuildEmbeddedResource
{
	private readonly string _name;
	private readonly ResourceCollection _resources = new();

	private EmbeddedResourceBuilder(string name) => _name = name;

	public static IBuildEmbeddedResource WithName(string name) => new EmbeddedResourceBuilder(name);

	public IBuildEmbeddedResource AddResource(IBuildResource resourceBuilder)
	{
		_resources.Add(resourceBuilder.Build());
		return this;
	}

	EmbeddedResource IBuildEmbeddedResource.Build()
	{
		return new EmbeddedResource(_name)
		{
			Resources = _resources
		};
	}
}
