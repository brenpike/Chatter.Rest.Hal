namespace Chatter.Rest.Hal;

/// <summary>
/// Configuration options for HAL JSON serialization behavior.
/// </summary>
public sealed class HalJsonOptions
{
	/// <summary>
	/// Process-global default options consumed by attribute-wired converters.
	/// Mutate only at application startup, before any serialization occurs.
	/// bool reads/writes are atomic on all .NET platforms; no locking is needed
	/// when mutations are restricted to startup.
	/// </summary>
	public static readonly HalJsonOptions Default = new();

	/// <summary>
	/// When true, all link relations serialize as JSON arrays regardless of count.
	/// Aligns with HAL spec guidance: "If you're unsure whether the link should be
	/// singular, assume it will be multiple."
	/// Default: false (preserves existing library behavior).
	/// </summary>
	public bool AlwaysUseArrayForLinks { get; set; } = false;
}
