using System.Collections.Generic;

namespace Chatter.Rest.Hal;

/// <summary>
/// Extension methods for navigating and querying HAL resources.
/// </summary>
public static class ResourceExtensions
{
	/// <summary>
	/// Gets the embedded resources with the specified name, cast to the specified type.
	/// </summary>
	/// <typeparam name="T">The type to cast the embedded resources to.</typeparam>
	/// <param name="resource">The resource to query.</param>
	/// <param name="name">The name of the embedded resource entry.</param>
	/// <returns>An enumerable of the embedded resources cast to type <typeparamref name="T"/>, or null if not found.</returns>
	public static IEnumerable<T?>? GetEmbeddedResources<T>(this Resource resource, string name) where T : class
		=> resource?.Embedded?.GetResources<T>(name);

	/// <summary>
	/// Gets the resource collection with the specified name from the embedded resources.
	/// </summary>
	/// <param name="resource">The resource to query.</param>
	/// <param name="name">The name of the embedded resource entry.</param>
	/// <returns>The resource collection, or null if not found.</returns>
	public static ResourceCollection? GetResourceCollection(this Resource resource, string name)
		=> resource?.Embedded?.GetResourceCollection(name);

	/// <summary>
	/// Gets the link with the specified relation from the resource's links.
	/// </summary>
	/// <param name="resource">The resource to query.</param>
	/// <param name="relation">The link relation to find.</param>
	/// <returns>The link, or null if not found.</returns>
	public static Link? GetLinkOrDefault(this Resource resource, string relation)
		=> resource?.Links?.GetLinkOrDefault(relation);

	/// <summary>
	/// Gets the collection of link objects for the specified link relation.
	/// </summary>
	/// <param name="resource">The resource to query.</param>
	/// <param name="relation">The link relation to find.</param>
	/// <returns>The link object collection, or null if the link relation is not found.</returns>
	public static LinkObjectCollection? GetLinkObjects(this Resource resource, string relation)
		=> resource?.Links?.GetLinkObjects(relation);

	/// <summary>
	/// Gets the link object with the specified relation and name from the resource's links.
	/// </summary>
	/// <param name="resource">The resource to query.</param>
	/// <param name="linkRelation">The link relation to find.</param>
	/// <param name="linkObjectName">The name of the specific link object within the relation.</param>
	/// <returns>The link object, or null if not found.</returns>
	public static LinkObject? GetLinkObjectOrDefault(this Resource resource, string linkRelation, string linkObjectName)
		=> resource?.Links?.GetLinkObjectOrDefault(linkRelation, linkObjectName);

	/// <summary>
	/// Gets the first link object with the specified relation from the resource's links.
	/// </summary>
	/// <param name="resource">The resource to query.</param>
	/// <param name="linkRelation">The link relation to find.</param>
	/// <returns>The first link object for the relation, or null if not found.</returns>
	public static LinkObject? GetLinkObjectOrDefault(this Resource resource, string linkRelation)
		=> resource?.Links?.GetLinkObjectOrDefault(linkRelation);
}
