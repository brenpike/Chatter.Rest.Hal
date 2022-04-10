namespace Chatter.Rest.Hal.Builders.Stages;

public interface ICuriesLinkCreationStage
{
	ILinkObjectPropertiesSelectionStage AddLinkObject(string href, string name);
}
