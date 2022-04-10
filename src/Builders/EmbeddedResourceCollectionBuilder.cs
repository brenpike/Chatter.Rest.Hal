using Chatter.Rest.Hal.Builders.Stages;

namespace Chatter.Rest.Hal.Builders;

public class EmbeddedResourceCollectionBuilder : HalBuilder<EmbeddedResourceCollection>
{
	private readonly IList<EmbeddedResourceBuilder> _embeddedBuilders = new List<EmbeddedResourceBuilder>();
	
	private EmbeddedResourceCollectionBuilder(IBuildHalPart<Resource> parent) : base(parent) { }

	public static EmbeddedResourceCollectionBuilder New(IBuildHalPart<Resource> parent) => new(parent);

	public IAddResourceStage AddEmbedded(string name)
	{
		var embedded = EmbeddedResourceBuilder.WithName(this, name);
		_embeddedBuilders.Add(embedded);
		return embedded;
	}

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
