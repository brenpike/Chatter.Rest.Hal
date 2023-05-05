using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using System;
using System.Collections.Generic;

namespace Chatter.Rest.Hal.Builders;

public sealed class ResourceCollectionBuilder : HalBuilder<ResourceCollection>, IAddResourceStage, IEmbeddedResourceCreationStage
{
	private readonly IList<IEmbeddedResourceCreationStage> _resourceBuilders = new List<IEmbeddedResourceCreationStage>();
	private ResourceCollectionBuilder(IBuildHalPart<EmbeddedResource> parent) : base(parent) { }

	public static ResourceCollectionBuilder New(IBuildHalPart<EmbeddedResource> parent) => new(parent);

	public bool ForceWriteAsCollection { get; private set; }

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

	public IEmbeddedResourceCreationStage AddResources<T>(IEnumerable<T> resources, Action<T, IEmbeddedResourceCreationStage>? builder = null)
	{
		ForceWriteAsCollection = true;
		foreach (var resource in resources)
		{
			var rb = new ResourceCollectionResourceBuilder(this, resource);
			builder?.Invoke(resource, rb);
			_resourceBuilders.Add(rb);
		}

		return this;
	}

	IEmbeddedCuriesLinkCreationStage IAddCuriesLinkToEmbeddedStage.AddCuries()
	{
		var linkCollectionBuilder = FindParent<LinkCollection>() as IAddCuriesLinkStage;
		return linkCollectionBuilder!.AddCuries();
	}

	IAddResourceStage IAddEmbeddedResourceToResourceStage.AddEmbedded(string name)
	{
		if (FindParent<EmbeddedResourceCollection>() is IAddEmbeddedResourceToResourceStage embedded)
		{
			return embedded.AddEmbedded(name);
		}

		if (FindParent<Resource>() is IAddEmbeddedResourceToResourceStage resource)
		{
			return resource.AddEmbedded(name);
		}

		throw new InvalidOperationException();
	}

	IEmbeddedLinkCreationStage IAddLinkToEmbeddedStage.AddLink(string rel)
	{
		var linkCollectionBuilder = FindParent<LinkCollection>() as IAddLinkStage;
		return linkCollectionBuilder!.AddLink(rel);
	}

	IEmbeddedLinkCreationStage IAddSelfLinkToEmbeddedStage.AddSelf()
	{
		var linkCollectionBuilder = FindParent<LinkCollection>() as IAddSelfLinkStage;
		return linkCollectionBuilder!.AddSelf();
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

	Resource IBuildHalPart<Resource>.BuildPart() => throw new NotImplementedException();
}

public sealed class ResourceCollectionResourceBuilder : ResourceBuilder, IEmbeddedResourceCreationStage
{

	internal ResourceCollectionResourceBuilder(IBuildHalPart<ResourceCollection> parent, object? state) : base(parent, state)
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

	public IEmbeddedResourceCreationStage AddResources<T>(IEnumerable<T> resources, Action<T, IEmbeddedResourceCreationStage>? builder = null)
	{
		var resourceCollectionBuilder = FindParent<ResourceCollection>() as IAddResourceStage;
		return resourceCollectionBuilder!.AddResources(resources, builder);
	}

	IEmbeddedCuriesLinkCreationStage IAddCuriesLinkToEmbeddedStage.AddCuries() => base.AddCuries();
	IEmbeddedLinkCreationStage IAddLinkToEmbeddedStage.AddLink(string rel) => base.AddLink(rel);
	IEmbeddedLinkCreationStage IAddSelfLinkToEmbeddedStage.AddSelf() => base.AddSelf();
}
