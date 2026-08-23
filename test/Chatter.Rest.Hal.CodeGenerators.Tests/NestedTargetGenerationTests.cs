using System.Linq;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.CodeGenerators.Tests;

/// <summary>
/// Covers issue #106 (2) and (3): the generated source must re-declare the full containing-type
/// chain instead of emitting a stray top-level type, and two identically named types in different
/// containing types must both survive deduplication.
/// </summary>
public class NestedTargetGenerationTests
{
	private const string NestedSource = @"
using Chatter.Rest.Hal;

namespace TestApp;

public partial class Outer
{
	[HalResponse]
	public partial class Inner { }
}
";

	[Fact]
	public void Nested_Target_Generates_Nested_Partial_Chain()
	{
		var outcome = GeneratorTestHarness.Run(NestedSource);

		var source = outcome.GeneratedSources.Single().SourceText.ToString();
		source.Should().Contain("partial class Outer");
		source.Should().Contain("partial class Inner");
		source.IndexOf("partial class Outer", System.StringComparison.Ordinal)
			.Should().BeLessThan(source.IndexOf("partial class Inner", System.StringComparison.Ordinal));
	}

	[Fact]
	public void Nested_Target_Receives_Hal_Members_And_Emits_No_TopLevel_Type()
	{
		var outcome = GeneratorTestHarness.Run(NestedSource);

		outcome.CompilationErrors.Should().BeEmpty();

		var inner = outcome.OutputCompilation.GetTypeByMetadataName("TestApp.Outer+Inner");
		inner.Should().NotBeNull();
		inner!.GetMembers("Links").Should().NotBeEmpty();
		inner.GetMembers("Embedded").Should().NotBeEmpty();

		outcome.OutputCompilation.GetTypeByMetadataName("TestApp.Inner")
			.Should().BeNull("the generator must not emit a stray top-level companion for a nested target");
	}

	[Fact]
	public void Nested_Target_Repeats_Generic_Containing_Type_Parameters()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

public partial class Outer<TOuter>
{
	[HalResponse]
	public partial class Inner { }
}
";

		var outcome = GeneratorTestHarness.Run(source);

		outcome.CompilationErrors.Should().BeEmpty();
		outcome.GeneratedSources.Single().SourceText.ToString()
			.Should().Contain("partial class Outer<TOuter>");
		outcome.GeneratedSources.Single().HintName.Should().Be("TestApp.Outer_1.Inner.g.cs");
	}

	[Fact]
	public void Nested_Target_Repeats_Containing_Record_Keyword()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

public partial record Outer
{
	[HalResponse]
	public partial class Inner { }
}
";

		var outcome = GeneratorTestHarness.Run(source);

		outcome.CompilationErrors.Should().BeEmpty();
		outcome.GeneratedSources.Single().SourceText.ToString()
			.Should().Contain("partial record Outer");
	}

	[Fact]
	public void Deeply_Nested_Target_Repeats_Every_Containing_Type()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

public partial class Level1
{
	public partial class Level2
	{
		[HalResponse]
		public partial class Level3 { }
	}
}
";

		var outcome = GeneratorTestHarness.Run(source);

		outcome.CompilationErrors.Should().BeEmpty();
		outcome.OutputCompilation.GetTypeByMetadataName("TestApp.Level1+Level2+Level3")!
			.GetMembers("Links").Should().NotBeEmpty();
		outcome.GeneratedSources.Single().HintName.Should().Be("TestApp.Level1.Level2.Level3.g.cs");
	}

	[Fact]
	public void Same_Name_In_Different_Containing_Types_Are_Both_Generated()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

public partial class ContainerA
{
	[HalResponse]
	public partial class Inner { }
}

public partial class ContainerB
{
	[HalResponse]
	public partial class Inner { }
}
";

		var outcome = GeneratorTestHarness.Run(source);

		outcome.CompilationErrors.Should().BeEmpty();
		outcome.GeneratedSources.Select(s => s.HintName).Should()
			.BeEquivalentTo(new[] { "TestApp.ContainerA.Inner.g.cs", "TestApp.ContainerB.Inner.g.cs" });

		outcome.OutputCompilation.GetTypeByMetadataName("TestApp.ContainerA+Inner")!
			.GetMembers("Links").Should().NotBeEmpty();
		outcome.OutputCompilation.GetTypeByMetadataName("TestApp.ContainerB+Inner")!
			.GetMembers("Links").Should().NotBeEmpty();
	}

	[Fact]
	public void Partial_Declarations_Across_Files_Generate_Once()
	{
		const string first = @"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public partial class Split
{
	public string Name { get; set; } = default!;
}
";
		const string second = @"
namespace TestApp;

public partial class Split
{
	public int Value { get; set; }
}
";

		var outcome = GeneratorTestHarness.Run(first, second);

		outcome.CompilationErrors.Should().BeEmpty();
		outcome.GeneratedSources.Should().ContainSingle();
	}

	[Fact]
	public void Same_Name_In_Different_Namespaces_Are_Both_Generated()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp.First
{
	[HalResponse]
	public partial class Shared { }
}

namespace TestApp.Second
{
	[HalResponse]
	public partial class Shared { }
}
";

		var outcome = GeneratorTestHarness.Run(source);

		outcome.CompilationErrors.Should().BeEmpty();
		outcome.GeneratedSources.Select(s => s.HintName).Should()
			.BeEquivalentTo(new[] { "TestApp.First.Shared.g.cs", "TestApp.Second.Shared.g.cs" });
	}
}
