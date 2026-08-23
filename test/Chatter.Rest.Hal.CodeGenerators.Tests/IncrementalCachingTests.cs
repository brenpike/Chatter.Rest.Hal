using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Chatter.Rest.Hal.CodeGenerators.Tests;

/// <summary>
/// Covers issue #108: the projection onto the equatable model happens inside the attribute
/// transform, so an edit that touches nothing the generator cares about reuses cached results
/// instead of re-running the pipeline and re-emitting every source.
/// </summary>
public class IncrementalCachingTests
{
	private const string HalSource = @"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public partial class CachedResponse
{
	public string Name { get; set; } = default!;
}

public partial class Container
{
	[HalResponse]
	public partial class Inner { }
}
";

	private static readonly IncrementalStepRunReason[] ReusedReasons =
	{
		IncrementalStepRunReason.Cached,
		IncrementalStepRunReason.Unchanged
	};

	[Fact]
	public void Unrelated_Edit_Reuses_Every_Cached_Step()
	{
		var (_, second) = RunTwice(afterEdit: "namespace Unrelated { internal class Bystander { } }");

		AssertAllStepsReused(second, TrackingNames.Targets);
		AssertAllStepsReused(second, TrackingNames.Models);
		AssertAllStepsReused(second, TrackingNames.DeduplicatedModels);
	}

	[Fact]
	public void Unrelated_Edit_Does_Not_Re_Emit_Sources()
	{
		var (_, second) = RunTwice(afterEdit: "namespace Unrelated { internal class Bystander { } }");

		second.TrackedOutputSteps
			.SelectMany(step => step.Value)
			.SelectMany(step => step.Outputs)
			.Select(output => output.Reason)
			.Should().OnlyContain(reason => ReusedReasons.Contains(reason));
	}

	[Fact]
	public void Unrelated_Edit_Produces_The_Same_Sources()
	{
		var (first, second) = RunTwice(afterEdit: "namespace Unrelated { internal class Bystander { } }");

		second.GeneratedSources.Select(s => s.HintName).Should()
			.BeEquivalentTo(first.GeneratedSources.Select(s => s.HintName));
	}

	[Fact]
	public void Adding_A_New_Target_Invalidates_The_Collected_Step()
	{
		const string newTarget = @"
using Chatter.Rest.Hal;

namespace TestApp;

[HalResponse]
public partial class AddedLater { }
";

		var (_, second) = RunTwice(afterEdit: newTarget);

		second.TrackedSteps[TrackingNames.DeduplicatedModels]
			.SelectMany(step => step.Outputs)
			.Select(output => output.Reason)
			.Should().Contain(IncrementalStepRunReason.Modified,
				"a genuinely new target must invalidate the collected models; otherwise the reuse assertions above prove nothing");
	}

	private static void AssertAllStepsReused(GeneratorRunResult result, string trackingName)
	{
		result.TrackedSteps.Should().ContainKey(trackingName);
		result.TrackedSteps[trackingName]
			.SelectMany(step => step.Outputs)
			.Select(output => output.Reason)
			.Should().OnlyContain(reason => ReusedReasons.Contains(reason),
				$"step '{trackingName}' must be reused when an unrelated file changes");
	}

	private static (GeneratorRunResult First, GeneratorRunResult Second) RunTwice(string afterEdit)
	{
		var compilation = GeneratorTestHarness.CreateCompilation(HalSource);
		var driver = GeneratorTestHarness.CreateDriver(trackIncrementalSteps: true);

		driver = driver.RunGenerators(compilation);
		var first = driver.GetRunResult().Results.Single();

		var edited = compilation.AddSyntaxTrees(
			CSharpSyntaxTree.ParseText(afterEdit, (CSharpParseOptions?)compilation.SyntaxTrees.First().Options, path: "Edited.cs"));

		driver = driver.RunGenerators(edited);
		var second = driver.GetRunResult().Results.Single();

		return (first, second);
	}
}
