using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Chatter.Rest.Hal.CodeGenerators;

/// <summary>
/// Projects a discovered <c>[HalResponse]</c> declaration onto the equatable
/// <see cref="HalClassInfo"/> model consumed by the <see cref="Emitter"/>.
/// </summary>
internal static class Parser
{
	internal const string HalResponseAttributeMetadataName = "Chatter.Rest.Hal.HalResponseAttribute";

	/// <summary>Fast syntax-only filter for the attribute discovery pipeline.</summary>
	internal static bool IsCandidate(SyntaxNode node) => node is ClassDeclarationSyntax;

	/// <summary>
	/// Builds the model for one annotated declaration. Returns <see langword="null"/> when the
	/// declaration cannot be described (for example, an attribute on a node that is not a type).
	/// </summary>
	internal static HalClassInfo? Transform(INamedTypeSymbol symbol,
		TypeDeclarationSyntax declaration,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var containingTypes = ImmutableArray.CreateBuilder<ContainingTypeInfo>();
		for (var parent = declaration.Parent as TypeDeclarationSyntax;
			parent is not null;
			parent = parent.Parent as TypeDeclarationSyntax)
		{
			containingTypes.Insert(0, new ContainingTypeInfo(KeywordFor(parent), NameWithTypeParameters(parent)));
		}

		return new HalClassInfo(
			NamespaceOf(symbol),
			new EquatableArray<ContainingTypeInfo>(containingTypes.ToImmutable()),
			NameWithTypeParameters(declaration),
			MetadataNameOf(symbol));
	}

	/// <summary>The declaration keyword to repeat when re-declaring <paramref name="declaration"/> as a partial.</summary>
	internal static string KeywordFor(TypeDeclarationSyntax declaration) => declaration switch
	{
		RecordDeclarationSyntax record =>
			record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? "record struct" : "record",
		StructDeclarationSyntax => "struct",
		InterfaceDeclarationSyntax => "interface",
		_ => "class"
	};

	/// <summary>
	/// The declared name plus its type parameter list. Constraints and type-parameter attributes are
	/// deliberately dropped: a supplementary partial declaration may omit them, and repeating them
	/// risks duplicate-attribute errors.
	/// </summary>
	internal static string NameWithTypeParameters(TypeDeclarationSyntax declaration)
	{
		var typeParameters = declaration.TypeParameterList;
		if (typeParameters is null || typeParameters.Parameters.Count == 0)
		{
			return declaration.Identifier.Text;
		}

		var names = typeParameters.Parameters.Select(static p => p.VarianceKeyword.IsKind(SyntaxKind.None)
			? p.Identifier.Text
			: $"{p.VarianceKeyword.Text} {p.Identifier.Text}");

		return $"{declaration.Identifier.Text}<{string.Join(", ", names)}>";
	}

	/// <summary>The fully qualified metadata name, for example <c>Ns.Outer`1+Inner</c>.</summary>
	internal static string MetadataNameOf(INamedTypeSymbol symbol)
	{
		var names = new List<string>();
		for (INamedTypeSymbol? type = symbol; type is not null; type = type.ContainingType)
		{
			names.Insert(0, type.MetadataName);
		}

		var ns = NamespaceOf(symbol);
		var nested = string.Join("+", names);
		return string.IsNullOrEmpty(ns) ? nested : $"{ns}.{nested}";
	}

	internal static string? NamespaceOf(INamedTypeSymbol symbol) =>
		symbol.ContainingNamespace is null || symbol.ContainingNamespace.IsGlobalNamespace
			? null
			: symbol.ContainingNamespace.ToDisplayString();
}
