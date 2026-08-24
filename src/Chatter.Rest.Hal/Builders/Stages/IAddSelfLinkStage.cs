namespace Chatter.Rest.Hal.Builders.Stages;

/// <summary>
/// A stage of the fluent builder from which the reserved "self" link relation can be added to the
/// link collection being built. Context-neutral counterpart of
/// <see cref="Resource.IAddSelfLinkToResourceStage"/> and
/// <see cref="Embedded.IAddSelfLinkToEmbeddedStage"/>, implemented by builders that serve both the
/// resource and embedded contexts.
/// </summary>
public interface IAddSelfLinkStage
{
	/// <summary>
	/// Adds a "self" link to the link collection being built. Repeated calls merge into the
	/// single "self" link.
	/// </summary>
	/// <returns>A <see cref="ILinkCreationStage"/> to add <see cref="LinkObject"/>s to the "self" link</returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-4.1.1
	///
	/// 4.1.1. Each Resource Object SHOULD contain a 'self' link that corresponds
	/// with the IANA registered 'self' relation (as defined by [RFC5988])
	/// whose target is the resource's URI.
	/// </remarks>
	ILinkCreationStage AddSelf();
}
