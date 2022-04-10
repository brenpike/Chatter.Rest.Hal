namespace Chatter.Rest.Hal.Builders.Stages;

public interface ILinkCreationStage
{
	ILinkObjectPropertiesSelectionStage AddLinkObject(string href);
}
