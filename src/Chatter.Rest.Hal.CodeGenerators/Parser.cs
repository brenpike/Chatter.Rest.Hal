using System.Linq;
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
		if (context.Node is not AttributeSyntax attributeSyntax)
			return null;

		if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
			return null;

		var fullName = attributeSymbol.ContainingType?.ToDisplayString();
		if (fullName != HalResponseAttributeQualifiedName)
			return null;
		// find the nearest enclosing class declaration rather than relying on Parent.Parent
		return attributeSyntax.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
	}

	internal static bool IsSyntaxTargetForGeneration(SyntaxNode node)
	{
		if (node is not AttributeSyntax attribute)
			return false;

		var name = ExtractName(attribute.Name);
		return name == HalResponse || name == HalResponseAttribute;
	}

	private static string? ExtractName(NameSyntax? name) =>
		name switch
		{
			IdentifierNameSyntax ins => ins.Identifier.Text,
			QualifiedNameSyntax qns => ExtractName(qns.Right),
			_ => null
		};
}