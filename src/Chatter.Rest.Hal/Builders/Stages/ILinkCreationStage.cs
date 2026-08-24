using Chatter.Rest.Hal.Builders.Stages.Embedded;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders.Stages;

/// <summary>
/// A stage of the fluent builder from which <see cref="LinkObject"/>s can be added to the link
/// being built, or the link's relation forced to serialize as a JSON array. Context-neutral union
/// of <see cref="IResourceLinkCreationStage"/> and <see cref="IEmbeddedLinkCreationStage"/>,
/// implemented by builders that serve both the resource and embedded contexts.
/// </summary>
public interface ILinkCreationStage : IResourceLinkCreationStage, IEmbeddedLinkCreationStage
{
}
