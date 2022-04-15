namespace Chatter.Rest.Hal.Builders.Stages.Embedded;

public interface IEmbeddedLinkObjectPropertiesSelectionStage : IEmbeddedLinkCreationStage, IEmbeddedCuriesLinkCreationStage, IAddLinkToEmbeddedStage, IAddSelfLinkToEmbeddedStage, IAddCuriesLinkToEmbeddedStage, IAddResourceStage, IAddEmbeddedResourceToResourceStage, IBuildResource
{
	/// <summary>
	/// Sets the templated value <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/> to true
	/// </summary>
	/// <returns>A <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/> to continue building a <see cref="LinkObject"/></returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.2
	/// 
	/// 5.2.The "templated" property is OPTIONAL.
	/// 
	/// Its value is boolean and SHOULD be true when the Link Object's "href"
	/// property is a URI Template.
	/// 
	/// Its value SHOULD be considered false if it is undefined or any other
	/// value than true.
	IEmbeddedLinkObjectPropertiesSelectionStage Templated();

	/// <summary>
	/// Sets the media type hint of the <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/>
	/// </summary>
	/// <param name="type">The OPTIONAL media type hint of the <see cref="LinkObject"/></param>
	/// <returns>A <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/> to continue building a <see cref="LinkObject"/></returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.2
	/// 
	/// 5.3. The "type" property is OPTIONAL.
	/// 
	/// Its value is a string used as a hint to indicate the media type
	/// expected when dereferencing the target resource.
	IEmbeddedLinkObjectPropertiesSelectionStage WithType(string type);

	/// <summary>
	/// Sets the depreciation URL of the <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/>
	/// </summary>
	/// <param name="deprecation">The OPTIONAL depreciation URL of the <see cref="LinkObject"/></param>
	/// <returns>A <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/> to continue building a <see cref="LinkObject"/></returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.4
	/// 
	/// 5.4.The "deprecation" property is OPTIONAL.
	/// 
	/// Its presence indicates that the link is to be deprecated(i.e.
	/// removed) at a future date.Its value is a URL that SHOULD provide
	/// further information about the deprecation.
	/// 
	/// A client SHOULD provide some notification (for example, by logging a
	/// warning message) whenever it traverses over a link that has this
	/// property.The notification SHOULD include the deprecation property's
	/// value so that a client manitainer can easily find information about
	/// the deprecation.
	IEmbeddedLinkObjectPropertiesSelectionStage WithDeprecationUrl(string deprecation);

	/// <summary>
	/// Sets the name of the <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/>
	/// </summary>
	/// <param name="name">The OPTIONAL (secondary) name of the <see cref="LinkObject"/></param>
	/// <returns>A <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/> to continue building a <see cref="LinkObject"/></returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.5
	/// 
	/// 5.5 The "name" property is OPTIONAL.
	///
	/// Its value MAY be used as a secondary key for selecting Link Objects
	/// which share the same relation type.</remarks>
	IEmbeddedLinkObjectPropertiesSelectionStage WithName(string name);

	/// <summary>
	/// Sets the profile URI of the <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/>
	/// </summary>
	/// <param name="profile">The OPTIONAL profile URI of the <see cref="LinkObject"/></param>
	/// <returns>A <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/> to continue building a <see cref="LinkObject"/></returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.6
	/// 
	/// 5.6. The "profile" property is OPTIONAL.
	/// 
	/// Its value is a string which is a URI that hints about the profile(as
	/// defined by [I-D.wilde-profile-link]) of the target resource.
	IEmbeddedLinkObjectPropertiesSelectionStage WithProfileUri(string profile);

	/// <summary>
	/// Sets the title of the <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/>
	/// </summary>
	/// <param name="title">The OPTIONAL title of the <see cref="LinkObject"/></param>
	/// <returns>A <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/> to continue building a <see cref="LinkObject"/></returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.7
	/// 
	/// 5.7. The "title" property is OPTIONAL.
	/// 
	/// Its value is a string and is intended for labelling the link with a
	/// human-readable identifier(as defined by [RFC5988]).
	IEmbeddedLinkObjectPropertiesSelectionStage WithTitle(string title);

	/// <summary>
	/// Sets the hreflang of the <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/>
	/// </summary>
	/// <param name="hreflang">The OPTIONAL hreflang of the <see cref="LinkObject"/></param>
	/// <returns>A <see cref="IEmbeddedLinkObjectPropertiesSelectionStage"/> to continue building a <see cref="LinkObject"/></returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.8
	/// 
	/// 5.8. The "hreflang" property is OPTIONAL.
	/// 
	/// Its value is a string and is intended for indicating the language of
	/// the target resource(as defined by [RFC5988]).
	IEmbeddedLinkObjectPropertiesSelectionStage WithHreflang(string hreflang);
}
