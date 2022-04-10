using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;

namespace Chatter.Rest.Hal.Builders;

public class ResourceCollectionBuilder : HalBuilder<ResourceCollection>, IAddResourceStage
{
	private readonly IList<IEmbeddedResource> _resourceBuilders = new List<IEmbeddedResource>();

	private ResourceCollectionBuilder(IBuildHalPart<EmbeddedResource> parent) : base(parent)
	{
	}

	public static ResourceCollectionBuilder New(IBuildHalPart<EmbeddedResource> parent) => new(parent);

	public IEmbeddedResource AddResource()
	{
		var rb = ResourceBuilder.Embedded(this);
		_resourceBuilders.Add(rb);
		return rb;
	}

	public IEmbeddedResource AddResource(object? state)
	{
		var rb = ResourceBuilder.Embedded(this, state);
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
