using System.Linq;
using System.Text.Json;
using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal.Extensions;

/// <summary>
/// Extension methods for configuring HAL JSON serialization via <see cref="JsonSerializerOptions"/>.
/// </summary>
public static class JsonSerializerOptionsExtensions
{
	/// <summary>
	/// Registers HAL JSON converters with the provided options instance.
	/// </summary>
	/// <remarks>
	/// Options-registered converters take precedence over <c>[JsonConverter]</c> attribute-wired
	/// converters when the consumer supplies these options to <see cref="System.Text.Json.JsonSerializer"/>.
	/// Consumers that never call this method continue using attribute-wired converters unchanged.
	/// Safe to call multiple times on the same instance — a duplicate guard is applied.
	/// </remarks>
	/// <param name="options">The <see cref="JsonSerializerOptions"/> to configure.</param>
	/// <param name="halOptions">
	/// HAL-specific serialization options. When <c>null</c>, <see cref="HalJsonOptions.Default"/> is used.
	/// </param>
	/// <returns>The same <paramref name="options"/> instance, for chaining.</returns>
	public static JsonSerializerOptions AddHalConverters(
		this JsonSerializerOptions options,
		HalJsonOptions? halOptions = null)
	{
		if (options.Converters.OfType<LinkCollectionConverter>().Any())
			return options;

		var resolved = halOptions ?? HalJsonOptions.Default;
		options.Converters.Add(new LinkCollectionConverter(resolved));
		options.Converters.Add(new LinkObjectCollectionConverter(resolved));
		options.Converters.Add(new LinkConverter(resolved));
		options.Converters.Add(new LinkObjectConverter());
		options.Converters.Add(new ResourceConverter());
		options.Converters.Add(new EmbeddedResourceCollectionConverter());
		options.Converters.Add(new EmbeddedResourceConverter());
		options.Converters.Add(new ResourceCollectionConverter());
		return options;
	}
}
