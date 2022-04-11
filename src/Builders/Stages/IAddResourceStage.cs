using Chatter.Rest.Hal.Builders.Stages.Embedded;

namespace Chatter.Rest.Hal.Builders.Stages;

public interface IAddResourceStage
{
	IEmbeddedResourceCreationStage AddResource();
	IEmbeddedResourceCreationStage AddResource(object? state);
}
