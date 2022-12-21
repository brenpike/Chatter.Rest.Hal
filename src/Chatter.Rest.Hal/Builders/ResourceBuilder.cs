using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders;

public class ResourceBuilder : HalBuilder<Resource>, IResourceCreationStage
{
	private readonly object? _state;
	private readonly LinkCollectionBuilder _linkCollectionBuilder;
	private readonly EmbeddedResourceCollectionBuilder _embeddedCollectionBuilder;

	protected ResourceBuilder(IBuildHalPart<ResourceCollection>? parent, object? state) : base(parent)
	{
		_state = state;
		_linkCollectionBuilder = LinkCollectionBuilder.New(this);
		_embeddedCollectionBuilder = EmbeddedResourceCollectionBuilder.New(this);
	}

	public static IResourceCreationStage New() => new ResourceBuilder(null, null);
	public static IResourceCreationStage WithState(object state) => new ResourceBuilder(null, state);

	protected ILinkCreationStage AddLink(string rel) => _linkCollectionBuilder.AddLink(rel);
	protected ILinkCreationStage AddSelf() => _linkCollectionBuilder.AddSelf();
	protected ICuriesLinkCreationStage AddCuries() => _linkCollectionBuilder.AddCuries();

	public IAddResourceStage AddEmbedded(string name) => _embeddedCollectionBuilder.AddEmbedded(name);

	IResourceCuriesLinkCreationStage IAddCuriesLinkToResourceStage.AddCuries() => AddCuries();
	IResourceLinkCreationStage IAddLinkToResourceStage.AddLink(string rel) => AddLink(rel);
	IResourceLinkCreationStage IAddSelfLinkToResourceStage.AddSelf() => AddSelf();

	public override Resource BuildPart()
	{
		return new Resource(_state)
		{
			Links = _linkCollectionBuilder.BuildPart(),
			Embedded = _embeddedCollectionBuilder.BuildPart()
		};
	}
}
