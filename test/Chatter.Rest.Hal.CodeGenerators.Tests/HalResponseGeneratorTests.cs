using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.CodeGenerators.Tests;

public class HalResponseGeneratorTests
{
	[Fact]
	public void GeneratorAddsLinksForFileScopedNamespaces()
	{
		var person = new Person
		{
			Name = "John Doe",
			Age = 42,
			Friends = new[] { "Tom", "Harry" }
		};
		person.Links.Should().BeNull();
		person.Embedded.Should().BeNull();
	}

	[Fact]
	public void GeneratorAddsLinksForScopedNameSpaces()
	{
		var person = new Person2
		{
			Name = "John Doe",
			Age = 42,
			Friends = new[] { "Tom", "Harry" }
		};
		person.Links.Should().BeNull();
		person.Embedded.Should().BeNull();
	}
}