using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using System;
using System.Collections.Generic;

namespace Chatter.Rest.Hal.Builders;

/// <summary>
/// Builder for constructing embedded resources within a HAL document.
/// </summary>
public class EmbeddedResourceBuilder : HalBuilder<EmbeddedResource>, IAddResourceStage
{
	private readonly string _name;
	private readonly ResourceCollectionBuilder _resourceCollectionBuilder;

	private EmbeddedResourceBuilder(IBuildHalPart<EmbeddedResourceCollection> ercb, string name) : base(ercb)
	{
		_name = name;
		_resourceCollectionBuilder = ResourceCollectionBuilder.New(this);
	}

	/// <summary>
	/// Creates a new embedded resource builder with the specified name.
	/// </summary>
	/// <param name="ercb">The parent embedded resource collection builder.</param>
	/// <param name="name">The name of the embedded resource.</param>
	/// <returns>A new embedded resource builder.</returns>
	public static EmbeddedResourceBuilder WithName(IBuildHalPart<EmbeddedResourceCollection> ercb, string name)
		=> new(ercb, name);

	///<inheritdoc/>
	public IEmbeddedResourceCreationStage AddResource() => _resourceCollectionBuilder.AddResource();
	///<inheritdoc/>
	public IEmbeddedResourceCreationStage AddResource(object? state) => _resourceCollectionBuilder.AddResource(state);

	IEmbeddedResourceCreationStage IAddResourceStage.AddResources<T>(IEnumerable<T> resources, Action<T, IEmbeddedResourceCreationStage>? builder = null)
		=> _resourceCollectionBuilder.AddResources(resources, builder);

	/// <summary>
	/// Builds the EmbeddedResource with its name and resources.
	/// </summary>
	/// <returns>The constructed EmbeddedResource.</returns>
	public override EmbeddedResource BuildPart()
	{
		return new EmbeddedResource(_name)
		{
			Resources = _resourceCollectionBuilder.BuildPart(),
			ForceWriteAsCollection = _resourceCollectionBuilder.ForceWriteAsCollection
		};
	}

}
