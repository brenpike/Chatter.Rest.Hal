using System.Collections.Generic;
using System.Linq;

namespace Chatter.Rest.Hal;

/// <summary>
/// Extension methods for transforming resource collections.
/// </summary>
public static class ResourceCollectionExtensions
{
	/// <summary>
	/// Casts each resource in the collection to the specified type.
	/// </summary>
	/// <typeparam name="TResource">The type to cast the resources to.</typeparam>
	/// <param name="rc">The resource collection to transform.</param>
	/// <returns>An enumerable of resources cast to type <typeparamref name="TResource"/>.</returns>
	public static IEnumerable<TResource?> As<TResource>(this ResourceCollection rc) where TResource : class
		=> rc.Select(r => r.As<TResource>());
}
