namespace Chatter.Rest.Hal.Builders.Stages.Embedded;

public interface IEmbeddedCuriesLinkCreationStage
{
	IEmbeddedLinkObjectPropertiesSelectionStage AddLinkObject(string href, string name);
}
