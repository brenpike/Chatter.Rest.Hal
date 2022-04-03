namespace Chatter.Rest.Hal.Builders;

public class LinkObjectBuilder : IBuildLinkObject
{
	private string _href;
	private bool? _templated;
	private string? _type;
	private string? _deprecation;
	private string? _name;
	private string? _profile;
	private string? _title;
	private string? _hreflang;

	private LinkObjectBuilder(string href) => _href = href;

	/// <inheritdoc/>
	public static IBuildLinkObject WithHref(string href) => new LinkObjectBuilder(href);

	/// <inheritdoc/>
	public IBuildLinkObject Templated()
	{
		_templated = true;
		return this;
	}

	/// <inheritdoc/>
	public IBuildLinkObject WithType(string type)
	{
		_type = type;
		return this;
	}

	/// <inheritdoc/>
	public IBuildLinkObject WithDeprecationUrl(string deprecation)
	{
		_deprecation = deprecation;
		return this;
	}

	/// <inheritdoc/>
	public IBuildLinkObject WithName(string name)
	{
		_name = name;
		return this;
	}

	/// <inheritdoc/>
	public IBuildLinkObject WithProfileUri(string profile)
	{
		_profile = profile;
		return this;
	}

	/// <inheritdoc/>
	public IBuildLinkObject WithTitle(string title)
	{
		_title = title;
		return this;
	}

	/// <inheritdoc/>
	public IBuildLinkObject WithHreflang(string hreflang)
	{
		_hreflang = hreflang;
		return this;
	}

	LinkObject IBuildLinkObject.Build()
	{
		return new LinkObject(_href)
		{
			Templated = _templated,
			Deprecation = _deprecation,
			Type = _type,
			Name = _name,
			Profile = _profile,
			Title = _title,
			Hreflang = _hreflang
		};
	}
}
