namespace Chatter.Rest.Hal.Builders.Stages.Embedded;

public interface IEmbeddedResourceCreationStage : IBuildHalPart<Hal.Resource>, IAddLinkToEmbeddedStage, IAddSelfLinkToEmbeddedStage, IAddCuriesLinkToEmbeddedStage, IAddResourceStage, IAddEmbeddedResourceToResourceStage, IBuildResource
{
}
