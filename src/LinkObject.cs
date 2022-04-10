using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal;

/// <summary>
/// A Link Object represents a hyperlink from the containing resource to a URI.
/// </summary>
[JsonConverter(typeof(LinkObjectConverter))]
public sealed record LinkObject : IHalPart
{
	public LinkObject(string href)
	{
		if (string.IsNullOrWhiteSpace(href))
		{
			throw new ArgumentNullException(nameof(href));
		}
		Href = href;
	}

	/// <summary>
	/// The REQUIRED href property of the Link Object as defined by the HAL specification
	/// </summary>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.1
	/// 
	/// 5.1. The "href" property is REQUIRED.
	/// 
	/// Its value is either a URI[RFC3986] or a URI Template[RFC6570].
	/// 
	/// If the value is a URI Template then the Link Object SHOULD have a
	/// "templated" attribute whose value is true.
	/// </remarks>
	public string Href { get; init; }

	/// <summary>
	/// The OPTIONAL templated property of the Link Object as defined by the HAL specification
	/// </summary>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.2
	/// 
	/// 5.2. The "templated" property is OPTIONAL.
	/// 
	/// Its value is boolean and SHOULD be true when the Link Object's "href"
	/// property is a URI Template.
	/// 
	/// Its value SHOULD be considered false if it is undefined or any other
	/// value than true.
	/// </remarks>
	public bool? Templated { get; init; }

	/// <summary>
	/// The OPTIONAL type property of the Link Object as defined by the HAL specification
	/// </summary>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.3
	/// 
	/// 5.3.  type
	/// 
	/// The "type" property is OPTIONAL.
	/// 
	/// Its value is a string used as a hint to indicate the media type
	/// expected when dereferencing the target resource.
	/// </remarks>
	public string? Type { get; init; }

	/// <summary>
	/// The OPTIONAL depreciation property of the Link Object as defined by the HAL specification
	/// </summary>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.4
	/// 
	/// 5.4.  deprecation
	/// 
	/// The "deprecation" property is OPTIONAL.
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
	/// </remarks>
	public string? Deprecation { get; init; }

	/// <summary>
	/// The OPTIONAL name property of the Link Object as defined by the HAL specification
	/// </summary>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.5
	/// 
	/// 5.5.  name
	/// 
	/// The "name" property is OPTIONAL.
	/// 
	/// Its value MAY be used as a secondary key for selecting Link Objects
	/// which share the same relation type.
	/// </remarks>
	public string? Name { get; init; }

	/// <summary>
	/// The OPTIONAL profile property of the Link Object as defined by the HAL specification
	/// </summary>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.6
	/// 
	/// 5.6.  profile
	/// 
	/// The "profile" property is OPTIONAL.
	/// 
	/// Its value is a string which is a URI that hints about the profile(as
	/// defined by [I-D.wilde-profile-link]) of the target resource.
	/// </remarks>
	public string? Profile { get; init; }

	/// <summary>
	/// The OPTIONAL title property of the Link Object as defined by the HAL specification
	/// </summary>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.7
	/// 
	/// 5.7.  title
	/// 
	/// The "title" property is OPTIONAL.
	/// 
	/// Its value is a string and is intended for labelling the link with a
	/// human-readable identifier(as defined by [RFC5988]).
	/// </remarks>
	public string? Title { get; init; }

	/// <summary>
	/// The OPTIONAL hreflang property of the Link Object as defined by the HAL specification
	/// </summary>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-5.8
	/// 
	/// 5.8.  hreflang
	/// 
	/// The "hreflang" property is OPTIONAL.
	/// 
	/// Its value is a string and is intended for indicating the language of
	/// the target resource(as defined by [RFC5988]).
	/// </remarks>
	public string? Hreflang { get; init; }
}
