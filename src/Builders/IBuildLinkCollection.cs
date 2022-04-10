namespace Chatter.Rest.Hal.Builders;

public interface IBuildLinkCollection
{
	ILinkCreationStage AddLink(string rel);
	ILinkCreationStage AddSelf();
	ICuriesLinkCreationStage AddCuries();
}
