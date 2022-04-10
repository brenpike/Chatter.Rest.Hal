namespace Chatter.Rest.Hal.Builders.Stages;

public interface IAddResourceStage
{
	IBuildResource AddResource();
	IBuildResource AddResource(object? state);
}
