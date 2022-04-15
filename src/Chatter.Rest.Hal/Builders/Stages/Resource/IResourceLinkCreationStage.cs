namespace Chatter.Rest.Hal.Builders.Stages.Resource;

public interface IResourceLinkCreationStage
{
	IResourceLinkObjectPropertiesSelectionStage AddLinkObject(string href);
}
