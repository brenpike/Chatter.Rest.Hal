using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Chatter.Rest.Hal.CodeGenerators.Tests;

/// <summary>
/// File-local types (C# 11 <c>file</c> modifier) cannot be re-declared from a generated syntax
/// tree, so the generator must report <c>HAL0005</c> and generate nothing instead of silently
/// attaching members to an unrelated type. Self-contained driver because the shared harness pins
/// C# 10 parse options, which predate the <c>file</c> modifier.
/// </summary>
public class FileLocalTargetTests
{
	[Fact]
	public void FileLocalContainingTypeReportsHal0005AndGeneratesNothing()
	{
		var (diagnostics, targetSources) = Run(@"
namespace Sample
{
	file partial class Wrapper
	{
		[Chatter.Rest.Hal.HalResponse]
		public partial class Payload
		{
		}
	}
}");

		Assert.Contains(diagnostics, d => d.Id == "HAL0005");
		Assert.Empty(targetSources);
	}

	[Fact]
	public void FileLocalTargetReportsHal0005AndGeneratesNothing()
	{
		var (diagnostics, targetSources) = Run(@"
namespace Sample
{
	[Chatter.Rest.Hal.HalResponse]
	file partial class Payload
	{
	}
}");

		Assert.Contains(diagnostics, d => d.Id == "HAL0005");
		Assert.Empty(targetSources);
	}

	private static (ImmutableArray<Diagnostic> generatorDiagnostics, ImmutableArray<string> targetSources) Run(string source)
	{
		var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp11);

		var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
		var references = trustedAssemblies
			.Split(Path.PathSeparator)
			.Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
			.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
			.ToImmutableArray();

		var compilation = CSharpCompilation.Create(
			"Chatter.Rest.Hal.CodeGenerators.FileLocalTargetTests",
			new[] { CSharpSyntaxTree.ParseText(source, parseOptions, path: "Source0.cs") },
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
				nullableContextOptions: NullableContextOptions.Enable));

		var driver = CSharpGeneratorDriver.Create(
			generators: new[] { new HalResponseGenerator().AsSourceGenerator() },
			parseOptions: parseOptions);

		var runResult = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _).GetRunResult();

		var targetSources = runResult.Results
			.SelectMany(r => r.GeneratedSources)
			.Select(s => s.HintName)
			.Where(h => !h.StartsWith("HalResponseAttribute", StringComparison.Ordinal))
			.ToImmutableArray();

		return (runResult.Diagnostics, targetSources);
	}
}
