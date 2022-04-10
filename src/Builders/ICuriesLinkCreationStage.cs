namespace Chatter.Rest.Hal.Builders;

public interface ICuriesLinkCreationStage
{
	ILinkObjectPropertiesSelectionStage AddLinkObject(string href, string name);
}
