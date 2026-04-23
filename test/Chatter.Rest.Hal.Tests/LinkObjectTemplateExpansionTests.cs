using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

public class LinkObjectTemplateExpansionTests
{
	[Fact]
	public void GetTemplateVariables_SingleVariable_ReturnsList()
	{
		var lo = new LinkObject("/items/{id}") { Templated = true };

		var variables = lo.GetTemplateVariables();

		variables.Should().Equal("id");
	}

	[Fact]
	public void GetTemplateVariables_MultipleVariables_ReturnsList()
	{
		var lo = new LinkObject("/items/{id}/details/{section}") { Templated = true };

		var variables = lo.GetTemplateVariables();

		variables.Should().Equal("id", "section");
	}

	[Fact]
	public void GetTemplateVariables_DuplicateVariables_ReturnsDistinct()
	{
		var lo = new LinkObject("/items/{id}/related/{id}") { Templated = true };

		var variables = lo.GetTemplateVariables();

		variables.Should().Equal("id");
	}

	[Fact]
	public void GetTemplateVariables_NoVariables_ReturnsEmpty()
	{
		var lo = new LinkObject("/items/42") { Templated = true };

		var variables = lo.GetTemplateVariables();

		variables.Should().BeEmpty();
	}

	[Fact]
	public void GetTemplateVariables_NotTemplated_ReturnsEmpty()
	{
		var lo = new LinkObject("/items/{id}") { Templated = false };

		var variables = lo.GetTemplateVariables();

		variables.Should().BeEmpty();
	}

	[Fact]
	public void GetTemplateVariables_TemplatedNull_ReturnsEmpty()
	{
		var lo = new LinkObject("/items/{id}");

		var variables = lo.GetTemplateVariables();

		variables.Should().BeEmpty();
	}

	[Fact]
	public void GetTemplateVariables_OperatorPrefixed_NotMatched()
	{
		var lo = new LinkObject("/items/{+path}") { Templated = true };

		var variables = lo.GetTemplateVariables();

		variables.Should().BeEmpty();
	}

	[Fact]
	public void Expand_SingleVariable_Substituted()
	{
		var lo = new LinkObject("/items/{id}") { Templated = true };

		var result = lo.Expand(new Dictionary<string, string> { { "id", "42" } });

		result.Should().Be("/items/42");
	}

	[Fact]
	public void Expand_MultipleVariables_AllSubstituted()
	{
		var lo = new LinkObject("/items/{id}/details/{section}") { Templated = true };

		var result = lo.Expand(new Dictionary<string, string>
		{
			{ "id", "42" },
			{ "section", "specs" }
		});

		result.Should().Be("/items/42/details/specs");
	}

	[Fact]
	public void Expand_UnresolvedVariable_LeftAsIs()
	{
		var lo = new LinkObject("/items/{id}/details/{section}") { Templated = true };

		var result = lo.Expand(new Dictionary<string, string> { { "id", "42" } });

		result.Should().Be("/items/42/details/{section}");
	}

	[Fact]
	public void Expand_EmptyDictionary_HrefUnchanged()
	{
		var lo = new LinkObject("/items/{id}") { Templated = true };

		var result = lo.Expand(new Dictionary<string, string>());

		result.Should().Be("/items/{id}");
	}

	[Fact]
	public void Expand_NotTemplated_ReturnsHrefUnchanged()
	{
		var lo = new LinkObject("/items/{id}") { Templated = false };

		var result = lo.Expand(new Dictionary<string, string> { { "id", "42" } });

		result.Should().Be("/items/{id}");
	}

	[Fact]
	public void Expand_TemplatedNull_ReturnsHrefUnchanged()
	{
		var lo = new LinkObject("/items/{id}");

		var result = lo.Expand(new Dictionary<string, string> { { "id", "42" } });

		result.Should().Be("/items/{id}");
	}

	[Fact]
	public void Expand_NullVariables_ThrowsArgumentNullException()
	{
		var lo = new LinkObject("/items/{id}") { Templated = true };

		Action act = () => lo.Expand((IDictionary<string, string>)null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Expand_OperatorPrefixed_LeftUnchanged()
	{
		var lo = new LinkObject("/search{?query,page}") { Templated = true };

		var result = lo.Expand(new Dictionary<string, string>
		{
			{ "query", "test" },
			{ "page", "1" }
		});

		result.Should().Be("/search{?query,page}");
	}

	[Fact]
	public void Expand_MixedLevel1AndOperator_OnlyLevel1Expanded()
	{
		var lo = new LinkObject("/items/{id}{?expand}") { Templated = true };

		var result = lo.Expand(new Dictionary<string, string>
		{
			{ "id", "42" },
			{ "expand", "all" }
		});

		result.Should().Be("/items/42{?expand}");
	}

	[Fact]
	public void Expand_ParamsTuple_SingleVariable_Substituted()
	{
		var lo = new LinkObject("/orders/{id}") { Templated = true };

		var result = lo.Expand(("id", "9001"));

		result.Should().Be("/orders/9001");
	}

	[Fact]
	public void Expand_ParamsTuple_MultipleVariables_AllSubstituted()
	{
		var lo = new LinkObject("/orders/{orderId}/items/{itemId}") { Templated = true };

		var result = lo.Expand(("orderId", "9001"), ("itemId", "3"));

		result.Should().Be("/orders/9001/items/3");
	}

	[Fact]
	public void Expand_ParamsTuple_EmptyParams_HrefUnchanged()
	{
		var lo = new LinkObject("/orders/{id}") { Templated = true };

		var result = lo.Expand();

		result.Should().Be("/orders/{id}");
	}

	[Fact]
	public void Expand_ParamsTuple_UnresolvedVariable_LeftAsIs()
	{
		var lo = new LinkObject("/orders/{orderId}/items/{itemId}") { Templated = true };

		var result = lo.Expand(("orderId", "9001"));

		result.Should().Be("/orders/9001/items/{itemId}");
	}

	[Fact]
	public void Expand_ParamsTuple_NotTemplated_ReturnsHrefUnchanged()
	{
		var lo = new LinkObject("/orders/{id}") { Templated = false };

		var result = lo.Expand(("id", "9001"));

		result.Should().Be("/orders/{id}");
	}
}
