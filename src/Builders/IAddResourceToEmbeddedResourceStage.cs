namespace Chatter.Rest.Hal.Builders;

public interface IAddResourceToEmbeddedResourceStage
{
	IBuildResource AddResource();
	IBuildResource AddResource(object? state);
}
