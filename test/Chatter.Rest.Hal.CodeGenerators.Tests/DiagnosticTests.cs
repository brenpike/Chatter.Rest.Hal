using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Chatter.Rest.Hal.CodeGenerators.Tests;

/// <summary>
/// Covers issue #107: every declaration the generator refuses must say why, instead of failing
/// silently or surfacing only as an unrelated compiler error.
/// </summary>
public class DiagnosticTests
{
	[Fact]
	public void NonPartial_Target_Reports_HAL0001()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public class NotPartial { }
";

		var outcome = GeneratorTestHarness.Run(source);

		var diagnostic = outcome.GeneratorDiagnostics.Should().ContainSingle().Subject;
		diagnostic.Id.Should().Be("HAL0001");
		diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
		diagnostic.GetMessage().Should().Contain("TestApp.NotPartial").And.Contain("partial");
	}

	[Fact]
	public void NonPartial_Target_Generates_Nothing()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public class NotPartial { }
";

		var outcome = GeneratorTestHarness.Run(source);

		outcome.GeneratedSources.Should().BeEmpty(
			"emitting a conflicting declaration would replace the diagnostic with an opaque CS0260");
		outcome.CompilationErrors.Should().BeEmpty();
	}

	[Fact]
	public void NonPartial_Containing_Type_Reports_HAL0002()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

public class Outer
{
	[HalResponse]
	public partial class Inner { }
}
";

		var outcome = GeneratorTestHarness.Run(source);

		var diagnostic = outcome.GeneratorDiagnostics.Should().ContainSingle().Subject;
		diagnostic.Id.Should().Be("HAL0002");
		diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
		diagnostic.GetMessage().Should().Contain("Outer");
		outcome.GeneratedSources.Should().BeEmpty();
	}

	[Fact]
	public void Record_Target_Reports_HAL0003()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public partial record RecordResponse(string Name);
";

		var outcome = GeneratorTestHarness.Run(source);

		var diagnostic = outcome.GeneratorDiagnostics.Should().ContainSingle().Subject;
		diagnostic.Id.Should().Be("HAL0003");
		diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
		diagnostic.GetMessage().Should().Contain("record");
		outcome.GeneratedSources.Should().BeEmpty();
	}

	[Fact]
	public void Record_Class_Target_Reports_HAL0003()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public partial record class RecordClassResponse
{
	public string Name { get; set; } = default!;
}
";

		var outcome = GeneratorTestHarness.Run(source);

		outcome.GeneratorDiagnostics.Select(d => d.Id).Should().Equal("HAL0003");
	}

	[Theory]
	[InlineData("Links", "global::Chatter.Rest.Hal.LinkCollection?")]
	[InlineData("Embedded", "global::Chatter.Rest.Hal.EmbeddedResourceCollection?")]
	public void Member_Collision_Reports_HAL0004(string memberName, string memberType)
	{
		var source = $@"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public partial class Colliding
{{
	public {memberType} {memberName} {{ get; set; }}
}}
";

		var outcome = GeneratorTestHarness.Run(source);

		var diagnostic = outcome.GeneratorDiagnostics.Should().ContainSingle().Subject;
		diagnostic.Id.Should().Be("HAL0004");
		diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
		diagnostic.GetMessage().Should().Contain(memberName);
		outcome.GeneratedSources.Should().BeEmpty(
			"emitting the member anyway would replace the diagnostic with an opaque CS0102");
	}

	[Fact]
	public void Member_Collision_On_A_Field_Also_Reports_HAL0004()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public partial class CollidingField
{
	private int Links;
}
";

		var outcome = GeneratorTestHarness.Run(source);

		outcome.GeneratorDiagnostics.Select(d => d.Id).Should().Contain("HAL0004");
	}

	[Fact]
	public void Valid_Target_Reports_No_Diagnostics()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public partial class Valid { }
";

		var outcome = GeneratorTestHarness.Run(source);

		outcome.GeneratorDiagnostics.Should().BeEmpty();
		outcome.GeneratedSources.Should().ContainSingle();
	}

	[Fact]
	public void Inherited_Hal_Members_Do_Not_Report_HAL0004()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public partial class BaseResponse { }

[HalResponse]
public partial class DerivedResponse : BaseResponse { }
";

		var outcome = GeneratorTestHarness.Run(source);

		outcome.GeneratorDiagnostics.Where(d => d.Id == "HAL0004").Should().BeEmpty(
			"only members declared on the target itself collide with the generated ones");
	}

	[Fact]
	public void One_Invalid_Target_Does_Not_Suppress_Generation_For_Others()
	{
		const string source = @"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public class NotPartial { }

[HalResponse]
public partial class Fine { }
";

		var outcome = GeneratorTestHarness.Run(source);

		outcome.GeneratorDiagnostics.Select(d => d.Id).Should().Equal("HAL0001");
		outcome.GeneratedSources.Select(s => s.HintName).Should().Equal("TestApp.Fine.g.cs");
	}
}
