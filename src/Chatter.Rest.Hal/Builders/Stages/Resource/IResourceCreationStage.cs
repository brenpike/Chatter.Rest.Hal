namespace Chatter.Rest.Hal.Builders.Stages.Resource;

/// <summary>
/// The root stage of the fluent builder, returned by the <see cref="Builders.ResourceBuilder"/>
/// entry points. From this stage the caller can add a "self" link, links with arbitrary
/// relations, a "curies" link, or named embedded resource entries, or build the resource.
/// </summary>
public interface IResourceCreationStage : IAddCuriesLinkToResourceStage, IAddLinkToResourceStage, IAddSelfLinkToResourceStage, IAddEmbeddedResourceToResourceStage, IBuildResource
{
}
