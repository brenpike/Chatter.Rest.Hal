using System;

namespace Chatter.Rest.Hal.Builders.Stages;

public interface IAddEmbeddedResourceToResourceStage
{
	IAddResourceStage AddEmbedded(string name);
}
