using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal;

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
	/// Safe to call multiple times on the same instance — the duplicate guard is applied per converter
	/// type, so a consumer who registered some HAL converters by hand still gets the remaining ones.
	/// A converter already present is left exactly as registered, including its
	/// <see cref="HalJsonOptions"/>; <paramref name="halOptions"/> applies only to converters this call
	/// actually adds.
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
		var resolved = halOptions ?? HalJsonOptions.Default;
		options.AddIfMissing(() => new LinkCollectionConverter(resolved));
		options.AddIfMissing(() => new LinkObjectCollectionConverter(resolved));
		options.AddIfMissing(() => new LinkConverter(resolved));
		options.AddIfMissing(() => new LinkObjectConverter());
		options.AddIfMissing(() => new ResourceConverter());
		options.AddIfMissing(() => new EmbeddedResourceCollectionConverter());
		options.AddIfMissing(() => new EmbeddedResourceConverter());
		options.AddIfMissing(() => new ResourceCollectionConverter());
		return options;
	}

	/// <summary>
	/// Adds the converter produced by <paramref name="factory"/> unless an instance of
	/// <typeparamref name="TConverter"/> is already registered.
	/// </summary>
	/// <remarks>
	/// Guarding on the exact HAL converter type — rather than on any converter that handles the same
	/// domain type — keeps a consumer's own converter first in the list, where
	/// <see cref="System.Text.Json.JsonSerializer"/> continues to select it.
	/// </remarks>
	private static void AddIfMissing<TConverter>(this JsonSerializerOptions options, Func<TConverter> factory)
		where TConverter : JsonConverter
	{
		for (int i = 0; i < options.Converters.Count; i++)
		{
			if (options.Converters[i] is TConverter)
			{
				return;
			}
		}

		options.Converters.Add(factory());
	}
}
