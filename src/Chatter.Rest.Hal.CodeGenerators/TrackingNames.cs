namespace Chatter.Rest.Hal.CodeGenerators;

/// <summary>
/// Names attached to the incremental pipeline steps so tests can assert that unrelated edits reuse
/// cached results rather than re-running the generator end to end.
/// </summary>
internal static class TrackingNames
{
	/// <summary>The per-declaration projection onto <see cref="HalTarget"/>.</summary>
	internal const string Targets = "HalResponseTargets";

	/// <summary>The generable subset of <see cref="Targets"/>, projected onto <see cref="HalClassInfo"/>.</summary>
	internal const string Models = "HalResponseModels";

	/// <summary>The collected models after deduplication and ordering.</summary>
	internal const string DeduplicatedModels = "HalResponseDeduplicatedModels";
}
