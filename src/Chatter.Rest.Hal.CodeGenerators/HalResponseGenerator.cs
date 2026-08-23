using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Chatter.Rest.Hal.CodeGenerators;

[Generator]
public class HalResponseGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Ship the marker attribute with the generator so installing this package alone is enough
		// for [HalResponse] to resolve and the generator to fire.
		context.RegisterPostInitializationOutput(static ctx =>
			ctx.AddSource(AttributeSource.HintName, AttributeSource.Source));

		// The projection onto the equatable model happens inside the attribute transform, before
		// Collect. Keeping the syntax node in the pipeline would hand Collect a value with a new
		// identity after every keystroke and re-emit every source on every unrelated edit.
		var targets = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				Parser.HalResponseAttributeMetadataName,
				static (node, _) => Parser.IsCandidate(node),
				static (ctx, cancellationToken) =>
					ctx.TargetSymbol is INamedTypeSymbol symbol && ctx.TargetNode is TypeDeclarationSyntax declaration
						? Parser.Transform(symbol, declaration, cancellationToken)
						: HalTarget.None)
			.WithTrackingName(TrackingNames.Targets);

		context.RegisterSourceOutput(targets, static (ctx, target) =>
		{
			foreach (var diagnostic in target.Diagnostics)
			{
				ctx.ReportDiagnostic(diagnostic.ToDiagnostic());
			}
		});

		var models = targets
			.Where(static target => target.Info.HasValue)
			.Select(static (target, _) => target.Info!.Value)
			.WithTrackingName(TrackingNames.Models)
			.Collect()
			.Select(static (collected, _) => collected
				.GroupBy(static info => info.MetadataName, StringComparer.Ordinal)
				.Select(static g => g.First())
				.OrderBy(static info => info.MetadataName, StringComparer.Ordinal)
				.ToImmutableArray())
			.WithTrackingName(TrackingNames.DeduplicatedModels);

		context.RegisterSourceOutput(models, static (ctx, classes) => Emitter.Emit(ctx, classes));
	}
}
