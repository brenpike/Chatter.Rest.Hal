namespace Chatter.Rest.Hal.Builders.Stages.Embedded;

/// <summary>
/// A stage of the fluent builder from which <see cref="LinkObject"/>s can be added to the link
/// being built on an embedded resource, or the link's relation forced to serialize as a JSON array
/// </summary>
public interface IEmbeddedLinkCreationStage
{
	/// <summary>
	/// Adds a <see cref="LinkObject"/> with the supplied href to the link being built
	/// </summary>
	/// <param name="href">The REQUIRED href of the <see cref="LinkObject"/>. Either a URI or a URI Template</param>
	/// <returns>A <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/> to continue building the <see cref="LinkObject"/></returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.1
	///
	/// 5.1. The "href" property is REQUIRED.
	///
	/// Its value is either a URI [RFC3986] or a URI Template [RFC6570].
	///
	/// If the value is a URI Template then the Link Object SHOULD have a
	/// "templated" attribute whose value is true.
	/// </remarks>
	IEmbeddedLinkObjectPropertiesSelectionStage AddLinkObject(string href);
	/// <summary>
	/// Forces the link relation being built to serialize as a JSON array even when it contains a
	/// single <see cref="LinkObject"/>
	/// </summary>
	/// <returns>A <see cref="IEmbeddedLinkCreationStage"/> to continue building the link</returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-4.1.1
	///
	/// 4.1.1. The reserved "_links" property values are either a Link Object or an
	/// array of Link Objects.
	/// </remarks>
	IEmbeddedLinkCreationStage AsArray();
}
