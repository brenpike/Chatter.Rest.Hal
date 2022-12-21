using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using System.Collections.Generic;

namespace Chatter.Rest.Hal.Builders;

public sealed class ResourceCollectionBuilder : HalBuilder<ResourceCollection>, IAddResourceStage
{
	private readonly IList<IEmbeddedResourceCreationStage> _resourceBuilders = new List<IEmbeddedResourceCreationStage>();
	private ResourceCollectionBuilder(IBuildHalPart<EmbeddedResource> parent) : base(parent) { }

	public static ResourceCollectionBuilder New(IBuildHalPart<EmbeddedResource> parent) => new(parent);

	public IEmbeddedResourceCreationStage AddResource()
	{
		var rb = ResourceCollectionResourceBuilder.New(this, null);
		_resourceBuilders.Add(rb);
		return rb;
	}

	public IEmbeddedResourceCreationStage AddResource(object? state)
	{
		var rb = ResourceCollectionResourceBuilder.New(this, state);
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

public sealed class ResourceCollectionResourceBuilder : ResourceBuilder, IEmbeddedResourceCreationStage
{

	private ResourceCollectionResourceBuilder(IBuildHalPart<ResourceCollection> parent, object? state) : base(parent, state)
	{ }

	internal static IEmbeddedResourceCreationStage New(IBuildHalPart<ResourceCollection> parent, object? state) => new ResourceCollectionResourceBuilder(parent, state);

	public IEmbeddedResourceCreationStage AddResource()
	{
		var resourceCollectionBuilder = FindParent<ResourceCollection>() as IAddResourceStage;
		return resourceCollectionBuilder!.AddResource();
	}

	public IEmbeddedResourceCreationStage AddResource(object? state)
	{
		var resourceCollectionBuilder = FindParent<ResourceCollection>() as IAddResourceStage;
		return resourceCollectionBuilder!.AddResource(state);
	}

	IEmbeddedCuriesLinkCreationStage IAddCuriesLinkToEmbeddedStage.AddCuries() => base.AddCuries();
	IEmbeddedLinkCreationStage IAddLinkToEmbeddedStage.AddLink(string rel) => base.AddLink(rel);
	IEmbeddedLinkCreationStage IAddSelfLinkToEmbeddedStage.AddSelf() => base.AddSelf();
}
