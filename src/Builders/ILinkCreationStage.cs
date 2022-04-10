namespace Chatter.Rest.Hal.Builders;

public interface ILinkCreationStage
{
	ILinkObjectPropertiesSelectionStage AddLinkObject(string href);
}
