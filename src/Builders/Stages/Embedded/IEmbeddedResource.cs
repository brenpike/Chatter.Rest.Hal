namespace Chatter.Rest.Hal.Builders.Stages.Embedded;

public interface IEmbeddedResource : IBuildHalPart<Hal.Resource>, IAddLinkToEmbeddedStage, IAddSelfLinkToEmbeddedStage, IAddCuriesLinkToEmbeddedStage, IBuildHal
{
}
