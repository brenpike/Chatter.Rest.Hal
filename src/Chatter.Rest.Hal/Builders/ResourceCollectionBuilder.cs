using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using System;
using System.Collections.Generic;

namespace Chatter.Rest.Hal.Builders;

/// <summary>
/// Builder for constructing resource collections within embedded resources.
/// </summary>
public sealed class ResourceCollectionBuilder : HalBuilder<ResourceCollection>, IAddResourceStage, IEmbeddedResourceCreationStage
{
	private readonly IList<IEmbeddedResourceCreationStage> _resourceBuilders = new List<IEmbeddedResourceCreationStage>();
	private ResourceCollectionBuilder(IBuildHalPart<EmbeddedResource> parent) : base(parent) { }

	internal static ResourceCollectionBuilder New(IBuildHalPart<EmbeddedResource> parent) => new(parent);

	/// <summary>
	/// Flag indicating whether this resource (if embedded) should be explicitly written to JSON as an array,
	/// rather than dynamically written as either an object or collection based on its resource count (as
	/// specified by <see href="https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-4.1.2"/>)
	/// </summary>
	public bool ForceWriteAsCollection { get; private set; } = false;

	/// <summary>
	/// Adds a new resource to the collection with no state.
	/// </summary>
	/// <returns>An embedded resource creation stage.</returns>
	public IEmbeddedResourceCreationStage AddResource()
	{
		var rb = ResourceCollectionResourceBuilder.New(this, null);
		_resourceBuilders.Add(rb);
		return rb;
	}

	/// <summary>
	/// Adds a new resource to the collection with the specified state.
	/// </summary>
	/// <param name="state">The state object for the resource.</param>
	/// <returns>An embedded resource creation stage.</returns>
	public IEmbeddedResourceCreationStage AddResource(object? state)
	{
		var rb = ResourceCollectionResourceBuilder.New(this, state);
		_resourceBuilders.Add(rb);
		return rb;
	}

	/// <summary>
	/// Adds multiple resources to the collection from an enumerable source.
	/// </summary>
	/// <typeparam name="T">The type of items in the source enumerable.</typeparam>
	/// <param name="resources">The collection of items to add as resources.</param>
	/// <param name="builder">Optional builder action to configure each resource.</param>
	/// <returns>An embedded resource creation stage.</returns>
	public IEmbeddedResourceCreationStage AddResources<T>(IEnumerable<T> resources, Action<T, IEmbeddedResourceCreationStage>? builder = null)
	{
		ForceWriteAsCollection = true; // Flag that this resource (if embedded) is a collection
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

		throw new InvalidOperationException("No parent EmbeddedResourceCollection or Resource builder found to add an embedded resource.");
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

	/// <summary>
	/// Builds the resource collection from all added resources.
	/// </summary>
	/// <returns>The constructed resource collection.</returns>
	public override ResourceCollection BuildPart()
	{
		var resourceCollection = new ResourceCollection();
		foreach (var rb in _resourceBuilders)
		{
			resourceCollection.Add(rb.BuildPart());
		}
		return resourceCollection;
	}

	Resource IBuildHalPart<Resource>.BuildPart() => throw new NotSupportedException("ResourceCollectionBuilder builds ResourceCollection, not individual Resource instances.");
}

/// <summary>
/// Builder for constructing individual resources within a resource collection.
/// </summary>
public sealed class ResourceCollectionResourceBuilder : ResourceBuilder, IEmbeddedResourceCreationStage
{

	internal ResourceCollectionResourceBuilder(IBuildHalPart<ResourceCollection> parent, object? state) : base(parent, state)
	{ }

	internal static IEmbeddedResourceCreationStage New(IBuildHalPart<ResourceCollection> parent, object? state) => new ResourceCollectionResourceBuilder(parent, state);

	/// <summary>
	/// Adds a sibling resource to the collection with no state.
	/// </summary>
	/// <returns>An embedded resource creation stage.</returns>
	public IEmbeddedResourceCreationStage AddResource()
	{
		var resourceCollectionBuilder = FindParent<ResourceCollection>() as IAddResourceStage;
		return resourceCollectionBuilder!.AddResource();
	}

	/// <summary>
	/// Adds a sibling resource to the collection with the specified state.
	/// </summary>
	/// <param name="state">The state object for the resource.</param>
	/// <returns>An embedded resource creation stage.</returns>
	public IEmbeddedResourceCreationStage AddResource(object? state)
	{
		var resourceCollectionBuilder = FindParent<ResourceCollection>() as IAddResourceStage;
		return resourceCollectionBuilder!.AddResource(state);
	}

	/// <summary>
	/// Adds multiple sibling resources to the collection from an enumerable source.
	/// </summary>
	/// <typeparam name="T">The type of items in the source enumerable.</typeparam>
	/// <param name="resources">The collection of items to add as resources.</param>
	/// <param name="builder">Optional builder action to configure each resource.</param>
	/// <returns>An embedded resource creation stage.</returns>
	public IEmbeddedResourceCreationStage AddResources<T>(IEnumerable<T> resources, Action<T, IEmbeddedResourceCreationStage>? builder = null)
	{
		var resourceCollectionBuilder = FindParent<ResourceCollection>() as IAddResourceStage;
		return resourceCollectionBuilder!.AddResources(resources, builder);
	}

	IEmbeddedCuriesLinkCreationStage IAddCuriesLinkToEmbeddedStage.AddCuries() => base.AddCuries();
	IEmbeddedLinkCreationStage IAddLinkToEmbeddedStage.AddLink(string rel) => base.AddLink(rel);
	IEmbeddedLinkCreationStage IAddSelfLinkToEmbeddedStage.AddSelf() => base.AddSelf();
}
