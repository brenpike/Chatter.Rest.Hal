namespace Chatter.Rest.Hal.Builders.Stages;

public interface IAddLinkStage
{
	ILinkCreationStage AddLink(string rel);
}
