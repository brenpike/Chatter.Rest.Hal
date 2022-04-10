using Chatter.Rest.Hal.Builders.Stages.Embedded;

namespace Chatter.Rest.Hal.Builders.Stages;

public interface IAddResourceStage
{
	IEmbeddedResource AddResource();
	IEmbeddedResource AddResource(object? state);
}
