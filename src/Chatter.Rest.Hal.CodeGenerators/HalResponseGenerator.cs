using Microsoft.CodeAnalysis;

namespace Chatter.Rest.Hal.CodeGenerators;

[Generator]
public class HalResponseGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var halResponseTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => Parser.IsSyntaxTargetForGeneration(node),
                static (ctx, _) => Parser.GetSemanticTargetForGeneration(ctx)
            )
            .Where(static type => type is not null)
            .Collect();

        context.RegisterSourceOutput(halResponseTypes, Emitter.Emit);
    }
}