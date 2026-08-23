using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Chatter.Rest.Hal.CodeGenerators.Tests;

/// <summary>
/// Upgrade-path coverage for consumers that declared <c>Chatter.Rest.Hal.HalResponseAttribute</c>
/// in their own source (the 0.3.x workaround, when no package shipped the attribute). The
/// <c>CHATTER_REST_HAL_CODEGEN_EXCLUDE_ATTRIBUTE</c> define suppresses the generated copy so such
/// projects upgrade without a same-assembly CS0101 collision.
/// </summary>
public class AttributeOptOutTests
{
	private const string ExcludeSymbol = "CHATTER_REST_HAL_CODEGEN_EXCLUDE_ATTRIBUTE";

	private const string ConsumerDeclaredAttribute = @"
namespace Chatter.Rest.Hal
{
	[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	internal sealed class HalResponseAttribute : System.Attribute
	{
	}
}";

	private const string AnnotatedClass = @"
namespace Consumer
{
	[Chatter.Rest.Hal.HalResponse]
	public partial class Order
	{
	}
}";

	[Fact]
	public void ExistingConsumerWithOwnAttributeAndExcludeDefineCompilesAndGenerates()
	{
		var outcome = Run(new[] { ConsumerDeclaredAttribute, AnnotatedClass }, ExcludeSymbol);

		Assert.Empty(outcome.compilationErrors);
		var generated = Assert.Single(outcome.generatedHintNames.Where(h => !h.StartsWith("HalResponseAttribute", StringComparison.Ordinal)));
		Assert.Contains("Order", generated, StringComparison.Ordinal);
	}

	[Fact]
	public void ExistingConsumerWithOwnAttributeAndNoDefineStillCollides()
	{
		var outcome = Run(new[] { ConsumerDeclaredAttribute, AnnotatedClass });

		Assert.Contains(outcome.compilationErrors, d => d.Id == "CS0101");
	}

	[Fact]
	public void DefaultConsumerWithoutDefineGetsShippedAttribute()
	{
		var outcome = Run(new[] { AnnotatedClass });

		Assert.Empty(outcome.compilationErrors);
		Assert.Contains(outcome.generatedHintNames, h => h.StartsWith("HalResponseAttribute", StringComparison.Ordinal));
	}

	private static (ImmutableArray<Diagnostic> compilationErrors, ImmutableArray<string> generatedHintNames) Run(
		string[] sources, params string[] preprocessorSymbols)
	{
		// Self-contained driver: the shared harness pins parse options without preprocessor
		// symbols, and these tests must not modify shared test infrastructure.
		var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp10, preprocessorSymbols: preprocessorSymbols);

		var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
		var references = trustedAssemblies
			.Split(Path.PathSeparator)
			.Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
			.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
			.ToImmutableArray();

		var compilation = CSharpCompilation.Create(
			"Chatter.Rest.Hal.CodeGenerators.AttributeOptOutTests",
			sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, parseOptions, path: $"Source{index}.cs")),
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
				nullableContextOptions: NullableContextOptions.Enable));

		var driver = CSharpGeneratorDriver.Create(
			generators: new[] { new HalResponseGenerator().AsSourceGenerator() },
			parseOptions: parseOptions);

		var runResult = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _).GetRunResult();

		var errors = outputCompilation.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToImmutableArray();
		var hintNames = runResult.Results
			.SelectMany(r => r.GeneratedSources)
			.Select(s => s.HintName)
			.ToImmutableArray();

		return (errors, hintNames);
	}
}
