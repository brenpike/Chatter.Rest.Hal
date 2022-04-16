using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Chatter.Rest.Hal.CodeGenerators;

internal class Parser
{
	private const string HalResponse = "HalResponse";
	private const string HalResponseAttribute = "HalResponseAttribute";
	private const string HalResponseAttributeQualifiedName = "Chatter.Rest.Hal.HalResponseAttribute";

	internal static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
	{
		var attributeSyntax = (AttributeSyntax)context.Node;

		if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
		{
			return null;
		}

		var fullName = attributeSymbol.ContainingType.ToDisplayString();

		if (fullName != HalResponseAttributeQualifiedName)
		{
			return null;
		}

		return attributeSyntax.Parent?.Parent as ClassDeclarationSyntax;
	}

	internal static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
		node is AttributeSyntax attribute &&
		ExtractName(attribute.Name) is HalResponse or HalResponseAttribute;

	private static string? ExtractName(NameSyntax? name) =>
		name switch
		{
			IdentifierNameSyntax ins => ins.Identifier.Text,
			QualifiedNameSyntax qns => ExtractName(qns.Right),
			_ => null
		};
}