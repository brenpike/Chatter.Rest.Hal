namespace Chatter.Rest.Hal.Builders.Stages.Embedded;

/// <summary>
/// A stage of the fluent builder from which a link with an arbitrary relation can be added to the
/// embedded resource being built
/// </summary>
public interface IAddLinkToEmbeddedStage
{
	/// <summary>
	/// Adds a link with the supplied relation to the embedded resource being built. Repeating a
	/// relation merges its <see cref="LinkObject"/>s into the single link for that relation,
	/// because HAL's "_links" is a JSON object keyed by relation.
	/// </summary>
	/// <param name="rel">The link relation type of the link. Must not be null or whitespace</param>
	/// <returns>A <see cref="IEmbeddedLinkCreationStage"/> to add <see cref="LinkObject"/>s to the link</returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-4.1.1
	///
	/// 4.1.1. The reserved "_links" property is OPTIONAL.
	///
	/// It is an object whose property names are link relation types (as
	/// defined by [RFC5988]) and values are either a Link Object or an array
	/// of Link Objects.
	/// </remarks>
	IEmbeddedLinkCreationStage AddLink(string rel);
}
