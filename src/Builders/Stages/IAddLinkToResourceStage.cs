namespace Chatter.Rest.Hal.Builders.Stages;

public interface IAddLinkToResourceStage
{
	ILinkCreationStage AddLink(string rel);
}
