using System;
using System.Linq;
using Xunit;

namespace Chatter.Rest.Hal.CodeGenerators.Tests;

/// <summary>
/// Containers whose partial re-declaration requires modifiers (<c>readonly</c>/<c>ref</c> on
/// structs), and the reserved marker hint name, must not break generation.
/// </summary>
public class ContainerModifierTests
{
	[Fact]
	public void TargetNestedInReadonlyPartialStructCompiles()
	{
		var outcome = GeneratorTestHarness.Run(@"
namespace Sample
{
	public readonly partial struct Wrapper
	{
		[Chatter.Rest.Hal.HalResponse]
		public partial class Payload
		{
		}
	}
}");

		Assert.Empty(outcome.CompilationErrors);
		var generated = outcome.SourceFor(outcome.GeneratedSources.Single().HintName);
		Assert.Contains("readonly partial struct Wrapper", generated, StringComparison.Ordinal);
	}

	[Fact]
	public void TargetNestedInRefPartialStructCompiles()
	{
		var outcome = GeneratorTestHarness.Run(@"
namespace Sample
{
	public ref partial struct Wrapper
	{
		[Chatter.Rest.Hal.HalResponse]
		public partial class Payload
		{
		}
	}
}");

		Assert.Empty(outcome.CompilationErrors);
		var generated = outcome.SourceFor(outcome.GeneratedSources.Single().HintName);
		Assert.Contains("ref partial struct Wrapper", generated, StringComparison.Ordinal);
	}

	[Fact]
	public void GlobalNamespaceTargetNamedHalResponseAttributeDoesNotCollideWithMarkerHint()
	{
		var outcome = GeneratorTestHarness.Run(@"
[Chatter.Rest.Hal.HalResponse]
public partial class HalResponseAttribute
{
}");

		Assert.Empty(outcome.CompilationErrors);
		// The harness's GeneratedSources filter also matches the suffixed hint, so inspect the
		// full set: the marker plus exactly one target source with a non-colliding hint name.
		var target = Assert.Single(outcome.AllGeneratedSources.Where(s => s.HintName != "HalResponseAttribute.g.cs"));
		Assert.Contains("partial class HalResponseAttribute", target.SourceText.ToString(), StringComparison.Ordinal);
	}
}
