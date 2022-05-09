using System.Collections.Generic;
using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;

namespace Chatter.Rest.Hal.Builders;

public sealed class ResourceCollectionBuilder : HalBuilder<ResourceCollection>, IAddResourceStage
{
	private readonly IList<IEmbeddedResourceCreationStage> _resourceBuilders = new List<IEmbeddedResourceCreationStage>();
	private ResourceCollectionBuilder(IBuildHalPart<EmbeddedResource> parent) : base(parent) { }

	public static ResourceCollectionBuilder New(IBuildHalPart<EmbeddedResource> parent) => new(parent);

	public IEmbeddedResourceCreationStage AddResource()
	{
		var rb = ResourceBuilder.Embedded(this);
		_resourceBuilders.Add(rb);
		return rb;
	}

	public IEmbeddedResourceCreationStage AddResource(object? state)
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
