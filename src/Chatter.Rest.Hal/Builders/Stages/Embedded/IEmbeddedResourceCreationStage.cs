namespace Chatter.Rest.Hal.Builders.Stages.Embedded;

/// <summary>
/// The embedded-context mirror of <see cref="Resource.IResourceCreationStage"/>, returned by the
/// <see cref="Stages.IAddResourceStage"/> methods. From this stage the caller can add a "self"
/// link, links with arbitrary relations, a "curies" link, sibling resources, or nested embedded
/// resource entries, or build the root resource.
/// </summary>
public interface IEmbeddedResourceCreationStage : IBuildHalPart<Hal.Resource>, IAddLinkToEmbeddedStage, IAddSelfLinkToEmbeddedStage, IAddCuriesLinkToEmbeddedStage, IAddResourceStage, IAddEmbeddedResourceToResourceStage, IBuildResource
{
}
