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

	/// <summary>The members the generator adds, and therefore the names it cannot share with user code.</summary>
	internal static readonly string[] GeneratedMemberNames = { "Links", "Embedded" };

	/// <summary>
	/// Fast syntax-only filter for the attribute discovery pipeline. Records are accepted here so the
	/// unsupported-target diagnostic can be reported instead of the annotation failing silently.
	/// </summary>
	internal static bool IsCandidate(SyntaxNode node) =>
		node is ClassDeclarationSyntax or RecordDeclarationSyntax;

	/// <summary>
	/// Inspects one annotated declaration, returning the model to emit when the declaration can
	/// receive the HAL members and diagnostics explaining every reason it cannot.
	/// </summary>
	internal static HalTarget Transform(INamedTypeSymbol symbol,
		TypeDeclarationSyntax declaration,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var typeName = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
		var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

		if (declaration is RecordDeclarationSyntax)
		{
			diagnostics.Add(DiagnosticInfo.Create(
				Diagnostics.RecordTargetNotSupported, declaration.Identifier.GetLocation(), typeName));
			return new HalTarget(null, diagnostics.ToImmutable());
		}

		var canGenerate = true;

		if (!declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
		{
			diagnostics.Add(DiagnosticInfo.Create(
				Diagnostics.TargetMustBePartial, declaration.Identifier.GetLocation(), typeName));
			canGenerate = false;
		}

		var containingTypes = ImmutableArray.CreateBuilder<ContainingTypeInfo>();
		for (var parent = declaration.Parent as TypeDeclarationSyntax;
			parent is not null;
			parent = parent.Parent as TypeDeclarationSyntax)
		{
			if (!parent.Modifiers.Any(SyntaxKind.PartialKeyword))
			{
				diagnostics.Add(DiagnosticInfo.Create(
					Diagnostics.ContainingTypeMustBePartial,
					parent.Identifier.GetLocation(),
					typeName,
					parent.Identifier.Text));
				canGenerate = false;
			}

			containingTypes.Insert(0, new ContainingTypeInfo(KeywordFor(parent), NameWithTypeParameters(parent)));
		}

		foreach (var memberName in GeneratedMemberNames)
		{
			if (!symbol.GetMembers(memberName).IsEmpty)
			{
				diagnostics.Add(DiagnosticInfo.Create(
					Diagnostics.HalMemberAlreadyDeclared,
					declaration.Identifier.GetLocation(),
					typeName,
					memberName));
				canGenerate = false;
			}
		}

		if (!canGenerate)
		{
			return new HalTarget(null, diagnostics.ToImmutable());
		}

		var info = new HalClassInfo(
			NamespaceOf(symbol),
			new EquatableArray<ContainingTypeInfo>(containingTypes.ToImmutable()),
			NameWithTypeParameters(declaration),
			MetadataNameOf(symbol));

		return new HalTarget(info, diagnostics.ToImmutable());
	}

	/// <summary>
	/// The declaration text (required modifiers, <c>partial</c>, and the keyword) to repeat when
	/// re-declaring <paramref name="declaration"/>. <c>readonly</c> and <c>ref</c> are required on
	/// every partial declaration of a struct, so dropping them would make the generated
	/// re-declaration of a <c>readonly partial struct</c> or <c>ref partial struct</c> container
	/// fail to compile.
	/// </summary>
	internal static string KeywordFor(TypeDeclarationSyntax declaration)
	{
		var keyword = declaration switch
		{
			RecordDeclarationSyntax record =>
				record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? "record struct" : "record",
			StructDeclarationSyntax => "struct",
			InterfaceDeclarationSyntax => "interface",
			_ => "class"
		};

		var prefix = string.Empty;
		if (declaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)) prefix += "readonly ";
		if (declaration.Modifiers.Any(SyntaxKind.RefKeyword)) prefix += "ref ";
		return $"{prefix}partial {keyword}";
	}

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
