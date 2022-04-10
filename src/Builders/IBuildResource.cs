﻿namespace Chatter.Rest.Hal.Builders;

public interface IBuildResource
{
	ILinkCreationStage AddLink(string rel);
	ILinkCreationStage AddSelf();
	ICuriesLinkCreationStage AddCuries();
	IAddResourceToEmbeddedResourceStage AddEmbedded(string name);
}
