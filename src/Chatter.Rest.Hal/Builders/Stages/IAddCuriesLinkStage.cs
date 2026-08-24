namespace Chatter.Rest.Hal.Builders.Stages;

/// <summary>
/// A stage of the fluent builder from which the reserved "curies" link relation can be added to the
/// link collection being built. Context-neutral counterpart of
/// <see cref="Resource.IAddCuriesLinkToResourceStage"/> and
/// <see cref="Embedded.IAddCuriesLinkToEmbeddedStage"/>, implemented by builders that serve both
/// the resource and embedded contexts.
/// </summary>
public interface IAddCuriesLinkStage
{
	/// <summary>
	/// Adds a "curies" link to the link collection being built. Repeated calls merge into the
	/// single "curies" link, so each additional CURIE definition extends that link's array rather
	/// than emitting a second "curies" entry.
	/// </summary>
	/// <returns>A <see cref="ICuriesLinkCreationStage"/> to add CURIE <see cref="LinkObject"/>s to the "curies" link</returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-8.2
	///
	/// 8.2. Custom link relation types can be represented via "CURIE syntax" links: a set of Link
	/// Objects under the reserved "curies" link relation, each with a "name" (the CURIE prefix)
	/// and a templated "href" containing the {rel} token.
	/// </remarks>
	ICuriesLinkCreationStage AddCuries();
}
