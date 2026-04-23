using System.Collections.Immutable;
using System.Linq;
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
				"Chatter.Rest.Hal.HalResponseAttribute",
				static (node, _) => node is ClassDeclarationSyntax,
				static (ctx, _) => (ClassDeclarationSyntax)ctx.TargetNode
			);

		var processedTypes = halResponseTypes
			.Collect()
			.Select(static (types, _) =>
				types
					.Where(static t => t is not null)
					.Select(static c => new HalClassInfo(
						c!.Identifier.Text,
						Emitter.GetNamespaceFrom(c)))
					.GroupBy(static x => (x.Namespace ?? string.Empty, x.Name))
					.Select(static g => g.First())
					.OrderBy(static x => x.Namespace ?? string.Empty, StringComparer.Ordinal)
					.ThenBy(static x => x.Name, StringComparer.Ordinal)
					.ToImmutableArray());

		context.RegisterSourceOutput(processedTypes, static (ctx, classes) =>
			Emitter.Emit(ctx, classes));
	}
}
