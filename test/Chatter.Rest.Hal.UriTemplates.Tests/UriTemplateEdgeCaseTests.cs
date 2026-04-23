using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.UriTemplates.Tests
{
	public class UriTemplateEdgeCaseTests
	{
		private static readonly Dictionary<string, string> Variables = new()
		{
			["var"] = "value",
			["hello"] = "Hello World!",
			["empty"] = "",
			["path"] = "/foo/bar",
			["x"] = "1024",
			["y"] = "768",
		};
		// "undef" is intentionally absent

		// 5.1 Template parsing edge cases

		[Fact]
		public void EmptyTemplate()
		{
			var template = new UriTemplate("");
			template.Expand(Variables).Should().Be("");
		}

		[Fact]
		public void LiteralOnly()
		{
			var template = new UriTemplate("/orders/list");
			template.Expand(Variables).Should().Be("/orders/list");
		}

		[Fact]
		public void ExpressionAtStart()
		{
			var template = new UriTemplate("{var}/rest");
			template.Expand(Variables).Should().Be("value/rest");
		}

		[Fact]
		public void ExpressionAtEnd()
		{
			var template = new UriTemplate("/prefix/{var}");
			template.Expand(Variables).Should().Be("/prefix/value");
		}

		[Fact]
		public void ExpressionOnly()
		{
			var template = new UriTemplate("{var}");
			template.Expand(Variables).Should().Be("value");
		}

		[Fact]
		public void ConsecutiveExpressions()
		{
			var template = new UriTemplate("{x}{y}");
			template.Expand(Variables).Should().Be("1024768");
		}

		[Fact]
		public void MultipleExpressionsWithLiterals()
		{
			var template = new UriTemplate("/a/{x}/b/{y}/c");
			template.Expand(Variables).Should().Be("/a/1024/b/768/c");
		}

		// 5.2 Mixed-level expressions in one template

		[Fact]
		public void Level1AndLevel3Query()
		{
			var vars = new Dictionary<string, string>
			{
				["id"] = "42",
				["status"] = "open",
				["page"] = "2",
			};
			var template = new UriTemplate("/orders/{id}{?status,page}");
			template.Expand(vars).Should().Be("/orders/42?status=open&page=2");
		}

		[Fact]
		public void Level1AndLevel2Reserved()
		{
			var vars = new Dictionary<string, string>
			{
				["path"] = "foo/bar",
			};
			var template = new UriTemplate("/proxy/{+path}/tail");
			template.Expand(vars).Should().Be("/proxy/foo/bar/tail");
		}

		[Fact]
		public void Level2AndLevel3()
		{
			var vars = new Dictionary<string, string>
			{
				["base"] = "/root",
				["segment"] = "value",
			};
			var template = new UriTemplate("{+base}{/segment}");
			template.Expand(vars).Should().Be("/root/value");
		}

		// 5.3 Malformed template handling

		[Fact]
		public void UnclosedBrace_Throws()
		{
			Action act = () => new UriTemplate("/orders/{id");
			act.Should().Throw<FormatException>();
		}

		[Fact]
		public void NestedBraces_Throws()
		{
			Action act = () => new UriTemplate("/orders/{{id}}");
			act.Should().Throw<FormatException>();
		}

		[Fact]
		public void EmptyExpression_Throws()
		{
			Action act = () => new UriTemplate("/orders/{}");
			act.Should().Throw<FormatException>();
		}

		// 5.4 Level 4 detection (not supported)

		[Fact]
		public void PrefixModifier_Throws()
		{
			Action act = () => new UriTemplate("{var:3}");
			act.Should().Throw<NotSupportedException>();
		}

		[Fact]
		public void ExplodeModifier_Throws()
		{
			Action act = () => new UriTemplate("{list*}");
			act.Should().Throw<NotSupportedException>();
		}

		[Fact]
		public void ExplodeWithOperator_Throws()
		{
			Action act = () => new UriTemplate("{/list*}");
			act.Should().Throw<NotSupportedException>();
		}

		// 5.5 Case sensitivity

		[Fact]
		public void VariableNames_CaseSensitive()
		{
			var vars = new Dictionary<string, string>
			{
				["Var"] = "upper",
				["var"] = "lower",
			};
			var template = new UriTemplate("{Var}");
			template.Expand(vars).Should().Be("upper");
		}
	}
}
