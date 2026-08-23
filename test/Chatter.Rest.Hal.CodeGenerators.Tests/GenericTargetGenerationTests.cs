using System.Linq;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.CodeGenerators.Tests;

/// <summary>
/// Covers issue #106 (1): the generated partial must repeat the target's type parameter list, so a
/// generic target receives the HAL members instead of an unrelated non-generic type being emitted.
/// </summary>
public class GenericTargetGenerationTests
{
	private const string GenericSource = @"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public partial class GenericResponse<TValue>
{
	public TValue? Value { get; set; }
}
";

	[Fact]
	public void Generic_Target_Generates_Partial_With_Type_Parameters()
	{
		var outcome = GeneratorTestHarness.Run(GenericSource);

		outcome.GeneratedSources.Should().ContainSingle();
		outcome.GeneratedSources.Single().SourceText.ToString()
			.Should().Contain("partial class GenericResponse<TValue>");
	}

	[Fact]
	public void Generic_Target_Does_Not_Emit_A_NonGeneric_Type()
	{
		var outcome = GeneratorTestHarness.Run(GenericSource);

		outcome.GeneratedSources.Single().SourceText.ToString()
			.Should().NotContain("partial class GenericResponse\r\n")
			.And.NotContain("partial class GenericResponse\n");

		outcome.OutputCompilation.GetTypeByMetadataName("TestApp.GenericResponse")
			.Should().BeNull("no arity-0 companion type may be emitted for a generic target");
	}

	[Fact]
	public void Generic_Target_Receives_Hal_Members_And_Compiles()
	{
		var outcome = GeneratorTestHarness.Run(GenericSource);

		outcome.CompilationErrors.Should().BeEmpty();

		var generated = outcome.OutputCompilation.GetTypeByMetadataName("TestApp.GenericResponse`1");
		generated.Should().NotBeNull();
		generated!.GetMembers("Links").Should().NotBeEmpty();
		generated.GetMembers("Embedded").Should().NotBeEmpty();
	}

	[Fact]
	public void Generic_Target_Hint_Name_Encodes_Arity()
	{
		var outcome = GeneratorTestHarness.Run(GenericSource);

		outcome.GeneratedSources.Single().HintName.Should().Be("TestApp.GenericResponse_1.g.cs");
	}

	[Fact]
	public void Same_Name_Different_Arity_Are_Both_Generated()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public partial class Response { }

[HalResponse]
public partial class Response<TValue> { }
";

		var outcome = GeneratorTestHarness.Run(source);

		outcome.CompilationErrors.Should().BeEmpty();
		outcome.GeneratedSources.Select(s => s.HintName).Should()
			.BeEquivalentTo(new[] { "TestApp.Response.g.cs", "TestApp.Response_1.g.cs" });
	}
}
