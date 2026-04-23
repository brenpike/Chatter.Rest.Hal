using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Chatter.Rest.Hal.Converters;

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
	/// Parses RFC 6570 Level 1 template variable names from <see cref="Href"/>.
	/// </summary>
	/// <returns>
	/// An ordered, distinct list of variable names found in <c>{variable}</c> tokens.
	/// Returns an empty list when <see cref="Href"/> contains no template variables
	/// or when <see cref="Templated"/> is not <c>true</c>.
	/// </returns>
	/// <remarks>
	/// Only RFC 6570 Level 1 simple string expansion is supported.
	/// Operator-prefixed expressions (<c>{+var}</c>, <c>{#var}</c>, etc.) are not matched.
	/// </remarks>
	public IReadOnlyList<string> GetTemplateVariables()
	{
		if (Templated != true)
		{
			return Array.Empty<string>();
		}

		var matches = Regex.Matches(Href, @"\{([A-Za-z0-9_]+)\}");
		var variables = new List<string>();
		var seen = new HashSet<string>(StringComparer.Ordinal);

		foreach (Match match in matches)
		{
			var name = match.Groups[1].Value;
			if (seen.Add(name))
			{
				variables.Add(name);
			}
		}

		return variables;
	}

	/// <summary>
	/// Performs RFC 6570 Level 1 simple string expansion on the <see cref="Href"/> URI template.
	/// </summary>
	/// <param name="variables">
	/// A dictionary mapping variable names to their substitution values.
	/// Keys are case-sensitive and must match template variable names exactly.
	/// </param>
	/// <returns>
	/// The resolved URI with matched variables substituted. Unresolved variables
	/// (present in the template but absent from <paramref name="variables"/>) are
	/// left as-is (e.g., <c>{id}</c> remains <c>{id}</c>). When <see cref="Templated"/>
	/// is not <c>true</c> or <see cref="Href"/> is null or empty, returns <see cref="Href"/> unchanged.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="variables"/> is null.
	/// </exception>
	/// <remarks>
	/// <para>
	/// This method implements the tolerant reader pattern: unresolved variables are preserved
	/// rather than throwing, allowing partial expansion when only some variables are known.
	/// </para>
	/// <para>
	/// Only RFC 6570 Level 1 simple string expansion is supported. Operator-prefixed
	/// expressions (<c>{+var}</c>, <c>{#var}</c>, etc.) are left unchanged.
	/// </para>
	/// </remarks>
	public string Expand(IDictionary<string, string> variables)
	{
		if (variables == null)
		{
			throw new ArgumentNullException(nameof(variables));
		}

		if (Templated != true || string.IsNullOrEmpty(Href))
		{
			return Href;
		}

		return Regex.Replace(Href, @"\{([A-Za-z0-9_]+)\}", match =>
		{
			var name = match.Groups[1].Value;
			return variables.TryGetValue(name, out var value) ? value : match.Value;
		});
	}
}
