using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders;

public class ResourceBuilder : HalBuilder<Resource>, IBuildResource
{
	private readonly object? _state;
	private readonly LinkCollectionBuilder _linkCollectionBuilder;
	private readonly EmbeddedResourceCollectionBuilder _embeddedCollectionBuilder;

	private ResourceBuilder(IBuildHalPart<ResourceCollection>? parent, object? state) : base(parent)
	{
		_state = state;
		_linkCollectionBuilder = LinkCollectionBuilder.New(this);
		_embeddedCollectionBuilder = EmbeddedResourceCollectionBuilder.New(this);
	}

	public static IResource New() => new ResourceBuilder(null, null);
	public static IResource New(object state) => new ResourceBuilder(null, state);
	internal static IEmbeddedResource Embedded() => new ResourceBuilder(null, null);
	internal static IEmbeddedResource Embedded(object state) => new ResourceBuilder(null, state);
	internal static IResource New(IBuildHalPart<ResourceCollection> parent, object? state) => new ResourceBuilder(parent, state);
	internal static IEmbeddedResource Embedded(IBuildHalPart<ResourceCollection> parent, object? state) => new ResourceBuilder(parent, state);

	public ILinkCreationStage AddLink(string rel) => _linkCollectionBuilder.AddLink(rel);
	public ILinkCreationStage AddSelf() => _linkCollectionBuilder.AddSelf();
	public ICuriesLinkCreationStage AddCuries() => _linkCollectionBuilder.AddCuries();
	public IAddResourceStage AddEmbedded(string name) => _embeddedCollectionBuilder.AddEmbedded(name);

	public override Resource BuildPart()
	{
		return new Resource(_state)
		{
			Links = _linkCollectionBuilder.BuildPart(),
			EmbeddedResources = _embeddedCollectionBuilder.BuildPart()
		};
	}

	IEmbeddedLinkCreationStage IAddLinkToEmbeddedStage.AddLink(string rel) => AddLink(rel);
	IEmbeddedLinkCreationStage IAddSelfLinkToEmbeddedStage.AddSelf() => AddSelf();
	IEmbeddedCuriesLinkCreationStage IAddCuriesLinkToEmbeddedStage.AddCuries() => AddCuries();
	IResourceCuriesLinkCreationStage IAddCuriesLinkToResourceStage.AddCuries() => AddCuries();
	IResourceLinkCreationStage IAddLinkToResourceStage.AddLink(string rel) => AddLink(rel);
	IResourceLinkCreationStage IAddSelfLinkToResourceStage.AddSelf() => AddSelf();
}
