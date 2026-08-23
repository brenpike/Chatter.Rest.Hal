using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using System;
using System.Collections.Generic;

namespace Chatter.Rest.Hal.Builders;

/// <summary>
/// Builder for constructing resource collections within embedded resources.
/// </summary>
public sealed class ResourceCollectionBuilder : HalBuilder<ResourceCollection>, IAddResourceStage, IEmbeddedResourceCreationStage, IDeclareUnbuildableHalParts
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

	/// <summary>
	/// Resolves the link collection of the resource that owns this collection's "_embedded" entry.
	/// </summary>
	/// <returns>The owning resource's link collection builder.</returns>
	/// <exception cref="InvalidOperationException">Thrown when no owning resource builder exists.</exception>
	/// <remarks>
	/// A LinkCollectionBuilder is always a child of a resource builder, never an ancestor of a
	/// ResourceCollectionBuilder, so the previous FindParent&lt;LinkCollection&gt;() lookup was
	/// null in every case. Links added from this stage belong to the resource that owns the
	/// embedded entry; per-resource links are configured inside the AddResources callback.
	/// </remarks>
	private LinkCollectionBuilder OwningResourceLinks()
	{
		if (FindParent<Resource>() is ResourceBuilder owner)
		{
			return owner.Links;
		}

		throw new InvalidOperationException("No owning resource builder was found to add a link to. Add links to an individual embedded resource via the AddResources builder callback.");
	}

	IEmbeddedCuriesLinkCreationStage IAddCuriesLinkToEmbeddedStage.AddCuries() => OwningResourceLinks().AddCuries();

	IAddResourceStage IAddEmbeddedResourceToResourceStage.AddEmbedded(string name)
	{
		// The resource that owns this collection's "_embedded" entry is the nearest Resource
		// ancestor; resolve it first so the embed lands on the owning resource.
		if (FindParent<Resource>() is IAddEmbeddedResourceToResourceStage resource)
		{
			return resource.AddEmbedded(name);
		}

		if (FindParent<EmbeddedResourceCollection>() is IAddEmbeddedResourceToResourceStage embedded)
		{
			return embedded.AddEmbedded(name);
		}

		throw new InvalidOperationException("No parent EmbeddedResourceCollection or Resource builder found to add an embedded resource.");
	}

	IEmbeddedLinkCreationStage IAddLinkToEmbeddedStage.AddLink(string rel) => OwningResourceLinks().AddLink(rel);

	IEmbeddedLinkCreationStage IAddSelfLinkToEmbeddedStage.AddSelf() => OwningResourceLinks().AddSelf();

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

	// IEmbeddedResourceCreationStage forces this builder to satisfy IBuildHalPart<Resource> even
	// though it cannot build one. Declaring Resource unbuildable keeps FindParent<Resource> walks
	// from stopping here and handing back a builder whose BuildPart() throws.
	bool IDeclareUnbuildableHalParts.CannotBuild(Type halPartType) => halPartType == typeof(Resource);
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
		=> OwningResourceCollection().AddResource();

	/// <summary>
	/// Adds a sibling resource to the collection with the specified state.
	/// </summary>
	/// <param name="state">The state object for the resource.</param>
	/// <returns>An embedded resource creation stage.</returns>
	public IEmbeddedResourceCreationStage AddResource(object? state)
		=> OwningResourceCollection().AddResource(state);

	/// <summary>
	/// Adds multiple sibling resources to the collection from an enumerable source.
	/// </summary>
	/// <typeparam name="T">The type of items in the source enumerable.</typeparam>
	/// <param name="resources">The collection of items to add as resources.</param>
	/// <param name="builder">Optional builder action to configure each resource.</param>
	/// <returns>An embedded resource creation stage.</returns>
	public IEmbeddedResourceCreationStage AddResources<T>(IEnumerable<T> resources, Action<T, IEmbeddedResourceCreationStage>? builder = null)
		=> OwningResourceCollection().AddResources(resources, builder);

	/// <summary>
	/// Resolves the collection builder this resource belongs to, so sibling resources are added
	/// to the same "_embedded" entry.
	/// </summary>
	/// <returns>The owning resource collection builder.</returns>
	/// <exception cref="InvalidOperationException">Thrown when no owning resource collection builder exists.</exception>
	private IAddResourceStage OwningResourceCollection()
	{
		if (FindParent<ResourceCollection>() is IAddResourceStage collection)
		{
			return collection;
		}

		throw new InvalidOperationException("No owning resource collection builder was found to add a sibling resource to.");
	}

	IEmbeddedCuriesLinkCreationStage IAddCuriesLinkToEmbeddedStage.AddCuries() => base.AddCuries();
	IEmbeddedLinkCreationStage IAddLinkToEmbeddedStage.AddLink(string rel) => base.AddLink(rel);
	IEmbeddedLinkCreationStage IAddSelfLinkToEmbeddedStage.AddSelf() => base.AddSelf();
}
