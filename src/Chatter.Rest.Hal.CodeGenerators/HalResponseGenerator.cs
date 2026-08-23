using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Chatter.Rest.Hal.CodeGenerators;

[Generator]
public class HalResponseGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var halResponseTypes = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				Parser.HalResponseAttributeMetadataName,
				static (node, _) => Parser.IsCandidate(node),
				static (ctx, _) => (Symbol: ctx.TargetSymbol as INamedTypeSymbol,
					Declaration: ctx.TargetNode as TypeDeclarationSyntax)
			);

		var processedTypes = halResponseTypes
			.Collect()
			.Select(static (types, cancellationToken) =>
			{
				var targets = types
					.Where(static t => t.Symbol is not null && t.Declaration is not null)
					.Select(t => Parser.Transform(t.Symbol!, t.Declaration!, cancellationToken))
					.ToImmutableArray();

				var models = targets
					.Where(static target => target.Info.HasValue)
					.Select(static target => target.Info!.Value)
					.GroupBy(static info => info.MetadataName, StringComparer.Ordinal)
					.Select(static g => g.First())
					.OrderBy(static info => info.MetadataName, StringComparer.Ordinal)
					.ToImmutableArray();

				var diagnostics = targets
					.SelectMany(static target => target.Diagnostics)
					.ToImmutableArray();

				return (Models: models, Diagnostics: diagnostics);
			});

		context.RegisterSourceOutput(processedTypes, static (ctx, processed) =>
		{
			foreach (var diagnostic in processed.Diagnostics)
			{
				ctx.ReportDiagnostic(diagnostic.ToDiagnostic());
			}

			Emitter.Emit(ctx, processed.Models);
		});
	}
}
