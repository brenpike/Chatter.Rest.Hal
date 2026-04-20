using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders;

/// <summary>
/// Builder for constructing HAL resources using a fluent API.
/// </summary>
public class ResourceBuilder : HalBuilder<Resource>, IResourceCreationStage
{
	private readonly object? _state;
	private readonly LinkCollectionBuilder _linkCollectionBuilder;
	private readonly EmbeddedResourceCollectionBuilder _embeddedCollectionBuilder;

	/// <summary>
	/// Initializes a new instance of the <see cref="ResourceBuilder"/> class.
	/// </summary>
	/// <param name="parent">The parent builder, or null if this is the root.</param>
	/// <param name="state">The state object for the resource.</param>
	protected ResourceBuilder(IBuildHalPart<ResourceCollection>? parent, object? state) : base(parent)
	{
		_state = state;
		_linkCollectionBuilder = LinkCollectionBuilder.New(this);
		_embeddedCollectionBuilder = EmbeddedResourceCollectionBuilder.New(this);
	}

	/// <summary>
	/// Creates a new resource builder with no initial state.
	/// </summary>
	/// <returns>A new resource creation stage.</returns>
	public static IResourceCreationStage New() => new ResourceBuilder(null, null);

	/// <summary>
	/// Creates a new resource builder with the specified state object.
	/// </summary>
	/// <param name="state">The state object for the resource.</param>
	/// <returns>A new resource creation stage.</returns>
	public static IResourceCreationStage WithState(object state) => new ResourceBuilder(null, state);

	/// <summary>
	/// Adds a link with the specified relation to the resource.
	/// </summary>
	/// <param name="rel">The link relation.</param>
	/// <returns>A link creation stage.</returns>
	protected ILinkCreationStage AddLink(string rel) => _linkCollectionBuilder.AddLink(rel);

	/// <summary>
	/// Adds a "self" link to the resource.
	/// </summary>
	/// <returns>A link creation stage.</returns>
	protected ILinkCreationStage AddSelf() => _linkCollectionBuilder.AddSelf();

	/// <summary>
	/// Adds a "curies" link to the resource for defining compact URI relations.
	/// </summary>
	/// <returns>A curies link creation stage.</returns>
	protected ICuriesLinkCreationStage AddCuries() => _linkCollectionBuilder.AddCuries();

	/// <summary>
	/// Adds an embedded resource with the specified name to the resource.
	/// </summary>
	/// <param name="name">The name of the embedded resource.</param>
	/// <returns>A resource addition stage.</returns>
	public IAddResourceStage AddEmbedded(string name) => _embeddedCollectionBuilder.AddEmbedded(name);

	IResourceCuriesLinkCreationStage IAddCuriesLinkToResourceStage.AddCuries() => AddCuries();
	IResourceLinkCreationStage IAddLinkToResourceStage.AddLink(string rel) => AddLink(rel);
	IResourceLinkCreationStage IAddSelfLinkToResourceStage.AddSelf() => AddSelf();

	/// <summary>
	/// Builds the Resource with its state, links, and embedded resources.
	/// </summary>
	/// <returns>The constructed Resource.</returns>
	public override Resource BuildPart()
	{
		return new Resource(_state)
		{
			Links = _linkCollectionBuilder.BuildPart(),
			Embedded = _embeddedCollectionBuilder.BuildPart()
		};
	}

}
