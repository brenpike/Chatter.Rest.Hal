namespace Chatter.Rest.Hal.Builders;

public class ResourceCollectionBuilder : HalBuilder<ResourceCollection>, IAddResourcesToCollectionStage
{
	private readonly IList<ResourceBuilder> _resourceBuilders = new List<ResourceBuilder>();

	private ResourceCollectionBuilder(IBuildHalPart<EmbeddedResource> parent) : base(parent)
	{
	}

	public static ResourceCollectionBuilder New(IBuildHalPart<EmbeddedResource> parent) => new(parent);

	public IBuildResource AddResource()
	{
		var rb = ResourceBuilder.New(this);
		_resourceBuilders.Add(rb);
		return rb;
	}

	public IBuildResource AddResource(object? state)
	{
		var rb = ResourceBuilder.New(this, state);
		_resourceBuilders.Add(rb);
		return rb;
	}

	public override ResourceCollection BuildPart()
	{
		var resourceCollection = new ResourceCollection();
		foreach (var rb in _resourceBuilders)
		{
			resourceCollection.Add(rb.BuildPart());
		}
		return resourceCollection;
	}
}
