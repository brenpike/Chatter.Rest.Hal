namespace Chatter.Rest.Hal.Builders.Stages;

public interface IAddEmbeddedResourceToResourceStage
{
	IAddResourceToEmbeddedResourceStage AddEmbedded(string name);
}
