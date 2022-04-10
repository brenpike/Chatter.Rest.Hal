namespace Chatter.Rest.Hal.Builders.Stages.Resource;

public interface IResourceCuriesLinkCreationStage
{
	IResourceLinkObjectPropertiesSelectionStage AddLinkObject(string href, string name);
}
