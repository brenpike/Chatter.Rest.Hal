namespace Chatter.Rest.Hal.Builders;

public interface IAddResourcesToCollectionStage
{
	IBuildResource AddResource();
	IBuildResource AddResource(object? state);
}
