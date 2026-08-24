using Chatter.Rest.Hal.Builders.Stages.Embedded;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders.Stages;

/// <summary>
/// A stage of the fluent builder from which CURIE <see cref="LinkObject"/>s can be added to the
/// "curies" link being built. Context-neutral union of
/// <see cref="IResourceCuriesLinkCreationStage"/> and <see cref="IEmbeddedCuriesLinkCreationStage"/>,
/// implemented by builders that serve both the resource and embedded contexts.
/// </summary>
public interface ICuriesLinkCreationStage : IResourceCuriesLinkCreationStage, IEmbeddedCuriesLinkCreationStage
{
}
