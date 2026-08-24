namespace Chatter.Rest.Hal.Builders.Stages.Resource;

/// <summary>
/// A stage of the fluent builder from which the reserved "self" link relation can be added to the
/// resource being built
/// </summary>
public interface IAddSelfLinkToResourceStage
{
	/// <summary>
	/// Adds a "self" link to the resource being built. Repeated calls merge into the single
	/// "self" link.
	/// </summary>
	/// <returns>A <see cref="IResourceLinkCreationStage"/> to add <see cref="LinkObject"/>s to the "self" link</returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-4.1.1
	///
	/// 4.1.1. Each Resource Object SHOULD contain a 'self' link that corresponds
	/// with the IANA registered 'self' relation (as defined by [RFC5988])
	/// whose target is the resource's URI.
	/// </remarks>
	IResourceLinkCreationStage AddSelf();
}
