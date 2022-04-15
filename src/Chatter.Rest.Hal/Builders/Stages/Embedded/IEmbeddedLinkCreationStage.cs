namespace Chatter.Rest.Hal.Builders.Stages.Embedded;

public interface IEmbeddedLinkCreationStage
{
	IEmbeddedLinkObjectPropertiesSelectionStage AddLinkObject(string href);
}
