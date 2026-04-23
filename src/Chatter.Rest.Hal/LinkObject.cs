using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal.Converters;
using Chatter.Rest.Hal.UriTemplates;

namespace Chatter.Rest.Hal;

/// <summary>
/// A Link Object represents a hyperlink from the containing resource to a URI.
/// </summary>
[JsonConverter(typeof(LinkObjectConverter))]
public sealed record LinkObject : IHalPart
{
	/// <summary>
	/// Initializes a new instance of the <see cref="LinkObject"/> record with the specified href.
	/// </summary>
	/// <param name="href">The URI or URI Template for the link. Must not be null or whitespace.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="href"/> is null or whitespace.</exception>
	public LinkObject(string href)
	{
		if (string.IsNullOrWhiteSpace(href))
		{
			throw new ArgumentException("Value cannot be null or whitespace.", nameof(href));
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
	public string Href { get; }

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
	public bool? Templated { get; set; }

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
	public string? Type { get; set; }

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
	public string? Deprecation { get; set; }

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
	public string? Name { get; set; }

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
	public string? Profile { get; set; }

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
	public string? Title { get; set; }

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
	public string? Hreflang { get; set; }

	/// <summary>
	/// Returns all variable names referenced in the URI template, in order of appearance, deduplicated.
	/// Returns an empty list when <see cref="Templated"/> is not true or <see cref="Href"/> is empty.
	/// </summary>
	public IReadOnlyList<string> GetTemplateVariables() =>
		Templated == true && !string.IsNullOrEmpty(Href)
			? new UriTemplate(Href).GetVariables()
			: Array.Empty<string>();

	/// <summary>
	/// Expands the URI template using the provided variable dictionary.
	/// Returns <see cref="Href"/> unchanged when <see cref="Templated"/> is not true.
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="variables"/> is null.</exception>
	public string Expand(IDictionary<string, string> variables)
	{
		if (variables is null) throw new ArgumentNullException(nameof(variables));
		if (Templated != true || string.IsNullOrEmpty(Href)) return Href;
		return new UriTemplate(Href).Expand(variables);
	}

	/// <summary>
	/// Expands the URI template using the provided key-value pairs.
	/// Returns <see cref="Href"/> unchanged when <see cref="Templated"/> is not true.
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="variables"/> is null.</exception>
	public string Expand(params (string Key, string Value)[] variables)
	{
		if (variables is null) throw new ArgumentNullException(nameof(variables));
		var dict = new Dictionary<string, string>();
		foreach (var (key, value) in variables)
			if (!dict.ContainsKey(key))
				dict[key] = value;
		return Expand(dict);
	}
}
