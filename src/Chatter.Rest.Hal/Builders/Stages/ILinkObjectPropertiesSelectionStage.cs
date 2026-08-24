using Chatter.Rest.Hal.Builders.Stages.Embedded;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders.Stages;

/// <summary>
/// A stage of the fluent builder from which the OPTIONAL HAL properties of the
/// <see cref="LinkObject"/> being built can be set, or the chain re-entered to add further links,
/// curies, or embedded resources. Context-neutral union of
/// <see cref="IResourceLinkObjectPropertiesSelectionStage"/> and
/// <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/>, implemented by builders that serve
/// both the resource and embedded contexts.
/// </summary>
public interface ILinkObjectPropertiesSelectionStage : IResourceLinkObjectPropertiesSelectionStage, IEmbeddedLinkObjectPropertiesSelectionStage, IBuildResource
{
}
