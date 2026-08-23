using System.Linq;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.CodeGenerators.Tests;

/// <summary>
/// Covers issue #109 (2): the marker attribute ships with the generator, so installing the
/// CodeGenerators package is enough for [HalResponse] to resolve and the generator to fire. This
/// test project deliberately references nothing else that declares the attribute.
/// </summary>
public class ShippedAttributeTests
{
	[Fact]
	public void Generator_Emits_The_Marker_Attribute()
	{
		var outcome = GeneratorTestHarness.Run("namespace Empty { }");

		var attributeSource = outcome.AllGeneratedSources
			.Should().ContainSingle(s => s.HintName == "HalResponseAttribute.g.cs").Subject;

		attributeSource.SourceText.ToString().Should()
			.Contain("namespace Chatter.Rest.Hal")
			.And.Contain("class HalResponseAttribute");
	}

	[Fact]
	public void Emitted_Attribute_Is_Enough_For_The_Generator_To_Fire()
	{
		const string source = @"
namespace TestApp;

[global::Chatter.Rest.Hal.HalResponseAttribute]
public partial class SelfContained { }
";

		var outcome = GeneratorTestHarness.Run(source);

		outcome.CompilationErrors.Should().BeEmpty();
		outcome.GeneratedSources.Select(s => s.HintName).Should().Equal("TestApp.SelfContained.g.cs");
	}

	[Fact]
	public void This_Assembly_Uses_The_Emitted_Attribute()
	{
		var attributeType = typeof(Person).GetCustomAttributes(inherit: false)
			.Select(a => a.GetType())
			.Single(t => t.Name == "HalResponseAttribute");

		attributeType.Assembly.FullName.Should().Be(typeof(Person).Assembly.FullName,
			"the attribute is generated into the consuming compilation, not referenced from another assembly");
	}
}
