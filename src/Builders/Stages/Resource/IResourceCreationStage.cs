namespace Chatter.Rest.Hal.Builders.Stages.Resource;

public interface IResourceCreationStage : IAddCuriesLinkToResourceStage, IAddLinkToResourceStage, IAddSelfLinkToResourceStage, IBuildResource
{
}
