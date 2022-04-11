using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;

namespace Chatter.Rest.Hal.Builders;

public class EmbeddedResourceBuilder : HalBuilder<EmbeddedResource>, IAddResourceStage
{
	private readonly string _name;
	private readonly ResourceCollectionBuilder _resourceCollectionBuilder;

	private EmbeddedResourceBuilder(IBuildHalPart<EmbeddedResourceCollection> ercb, string name) : base(ercb)
	{
		_name = name;
		_resourceCollectionBuilder = ResourceCollectionBuilder.New(this);
	}

	public static EmbeddedResourceBuilder WithName(IBuildHalPart<EmbeddedResourceCollection> ercb, string name)
		=> new(ercb, name);

	public IEmbeddedResourceCreationStage AddResource() => _resourceCollectionBuilder.AddResource();
	public IEmbeddedResourceCreationStage AddResource(object? state) => _resourceCollectionBuilder.AddResource(state);

	public override EmbeddedResource BuildPart()
	{
		return new EmbeddedResource(_name)
		{
			Resources = _resourceCollectionBuilder.BuildPart()
		};
	}
}
