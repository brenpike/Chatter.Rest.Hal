using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.UriTemplates.Tests
{
	public class UriTemplateLevel2Tests
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

		// 2.1 Reserved expansion {+var}

		[Fact]
		public void Plus_SimpleValue()
		{
			var template = new UriTemplate("{+var}");
			template.Expand(Variables).Should().Be("value");
		}

		[Fact]
		public void Plus_WithSpace()
		{
			var template = new UriTemplate("{+hello}");
			template.Expand(Variables).Should().Be("Hello%20World!");
		}

		[Fact]
		public void Plus_WithSlashes_PreservesSlashes()
		{
			var template = new UriTemplate("{+path}");
			template.Expand(Variables).Should().Be("/foo/bar");
		}

		[Fact]
		public void Plus_TrailingLiteral()
		{
			var template = new UriTemplate("{+path}/here");
			template.Expand(Variables).Should().Be("/foo/bar/here");
		}

		[Fact]
		public void Plus_InQueryContext()
		{
			var template = new UriTemplate("here?ref={+path}");
			template.Expand(Variables).Should().Be("here?ref=/foo/bar");
		}

		[Fact]
		public void Plus_EmptyValue()
		{
			var template = new UriTemplate("{+empty}");
			template.Expand(Variables).Should().Be("");
		}

		[Fact]
		public void Plus_Undefined()
		{
			var template = new UriTemplate("{+undef}");
			template.Expand(Variables).Should().Be("");
		}

		[Fact]
		public void Plus_MultipleVars()
		{
			var template = new UriTemplate("{+x,hello,y}");
			template.Expand(Variables).Should().Be("1024,Hello%20World!,768");
		}

		[Fact]
		public void Plus_MultipleVarsWithPath()
		{
			var template = new UriTemplate("{+path,x}/here");
			template.Expand(Variables).Should().Be("/foo/bar,1024/here");
		}

		[Fact]
		public void Plus_ReservedCharsPreserved()
		{
			var vars = new Dictionary<string, string> { ["var"] = "value" };
			var template = new UriTemplate("{+var}");
			template.Expand(vars).Should().Be("value");
		}

		[Fact]
		public void Plus_AmpersandPreserved()
		{
			var vars = new Dictionary<string, string> { ["var"] = "a&b" };
			var template = new UriTemplate("{+var}");
			template.Expand(vars).Should().Be("a&b");
		}

		[Fact]
		public void Plus_ColonPreserved()
		{
			var vars = new Dictionary<string, string> { ["var"] = "a:b" };
			var template = new UriTemplate("{+var}");
			template.Expand(vars).Should().Be("a:b");
		}

		// 2.2 Fragment expansion {#var}

		[Fact]
		public void Hash_SimpleValue()
		{
			var template = new UriTemplate("{#var}");
			template.Expand(Variables).Should().Be("#value");
		}

		[Fact]
		public void Hash_WithSpace()
		{
			var template = new UriTemplate("{#hello}");
			template.Expand(Variables).Should().Be("#Hello%20World!");
		}

		[Fact]
		public void Hash_WithSlashes()
		{
			var template = new UriTemplate("{#path}");
			template.Expand(Variables).Should().Be("#/foo/bar");
		}

		[Fact]
		public void Hash_TrailingLiteral()
		{
			var template = new UriTemplate("{#path,x}/here");
			template.Expand(Variables).Should().Be("#/foo/bar,1024/here");
		}

		[Fact]
		public void Hash_MultipleVars()
		{
			var template = new UriTemplate("{#x,hello,y}");
			template.Expand(Variables).Should().Be("#1024,Hello%20World!,768");
		}

		[Fact]
		public void Hash_EmptyValue()
		{
			var template = new UriTemplate("{#empty}");
			template.Expand(Variables).Should().Be("#");
		}

		[Fact]
		public void Hash_Undefined_NoHash()
		{
			var template = new UriTemplate("{#undef}");
			template.Expand(Variables).Should().Be("");
		}
	}
}
