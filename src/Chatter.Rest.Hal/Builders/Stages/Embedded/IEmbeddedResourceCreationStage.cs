namespace Chatter.Rest.Hal.Builders.Stages.Embedded;

/// <summary>
/// The embedded-context mirror of <see cref="Resource.IResourceCreationStage"/>, returned by the
/// <see cref="Stages.IAddResourceStage"/> methods. What subsequent link/embed calls target depends
/// on which method returned the stage: after <see cref="Stages.IAddResourceStage.AddResource()"/>
/// (or its state overload) they configure the newly added embedded resource; after
/// <see cref="Stages.IAddResourceStage.AddResources{T}"/> the stage is the collection builder, so
/// "self"/link/curies additions target the resource that OWNS the "_embedded" entry and nested
/// embed calls delegate to that owning resource — per-item configuration happens only inside the
/// <c>AddResources</c> callback. The caller can also add sibling resources or build the root
/// resource from this stage.
/// </summary>
public interface IEmbeddedResourceCreationStage : IBuildHalPart<Hal.Resource>, IAddLinkToEmbeddedStage, IAddSelfLinkToEmbeddedStage, IAddCuriesLinkToEmbeddedStage, IAddResourceStage, IAddEmbeddedResourceToResourceStage, IBuildResource
{
}
