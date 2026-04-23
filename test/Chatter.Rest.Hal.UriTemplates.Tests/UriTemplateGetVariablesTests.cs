using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.UriTemplates.Tests
{
	public class UriTemplateGetVariablesTests
	{
		[Fact]
		public void SingleLevel1Var()
		{
			var template = new UriTemplate("{var}");
			template.GetVariables().Should().Equal("var");
		}

		[Fact]
		public void MultipleLevel1Vars()
		{
			var template = new UriTemplate("{x,y}");
			template.GetVariables().Should().Equal("x", "y");
		}

		[Fact]
		public void Level2PlusVar()
		{
			var template = new UriTemplate("{+path}");
			template.GetVariables().Should().Equal("path");
		}

		[Fact]
		public void Level2HashVar()
		{
			var template = new UriTemplate("{#var}");
			template.GetVariables().Should().Equal("var");
		}

		[Fact]
		public void Level3QueryVars()
		{
			var template = new UriTemplate("{?status,page}");
			template.GetVariables().Should().Equal("status", "page");
		}

		[Fact]
		public void MixedExpressions()
		{
			var template = new UriTemplate("/orders/{id}{?status,page}");
			template.GetVariables().Should().Equal("id", "status", "page");
		}

		[Fact]
		public void DeduplicatesAcrossExpressions()
		{
			var template = new UriTemplate("{x}/foo/{x}");
			template.GetVariables().Should().Equal("x");
		}

		[Fact]
		public void NoExpressions()
		{
			var template = new UriTemplate("/literal");
			template.GetVariables().Should().Equal();
		}

		[Fact]
		public void AllOperators()
		{
			var template = new UriTemplate("{a}{+b}{#c}{.d}{/e}{;f}{?g}{&h}");
			template.GetVariables().Should().Equal("a", "b", "c", "d", "e", "f", "g", "h");
		}
	}
}
