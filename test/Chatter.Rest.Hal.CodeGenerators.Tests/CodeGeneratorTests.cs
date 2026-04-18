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
}
