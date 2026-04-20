using System.Linq;

namespace Chatter.Rest.Hal;

/// <summary>
/// Extension methods for querying link object collections.
/// </summary>
public static class LinkObjectCollectionExtensions
{
	/// <summary>
	/// Gets the link object with the specified name from the collection.
	/// </summary>
	/// <param name="linkObjects">The link object collection to query.</param>
	/// <param name="name">The name of the link object to find.</param>
	/// <returns>The link object, or null if not found.</returns>
	public static LinkObject? GetLinkObjectOrDefault(this LinkObjectCollection linkObjects, string name)
		=> linkObjects?.SingleOrDefault(lo => lo.Name?.Equals(name) ?? false);
}
