using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.UriTemplates.Tests
{
	public class UriTemplateLevel1Tests
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

		// 1.1 RFC canonical examples

		[Fact]
		public void SingleVar_SimpleValue()
		{
			var template = new UriTemplate("{var}");
			template.Expand(Variables).Should().Be("value");
		}

		[Fact]
		public void SingleVar_WithSpaceAndBang()
		{
			var template = new UriTemplate("{hello}");
			template.Expand(Variables).Should().Be("Hello%20World%21");
		}

		[Fact]
		public void SingleVar_EmptyValue()
		{
			var template = new UriTemplate("{empty}");
			template.Expand(Variables).Should().Be("");
		}

		[Fact]
		public void SingleVar_Undefined()
		{
			var template = new UriTemplate("{undef}");
			template.Expand(Variables).Should().Be("");
		}

		[Fact]
		public void SingleVar_WithSlashes()
		{
			var template = new UriTemplate("{path}");
			template.Expand(Variables).Should().Be("%2Ffoo%2Fbar");
		}

		// 1.2 Literal text preservation

		[Fact]
		public void NoExpression_LiteralOnly()
		{
			var template = new UriTemplate("/orders/list");
			template.Expand(Variables).Should().Be("/orders/list");
		}

		[Fact]
		public void LeadingLiteral()
		{
			var template = new UriTemplate("/orders/{var}");
			template.Expand(Variables).Should().Be("/orders/value");
		}

		[Fact]
		public void TrailingLiteral()
		{
			var template = new UriTemplate("{var}/orders");
			template.Expand(Variables).Should().Be("value/orders");
		}

		[Fact]
		public void MiddleLiteral()
		{
			var template = new UriTemplate("/orders/{var}/items");
			template.Expand(Variables).Should().Be("/orders/value/items");
		}

		[Fact]
		public void EmptyTemplate()
		{
			var template = new UriTemplate("");
			template.Expand(Variables).Should().Be("");
		}

		// 1.3 Multiple expressions

		[Fact]
		public void TwoExpressions()
		{
			var template = new UriTemplate("/orders/{x}/items/{y}");
			template.Expand(Variables).Should().Be("/orders/1024/items/768");
		}

		[Fact]
		public void ConsecutiveExpressions()
		{
			var template = new UriTemplate("{x}{y}");
			template.Expand(Variables).Should().Be("1024768");
		}

		// 1.4 Encoding edge cases

		[Fact]
		public void Encoding_SpaceEncoded()
		{
			var template = new UriTemplate("{hello}");
			template.Expand(Variables).Should().Be("Hello%20World%21");
		}

		[Fact]
		public void Encoding_SlashEncoded()
		{
			var template = new UriTemplate("{path}");
			template.Expand(Variables).Should().Be("%2Ffoo%2Fbar");
		}

		[Fact]
		public void Encoding_TildeNotEncoded()
		{
			var vars = new Dictionary<string, string> { ["var"] = "val~ue" };
			var template = new UriTemplate("{var}");
			template.Expand(vars).Should().Be("val~ue");
		}

		[Fact]
		public void Encoding_HyphenNotEncoded()
		{
			var vars = new Dictionary<string, string> { ["var"] = "val-ue" };
			var template = new UriTemplate("{var}");
			template.Expand(vars).Should().Be("val-ue");
		}

		[Fact]
		public void Encoding_DotNotEncoded()
		{
			var vars = new Dictionary<string, string> { ["var"] = "val.ue" };
			var template = new UriTemplate("{var}");
			template.Expand(vars).Should().Be("val.ue");
		}

		[Fact]
		public void Encoding_UnderscoreNotEncoded()
		{
			var vars = new Dictionary<string, string> { ["var"] = "val_ue" };
			var template = new UriTemplate("{var}");
			template.Expand(vars).Should().Be("val_ue");
		}

		[Fact]
		public void Encoding_ReservedColonEncoded()
		{
			var vars = new Dictionary<string, string> { ["var"] = "val:ue" };
			var template = new UriTemplate("{var}");
			template.Expand(vars).Should().Be("val%3Aue");
		}

		[Fact]
		public void Encoding_ReservedAmpersandEncoded()
		{
			var vars = new Dictionary<string, string> { ["var"] = "a&b" };
			var template = new UriTemplate("{var}");
			template.Expand(vars).Should().Be("a%26b");
		}

		// 1.5 Guard conditions

		[Fact]
		public void NullDictionary_Throws()
		{
			var template = new UriTemplate("{var}");
			Action act = () => template.Expand((IDictionary<string, string>)null!);
			act.Should().Throw<ArgumentNullException>();
		}

		[Fact]
		public void EmptyDictionary_AllExpressionsEmpty()
		{
			var template = new UriTemplate("{var}");
			template.Expand(new Dictionary<string, string>()).Should().Be("");
		}
	}
}
