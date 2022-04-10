namespace Chatter.Rest.Hal.Builders;

public interface IEmbeddedResourceCreationStage
{
	IBuildResource AddResource();
	IBuildResource AddResource(object? state);
}
