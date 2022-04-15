namespace Chatter.Rest.Hal.Builders.Stages.Embedded;

public interface IAddLinkToEmbeddedStage
{
	IEmbeddedLinkCreationStage AddLink(string rel);
}
