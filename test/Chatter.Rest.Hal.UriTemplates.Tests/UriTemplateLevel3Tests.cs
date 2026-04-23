using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.UriTemplates.Tests
{
	public class UriTemplateLevel3Tests
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

		// 3.1 No-operator multi-variable {x,y}

		[Fact]
		public void NoOp_TwoVars()
		{
			var template = new UriTemplate("{x,y}");
			template.Expand(Variables).Should().Be("1024,768");
		}

		[Fact]
		public void NoOp_ThreeVars()
		{
			var template = new UriTemplate("{x,hello,y}");
			template.Expand(Variables).Should().Be("1024,Hello%20World%21,768");
		}

		[Fact]
		public void NoOp_WithUndefined_OmitsUndefined()
		{
			var template = new UriTemplate("{x,undef,y}");
			template.Expand(Variables).Should().Be("1024,768");
		}

		[Fact]
		public void NoOp_AllUndefined()
		{
			var template = new UriTemplate("{undef,undef}");
			template.Expand(Variables).Should().Be("");
		}

		[Fact]
		public void NoOp_WithEmpty()
		{
			var template = new UriTemplate("{x,empty,y}");
			template.Expand(Variables).Should().Be("1024,,768");
		}

		// 3.2 Reserved multi-variable {+x,y}

		[Fact]
		public void Plus_TwoVars()
		{
			var template = new UriTemplate("{+x,hello,y}");
			template.Expand(Variables).Should().Be("1024,Hello%20World!,768");
		}

		[Fact]
		public void Plus_WithPath()
		{
			var template = new UriTemplate("{+path,x}/here");
			template.Expand(Variables).Should().Be("/foo/bar,1024/here");
		}

		// 3.3 Fragment multi-variable {#x,y}

		[Fact]
		public void Hash_TwoVars()
		{
			var template = new UriTemplate("{#x,hello,y}");
			template.Expand(Variables).Should().Be("#1024,Hello%20World!,768");
		}

		// 3.4 Label expansion {.var}

		[Fact]
		public void Dot_SingleVar()
		{
			var template = new UriTemplate("{.var}");
			template.Expand(Variables).Should().Be(".value");
		}

		[Fact]
		public void Dot_TwoVars()
		{
			var template = new UriTemplate("{.x,y}");
			template.Expand(Variables).Should().Be(".1024.768");
		}

		[Fact]
		public void Dot_Undefined()
		{
			var template = new UriTemplate("{.undef}");
			template.Expand(Variables).Should().Be("");
		}

		[Fact]
		public void Dot_EmptyValue()
		{
			var template = new UriTemplate("{.empty}");
			template.Expand(Variables).Should().Be(".");
		}

		[Fact]
		public void Dot_InPath()
		{
			var vars = new Dictionary<string, string>(Variables)
			{
				["format"] = "value",
			};
			var template = new UriTemplate("/api{.format}");
			template.Expand(vars).Should().Be("/api.value");
		}

		[Fact]
		public void Dot_MixedDefinedUndefined()
		{
			var template = new UriTemplate("{.x,undef,y}");
			template.Expand(Variables).Should().Be(".1024.768");
		}

		// 3.5 Path segment expansion {/var}

		[Fact]
		public void Slash_SingleVar()
		{
			var template = new UriTemplate("{/var}");
			template.Expand(Variables).Should().Be("/value");
		}

		[Fact]
		public void Slash_TwoVars()
		{
			var template = new UriTemplate("{/var,x}");
			template.Expand(Variables).Should().Be("/value/1024");
		}

		[Fact]
		public void Slash_Undefined()
		{
			var template = new UriTemplate("{/undef}");
			template.Expand(Variables).Should().Be("");
		}

		[Fact]
		public void Slash_EmptyValue()
		{
			var template = new UriTemplate("{/empty}");
			template.Expand(Variables).Should().Be("/");
		}

		[Fact]
		public void Slash_InPath()
		{
			var template = new UriTemplate("/base{/var}");
			template.Expand(Variables).Should().Be("/base/value");
		}

		[Fact]
		public void Slash_MixedDefinedUndefined()
		{
			var template = new UriTemplate("{/x,undef,y}");
			template.Expand(Variables).Should().Be("/1024/768");
		}

		// 3.6 Path-style parameter expansion {;var}

		[Fact]
		public void Semicolon_SingleVar()
		{
			var template = new UriTemplate("{;x}");
			template.Expand(Variables).Should().Be(";x=1024");
		}

		[Fact]
		public void Semicolon_MultipleVars()
		{
			var template = new UriTemplate("{;x,y,empty}");
			template.Expand(Variables).Should().Be(";x=1024;y=768;empty");
		}

		[Fact]
		public void Semicolon_EmptyValue_NoEquals()
		{
			var template = new UriTemplate("{;empty}");
			template.Expand(Variables).Should().Be(";empty");
		}

		[Fact]
		public void Semicolon_Undefined_Omitted()
		{
			var template = new UriTemplate("{;undef}");
			template.Expand(Variables).Should().Be("");
		}

		[Fact]
		public void Semicolon_MixedDefinedUndefined()
		{
			var template = new UriTemplate("{;x,undef,y}");
			template.Expand(Variables).Should().Be(";x=1024;y=768");
		}

		// 3.7 Query string expansion {?var}

		[Fact]
		public void Query_SingleVar()
		{
			var template = new UriTemplate("{?x}");
			template.Expand(Variables).Should().Be("?x=1024");
		}

		[Fact]
		public void Query_TwoVars()
		{
			var template = new UriTemplate("{?x,y}");
			template.Expand(Variables).Should().Be("?x=1024&y=768");
		}

		[Fact]
		public void Query_WithEmpty()
		{
			var template = new UriTemplate("{?x,y,empty}");
			template.Expand(Variables).Should().Be("?x=1024&y=768&empty=");
		}

		[Fact]
		public void Query_WithUndefined_OmitsUndefined()
		{
			var template = new UriTemplate("{?x,y,undef}");
			template.Expand(Variables).Should().Be("?x=1024&y=768");
		}

		[Fact]
		public void Query_Undefined_NoPrefix()
		{
			var template = new UriTemplate("{?undef}");
			template.Expand(Variables).Should().Be("");
		}

		[Fact]
		public void Query_EmptyOnly()
		{
			var template = new UriTemplate("{?empty}");
			template.Expand(Variables).Should().Be("?empty=");
		}

		[Fact]
		public void Query_MixedDefinedUndefined()
		{
			var template = new UriTemplate("{?x,undef,y}");
			template.Expand(Variables).Should().Be("?x=1024&y=768");
		}

		[Fact]
		public void Query_InFullPath()
		{
			var template = new UriTemplate("/orders{?x,y}");
			template.Expand(Variables).Should().Be("/orders?x=1024&y=768");
		}

		// 3.8 Query continuation expansion {&var}

		[Fact]
		public void Ampersand_SingleVar()
		{
			var template = new UriTemplate("{&x}");
			template.Expand(Variables).Should().Be("&x=1024");
		}

		[Fact]
		public void Ampersand_TwoVars()
		{
			var template = new UriTemplate("{&x,y}");
			template.Expand(Variables).Should().Be("&x=1024&y=768");
		}

		[Fact]
		public void Ampersand_WithEmpty()
		{
			var template = new UriTemplate("{&x,y,empty}");
			template.Expand(Variables).Should().Be("&x=1024&y=768&empty=");
		}

		[Fact]
		public void Ampersand_WithUndefined_OmitsUndefined()
		{
			var template = new UriTemplate("{&x,y,undef}");
			template.Expand(Variables).Should().Be("&x=1024&y=768");
		}

		[Fact]
		public void Ampersand_Undefined_NoPrefix()
		{
			var template = new UriTemplate("{&undef}");
			template.Expand(Variables).Should().Be("");
		}

		[Fact]
		public void Ampersand_InQueryString()
		{
			var template = new UriTemplate("/orders?sort=date{&x,y}");
			template.Expand(Variables).Should().Be("/orders?sort=date&x=1024&y=768");
		}
	}
}
