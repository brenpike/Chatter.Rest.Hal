using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Chatter.Rest.Hal.CodeGenerators.Tests;

/// <summary>
/// The result of driving <see cref="HalResponseGenerator"/> over an in-memory compilation.
/// </summary>
internal sealed class GeneratorRunOutcome
{
	internal GeneratorRunOutcome(GeneratorDriver driver,
		GeneratorDriverRunResult runResult,
		Compilation outputCompilation)
	{
		Driver = driver;
		RunResult = runResult;
		OutputCompilation = outputCompilation;
	}

	internal GeneratorDriver Driver { get; }

	internal GeneratorDriverRunResult RunResult { get; }

	internal Compilation OutputCompilation { get; }

	/// <summary>Diagnostics reported by the generator itself.</summary>
	internal ImmutableArray<Diagnostic> GeneratorDiagnostics => RunResult.Diagnostics;

	/// <summary>Compile errors in the compilation produced from the user source plus generated source.</summary>
	internal ImmutableArray<Diagnostic> CompilationErrors =>
		OutputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray();

	/// <summary>Every generated source, including the post-initialization attribute.</summary>
	internal ImmutableArray<GeneratedSourceResult> AllGeneratedSources =>
		RunResult.Results.SelectMany(r => r.GeneratedSources).ToImmutableArray();

	/// <summary>Generated sources produced by the generator, excluding the post-initialization attribute.</summary>
	internal ImmutableArray<GeneratedSourceResult> GeneratedSources =>
		RunResult.Results
			.SelectMany(r => r.GeneratedSources)
			.Where(s => !s.HintName.StartsWith("HalResponseAttribute", StringComparison.Ordinal))
			.ToImmutableArray();

	internal string SourceFor(string hintName) =>
		GeneratedSources.Single(s => s.HintName == hintName).SourceText.ToString();
}

/// <summary>
/// Compiles source snippets in memory and runs <see cref="HalResponseGenerator"/> over them, so the
/// generator's real output and diagnostics can be asserted without a separate build.
/// </summary>
internal static class GeneratorTestHarness
{
	internal const string AssemblyName = "Chatter.Rest.Hal.CodeGenerators.GeneratorTests";

	private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp10);

	private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

	internal static CSharpCompilation CreateCompilation(params string[] sources) =>
		CSharpCompilation.Create(
			AssemblyName,
			sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, ParseOptions, path: $"Source{index}.cs")),
			References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
				nullableContextOptions: NullableContextOptions.Enable));

	internal static GeneratorDriver CreateDriver(bool trackIncrementalSteps = false) =>
		CSharpGeneratorDriver.Create(
			generators: new[] { new HalResponseGenerator().AsSourceGenerator() },
			parseOptions: ParseOptions,
			driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalSteps));

	internal static GeneratorRunOutcome Run(params string[] sources) =>
		Run(CreateCompilation(sources), CreateDriver());

	internal static GeneratorRunOutcome Run(Compilation compilation, GeneratorDriver driver)
	{
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
		return new GeneratorRunOutcome(driver, driver.GetRunResult(), outputCompilation);
	}

	private static ImmutableArray<MetadataReference> CreateReferences()
	{
		var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
		return trustedAssemblies
			.Split(Path.PathSeparator)
			.Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
			.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
			.ToImmutableArray();
	}
}
