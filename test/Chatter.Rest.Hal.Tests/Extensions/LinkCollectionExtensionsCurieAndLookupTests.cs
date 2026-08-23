using System;
using Chatter.Rest.Hal.Builders;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Extensions;

/// <summary>
/// Regression coverage for issue #105: SingleOrDefault threw on legal input, CURIE expansion
/// substituted the reference unescaped, and builder arguments were validated far too late.
/// </summary>
public class LinkCollectionExtensionsCurieAndLookupTests
{
	private static LinkCollection CurieCollection(params LinkObject[] definitions)
	{
		var curies = new Link("curies");
		foreach (var definition in definitions)
		{
			curies.LinkObjects.Add(definition);
		}

		return new LinkCollection { curies };
	}

	private static LinkObject Curie(string name, string href = "https://docs.acme.com/relations/{rel}", bool? templated = true)
		=> new LinkObject(href) { Name = name, Templated = templated };

	[Fact]
	public void GetLinkObjectOrDefault_Returns_The_First_Object_When_A_Rel_Carries_Several()
	{
		// Verified failing case from #105: the documented "gets the first link object" behaviour
		// used SingleOrDefault and threw for any rel legitimately carrying multiple link objects.
		var link = new Link("admins");
		link.LinkObjects.Add(new LinkObject("/admins/2") { Title = "Fred" });
		link.LinkObjects.Add(new LinkObject("/admins/5") { Title = "Kate" });

		var links = new LinkCollection { link };

		var result = links.GetLinkObjectOrDefault("admins");

		result.Should().NotBeNull();
		result!.Href.Should().Be("/admins/2");
	}

	[Fact]
	public void GetLinkObjectOrDefault_Returns_Null_For_An_Unknown_Rel()
	{
		var links = new LinkCollection { new Link("self") { LinkObjects = { new LinkObject("/self") } } };

		links.GetLinkObjectOrDefault("nope").Should().BeNull();
	}

	[Fact]
	public void ExpandCurieRelation_Searches_Every_Curies_Link()
	{
		// Two "curies" links are reachable in a hand-built collection; the lookup must not throw
		// and must find definitions in either of them.
		var links = CurieCollection(Curie("acme"));
		var second = new Link("curies");
		second.LinkObjects.Add(Curie("other", "https://docs.other.com/rel/{rel}"));
		links.Add(second);

		links.ExpandCurieRelation("acme:widgets").Should().Be("https://docs.acme.com/relations/widgets");
		links.ExpandCurieRelation("other:widgets").Should().Be("https://docs.other.com/rel/widgets");
	}

	[Fact]
	public void ExpandCurieRelation_Does_Not_Throw_When_Two_Curies_Links_Exist()
	{
		var links = CurieCollection(Curie("acme"));
		links.Add(new Link("curies"));

		Action act = () => links.ExpandCurieRelation("unknown:widgets");

		act.Should().NotThrow();
		links.ExpandCurieRelation("unknown:widgets").Should().Be("unknown:widgets");
	}

	[Theory]
	[InlineData("acme:x y#z", "https://docs.acme.com/relations/x%20y%23z")]
	[InlineData("acme:a/b", "https://docs.acme.com/relations/a%2Fb")]
	[InlineData("acme:a?b=c", "https://docs.acme.com/relations/a%3Fb%3Dc")]
	[InlineData("acme:bar:baz", "https://docs.acme.com/relations/bar%3Abaz")]
	[InlineData("acme:plain-rel_1.0~x", "https://docs.acme.com/relations/plain-rel_1.0~x")]
	public void ExpandCurieRelation_Percent_Encodes_The_Reference(string relation, string expected)
	{
		// RFC 6570 simple string expansion percent-encodes everything outside the unreserved set.
		var links = CurieCollection(Curie("acme"));

		links.ExpandCurieRelation(relation).Should().Be(expected);
	}

	[Fact]
	public void ExpandCurieRelation_Returns_The_Original_Relation_For_An_Empty_Reference()
	{
		var links = CurieCollection(Curie("acme"));

		links.ExpandCurieRelation("acme:").Should().Be("acme:");
	}

	[Fact]
	public void ExpandCurieRelation_Returns_The_Original_Relation_When_The_Definition_Is_Not_Templated()
	{
		// HAL section 8.2: a CURIE href is a URI Template and SHOULD be marked templated. An href
		// that is not marked templated cannot be expanded.
		var links = CurieCollection(Curie("acme", templated: null));

		links.ExpandCurieRelation("acme:widgets").Should().Be("acme:widgets");

		var explicitlyFalse = CurieCollection(Curie("acme", templated: false));
		explicitlyFalse.ExpandCurieRelation("acme:widgets").Should().Be("acme:widgets");
	}

	[Fact]
	public void ExpandCurieRelation_Returns_The_Original_Relation_When_The_Template_Lacks_The_Rel_Token()
	{
		var links = CurieCollection(Curie("acme", "https://docs.acme.com/relations/static"));

		links.ExpandCurieRelation("acme:widgets").Should().Be("acme:widgets");
	}

	[Fact]
	public void ExpandCurieRelation_Matches_The_Prefix_Case_Sensitively()
	{
		var links = CurieCollection(Curie("acme"));

		links.ExpandCurieRelation("ACME:widgets").Should().Be("ACME:widgets");
	}

	[Fact]
	public void ExpandCurieRelation_Expands_A_Curie_Defined_Through_The_Builder()
	{
		var resource = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/orders")
			.AddCuries().AddLinkObject("http://example.com/docs/{rel}", "ex")
			.Build();

		resource!.Links.ExpandCurieRelation("ex:widgets").Should().Be("http://example.com/docs/widgets");
	}

	[Fact]
	public void AddLink_Rejects_A_Null_Or_Whitespace_Rel_At_The_Call_Site()
	{
		// Verified failing case from #105: these used to be accepted and blew up inside Build().
		Action nullRel = () => ResourceBuilder.New().AddLink(null!);
		Action emptyRel = () => ResourceBuilder.New().AddLink(string.Empty);
		Action whitespaceRel = () => ResourceBuilder.New().AddLink("   ");

		nullRel.Should().Throw<ArgumentException>().And.ParamName.Should().Be("rel");
		emptyRel.Should().Throw<ArgumentException>().And.ParamName.Should().Be("rel");
		whitespaceRel.Should().Throw<ArgumentException>().And.ParamName.Should().Be("rel");
	}

	[Fact]
	public void AddLinkObject_Rejects_A_Null_Or_Whitespace_Href_At_The_Call_Site()
	{
		Action nullHref = () => ResourceBuilder.New().AddLink("next").AddLinkObject(null!);
		Action emptyHref = () => ResourceBuilder.New().AddLink("next").AddLinkObject(string.Empty);
		Action whitespaceHref = () => ResourceBuilder.New().AddLink("next").AddLinkObject("   ");

		nullHref.Should().Throw<ArgumentException>().And.ParamName.Should().Be("href");
		emptyHref.Should().Throw<ArgumentException>().And.ParamName.Should().Be("href");
		whitespaceHref.Should().Throw<ArgumentException>().And.ParamName.Should().Be("href");
	}

	[Fact]
	public void AddCuries_LinkObject_Rejects_A_Null_Or_Whitespace_Href_Or_Name_At_The_Call_Site()
	{
		Action nullHref = () => ResourceBuilder.New().AddCuries().AddLinkObject(null!, "ex");
		Action nullName = () => ResourceBuilder.New().AddCuries().AddLinkObject("http://example.com/docs/{rel}", null!);
		Action whitespaceName = () => ResourceBuilder.New().AddCuries().AddLinkObject("http://example.com/docs/{rel}", "  ");

		nullHref.Should().Throw<ArgumentException>().And.ParamName.Should().Be("href");
		nullName.Should().Throw<ArgumentException>().And.ParamName.Should().Be("name");
		whitespaceName.Should().Throw<ArgumentException>().And.ParamName.Should().Be("name");
	}
}
