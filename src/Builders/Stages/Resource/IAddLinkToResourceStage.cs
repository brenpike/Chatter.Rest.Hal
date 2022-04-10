namespace Chatter.Rest.Hal.Builders.Stages.Resource;

public interface IAddLinkToResourceStage
{
	IResourceLinkCreationStage AddLink(string rel);
}
