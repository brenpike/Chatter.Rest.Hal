using Microsoft.CodeAnalysis;

namespace Chatter.Rest.Hal.CodeGenerators;

/// <summary>
/// The diagnostics reported by <see cref="HalResponseGenerator"/> when an annotated declaration
/// cannot receive the HAL members. Every case here previously failed silently or surfaced only as an
/// unrelated compiler error.
/// </summary>
internal static class Diagnostics
{
	private const string Category = "Chatter.Rest.Hal.CodeGenerators";

	/// <summary>HAL0001: the annotated type is not declared <c>partial</c>.</summary>
	internal static readonly DiagnosticDescriptor TargetMustBePartial = new(
		id: "HAL0001",
		title: "[HalResponse] target must be partial",
		messageFormat: "'{0}' is annotated with [HalResponse] but is not declared 'partial', so the generator cannot add the Links and Embedded members. Add the 'partial' modifier.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>HAL0002: a type containing the annotated type is not declared <c>partial</c>.</summary>
	internal static readonly DiagnosticDescriptor ContainingTypeMustBePartial = new(
		id: "HAL0002",
		title: "Containing type of a [HalResponse] target must be partial",
		messageFormat: "'{0}' is annotated with [HalResponse] but its containing type '{1}' is not declared 'partial', so the generator cannot re-declare the nesting chain. Add the 'partial' modifier to '{1}'.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>HAL0003: the annotated type is a record, which the generator does not support.</summary>
	internal static readonly DiagnosticDescriptor RecordTargetNotSupported = new(
		id: "HAL0003",
		title: "[HalResponse] does not support record targets",
		messageFormat: "'{0}' is a record. [HalResponse] supports class declarations only, and no HAL members are generated for it. Declare it as a partial class instead.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	/// <summary>HAL0005: the annotated type or a containing type is file-local.</summary>
	internal static readonly DiagnosticDescriptor FileLocalTargetNotSupported = new(
		id: "HAL0005",
		title: "[HalResponse] does not support file-local types",
		messageFormat: "'{0}' is annotated with [HalResponse] but '{1}' is declared with the 'file' modifier. Generated sources live in a separate file, where a re-declared file-local type would be an unrelated type, so no HAL members are generated. Remove the 'file' modifier.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>HAL0004: the annotated type already declares a member the generator would add.</summary>
	internal static readonly DiagnosticDescriptor HalMemberAlreadyDeclared = new(
		id: "HAL0004",
		title: "[HalResponse] target already declares a HAL member",
		messageFormat: "'{0}' already declares a member named '{1}', so the generator cannot add the HAL '{1}' member. Remove the existing member or the [HalResponse] annotation.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);
}
