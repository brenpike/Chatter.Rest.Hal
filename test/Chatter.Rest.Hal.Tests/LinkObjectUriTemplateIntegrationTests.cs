using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

public class LinkObjectUriTemplateIntegrationTests
{
	[Fact]
	public void Level2_Plus_ExpandsReservedChars()
	{
		var link = new LinkObject("/proxy/{+path}") { Templated = true };

		var result = link.Expand(("path", "foo/bar"));

		result.Should().Be("/proxy/foo/bar");
	}

	[Fact]
	public void Level2_Hash_ExpandsFragment()
	{
		var link = new LinkObject("/page{#section}") { Templated = true };

		var result = link.Expand(("section", "intro"));

		result.Should().Be("/page#intro");
	}

	[Fact]
	public void Level3_Query_BuildsQueryString()
	{
		var link = new LinkObject("/orders{?status,page}") { Templated = true };

		var result = link.Expand(("status", "open"), ("page", "2"));

		result.Should().Be("/orders?status=open&page=2");
	}

	[Fact]
	public void Level3_Slash_BuildsPathSegment()
	{
		var link = new LinkObject("/base{/segment}") { Templated = true };

		var result = link.Expand(("segment", "value"));

		result.Should().Be("/base/value");
	}

	[Fact]
	public void GetTemplateVariables_Level2_ReturnsVars()
	{
		var link = new LinkObject("{+path}") { Templated = true };

		var vars = link.GetTemplateVariables();

		vars.Should().BeEquivalentTo(new[] { "path" });
	}

	[Fact]
	public void GetTemplateVariables_Level3Query_ReturnsVars()
	{
		var link = new LinkObject("{?status,page}") { Templated = true };

		var vars = link.GetTemplateVariables();

		vars.Should().BeEquivalentTo(new[] { "status", "page" });
	}

	[Fact]
	public void NotTemplated_Level2Syntax_Unchanged()
	{
		var link = new LinkObject("{+path}") { Templated = false };

		var result = link.Expand(("path", "/foo"));

		result.Should().Be("{+path}");
	}
}
