namespace Chatter.Rest.Hal.Builders.Stages.Resource;

public interface IResource : IAddCuriesLinkToResourceStage, IAddLinkToResourceStage, IAddSelfLinkToResourceStage, IBuildHal
{
}
