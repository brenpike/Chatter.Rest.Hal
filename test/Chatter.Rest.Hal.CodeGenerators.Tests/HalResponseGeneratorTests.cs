using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.CodeGenerators.Tests;

public class HalResponseGeneratorTests
{
	[Fact]
	public void GeneratorAddsLinks()
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
}