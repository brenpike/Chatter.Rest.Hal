using System;
using System.Collections.Generic;
using Chatter.Rest.Hal.Builders.Stages;

namespace Chatter.Rest.Hal.Builders;

/// <summary>
/// Builder for constructing embedded resource collections within a HAL resource.
/// </summary>
public class EmbeddedResourceCollectionBuilder : HalBuilder<EmbeddedResourceCollection>, IAddEmbeddedResourceToResourceStage
{
	private readonly IList<EmbeddedResourceBuilder> _embeddedBuilders = new List<EmbeddedResourceBuilder>();

	private EmbeddedResourceCollectionBuilder(IBuildHalPart<Resource> parent) : base(parent) { }

	internal static EmbeddedResourceCollectionBuilder New(IBuildHalPart<Resource> parent) => new(parent);

	///<inheritdoc/>
	/// <summary>
	/// Adds an embedded resource with the specified name to the collection.
	/// </summary>
	/// <param name="name">The name of the embedded resource.</param>
	/// <returns>A resource addition stage.</returns>
	public IAddResourceStage AddEmbedded(string name)
	{
		var embedded = EmbeddedResourceBuilder.WithName(this, name);
		_embeddedBuilders.Add(embedded);
		return embedded;
	}

	///<inheritdoc/>
	/// <summary>
	/// Builds the embedded resource collection from all added embedded resources.
	/// </summary>
	/// <returns>The constructed embedded resource collection.</returns>
	public override EmbeddedResourceCollection BuildPart()
	{
		var embeddedCollection = new EmbeddedResourceCollection();
		foreach (var embedded in _embeddedBuilders)
		{
			embeddedCollection.Add(embedded.BuildPart());
		}
		return embeddedCollection;
	}
}
