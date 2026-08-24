namespace Chatter.Rest.Hal.Builders.Stages.Embedded;

/// <summary>
/// A stage of the fluent builder from which CURIE <see cref="LinkObject"/>s can be added to the
/// "curies" link of the embedded resource being built
/// </summary>
public interface IEmbeddedCuriesLinkCreationStage
{
	/// <summary>
	/// Adds a CURIE <see cref="LinkObject"/> with the supplied href and name to the "curies" link
	/// </summary>
	/// <param name="href">The REQUIRED href of the CURIE <see cref="LinkObject"/>. A URI Template containing the {rel} token</param>
	/// <param name="name">The REQUIRED name of the CURIE <see cref="LinkObject"/>, used as the prefix of compact link relation types</param>
	/// <returns>A <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/> to continue building the <see cref="LinkObject"/></returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-8.2
	///
	/// 8.2. Custom link relation types can be represented via "CURIE syntax" links: a set of Link
	/// Objects under the reserved "curies" link relation, each with a "name" (the CURIE prefix)
	/// and a templated "href" containing the {rel} token.
	/// </remarks>
	IEmbeddedLinkObjectPropertiesSelectionStage AddLinkObject(string href, string name);
	/// <summary>
	/// Forces the "curies" relation to serialize as a JSON array. No-op for curies since 2.0.0:
	/// the curies relation is array-form by default per HAL section 8.3.
	/// </summary>
	/// <returns>A <see cref="IEmbeddedCuriesLinkCreationStage"/> to continue building the "curies" link</returns>
	IEmbeddedCuriesLinkCreationStage AsArray();
}
