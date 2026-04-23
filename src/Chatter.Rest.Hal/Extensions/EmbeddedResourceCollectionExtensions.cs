using System;
using System.Collections.Generic;
using System.Linq;

namespace Chatter.Rest.Hal;

/// <summary>
/// Extension methods for querying embedded resource collections.
/// </summary>
public static class EmbeddedResourceCollectionExtensions
{
	/// <summary>
	/// Gets the embedded resource with the specified name.
	/// </summary>
	/// <param name="erc">The embedded resource collection to query.</param>
	/// <param name="name">The name of the embedded resource to find.</param>
	/// <returns>The embedded resource, or null if not found.</returns>
	/// <remarks>
	/// This method uses <see cref="System.Linq.Enumerable.SingleOrDefault{TSource}(System.Collections.Generic.IEnumerable{TSource}, System.Func{TSource, bool})"/>
	/// to preserve the duplicate-throw contract: it throws <see cref="InvalidOperationException"/>
	/// if multiple embedded resources share the same name. Callers needing O(1) lookup without the
	/// duplicate-throw contract should use <see cref="EmbeddedResourceCollection.TryGetByName"/> directly.
	/// </remarks>
	public static EmbeddedResource? GetEmbeddedResource(this EmbeddedResourceCollection? erc, string name)
		=> erc?.SingleOrDefault(e => e.Name?.Equals(name, StringComparison.Ordinal) == true);

	/// <summary>
	/// Gets the resource collection from the embedded resource with the specified name.
	/// </summary>
	/// <param name="erc">The embedded resource collection to query.</param>
	/// <param name="name">The name of the embedded resource to find.</param>
	/// <returns>The resource collection, or null if not found.</returns>
	public static ResourceCollection? GetResourceCollection(this EmbeddedResourceCollection erc, string name)
		=> erc?.GetEmbeddedResource(name)?.Resources;

	/// <summary>
	/// Gets the resources from the embedded resource with the specified name, cast to the specified type.
	/// </summary>
	/// <typeparam name="T">The type to cast the resources to.</typeparam>
	/// <param name="erc">The embedded resource collection to query.</param>
	/// <param name="name">The name of the embedded resource to find.</param>
	/// <returns>An enumerable of the resources cast to type <typeparamref name="T"/>, or null if not found.</returns>
	public static IEnumerable<T?>? GetResources<T>(this EmbeddedResourceCollection erc, string name) where T : class
		=> erc?.GetResourceCollection(name)?.As<T>();
}
