using FluentAssertions;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Chatter.Rest.Hal.CodeGenerators.Tests;

public class CodeGeneratorTests
{
	[Fact]
	public void GeneratedProperties_ArePresentOnce_WithCorrectTypes()
	{
		var personType = typeof(Person);
		var linksProp = personType.GetProperty("Links");
		var embeddedProp = personType.GetProperty("Embedded");

		linksProp.Should().NotBeNull();
		embeddedProp.Should().NotBeNull();

		linksProp!.PropertyType.Should().Be(typeof(Chatter.Rest.Hal.LinkCollection));
		embeddedProp!.PropertyType.Should().Be(typeof(Chatter.Rest.Hal.EmbeddedResourceCollection));

		// ensure properties are declared only once on the type (DeclaredOnly)
		var propertyNames = personType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.Select(p => p.Name).ToArray();
		propertyNames.Should().Contain(new[] { "Links", "Embedded" });
	}

	[Fact]
	public void Generator_IsIdempotent_AfterMultipleCompiles()
	{
		var personType = typeof(Person);
		var props = personType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		props.Should().Contain(p => p.Name == "Links");
		props.Should().Contain(p => p.Name == "Embedded");
	}

	[Fact]
	public void Class_Without_HalResponse_Attribute_Is_Not_Modified()
	{
		// HAL spec section 9.4: the source generator should only process classes
		// decorated with [HalResponse]. Classes without the attribute must not
		// have Links or Embedded properties added.
		var plainType = typeof(PlainClass);
		plainType.GetProperty("Links").Should().BeNull(
			"the generator should not add Links to classes without [HalResponse]");
		plainType.GetProperty("Embedded").Should().BeNull(
			"the generator should not add Embedded to classes without [HalResponse]");
	}

	[Fact]
	public void Abstract_Class_With_HalResponse_Gets_Generated_Properties()
	{
		// HAL spec section 9.5: the source generator should handle abstract classes
		// decorated with [HalResponse] and generate Links and Embedded properties.
		var abstractType = typeof(AbstractPersonResponse);
		var linksProp = abstractType.GetProperty("Links");
		var embeddedProp = abstractType.GetProperty("Embedded");
		linksProp.Should().NotBeNull("abstract classes with [HalResponse] should have Links generated");
		embeddedProp.Should().NotBeNull("abstract classes with [HalResponse] should have Embedded generated");
		linksProp!.PropertyType.Should().Be(typeof(Chatter.Rest.Hal.LinkCollection));
		embeddedProp!.PropertyType.Should().Be(typeof(Chatter.Rest.Hal.EmbeddedResourceCollection));
	}
}
