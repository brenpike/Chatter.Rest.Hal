namespace Chatter.Rest.Hal.Builders;

public interface IBuildEmbeddedResourceCollection
{
	IAddResourceToEmbeddedResourceStage AddEmbedded(string name);
}
