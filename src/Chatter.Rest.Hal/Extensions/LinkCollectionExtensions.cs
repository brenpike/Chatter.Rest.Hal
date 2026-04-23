using System;
using System.Linq;

namespace Chatter.Rest.Hal;

/// <summary>
/// Extension methods for querying and manipulating link collections.
/// </summary>
public static class LinkCollectionExtensions
{
	/// <summary>
	/// Gets the link with the specified relation from the collection.
	/// </summary>
	/// <param name="links">The link collection to query.</param>
	/// <param name="relation">The link relation to find.</param>
	/// <returns>The link, or null if not found.</returns>
	/// <remarks>
	/// This method uses <see cref="System.Linq.Enumerable.SingleOrDefault{TSource}(System.Collections.Generic.IEnumerable{TSource}, System.Func{TSource, bool})"/>
	/// to preserve the duplicate-throw contract: it throws <see cref="InvalidOperationException"/>
	/// if multiple links share the same relation. Callers needing O(1) lookup without the
	/// duplicate-throw contract should use <see cref="LinkCollection.TryGetByRel"/> directly.
	/// </remarks>
	public static Link? GetLinkOrDefault(this LinkCollection? links, string relation)
		=> links?.SingleOrDefault(l => l.Rel?.Equals(relation, StringComparison.Ordinal) == true);

	/// <summary>
	/// Gets the collection of link objects for the specified link relation.
	/// </summary>
	/// <param name="links">The link collection to query.</param>
	/// <param name="relation">The link relation to find.</param>
	/// <returns>The link object collection, or null if the link relation is not found.</returns>
	public static LinkObjectCollection? GetLinkObjects(this LinkCollection links, string relation)
		=> links?.GetLinkOrDefault(relation)?.LinkObjects;

	/// <summary>
	/// Gets the link object with the specified relation and name.
	/// </summary>
	/// <param name="links">The link collection to query.</param>
	/// <param name="linkRelation">The link relation to find.</param>
	/// <param name="linkObjectName">The name of the specific link object within the relation.</param>
	/// <returns>The link object, or null if not found.</returns>
	public static LinkObject? GetLinkObjectOrDefault(this LinkCollection links, string linkRelation, string linkObjectName)
		=> links?.GetLinkObjects(linkRelation)?.GetLinkObjectOrDefault(linkObjectName);

	/// <summary>
	/// Gets the first link object with the specified relation.
	/// </summary>
	/// <param name="links">The link collection to query.</param>
	/// <param name="linkRelation">The link relation to find.</param>
	/// <returns>The first link object for the relation, or null if not found.</returns>
	public static LinkObject? GetLinkObjectOrDefault(this LinkCollection links, string linkRelation)
		=> links?.GetLinkObjects(linkRelation)?.SingleOrDefault();

	/// <summary>
	/// Expands a compact URI relation (CURIE) to its full URI form using CURIE templates
	/// defined in the "curies" link relation. If the relation is not a CURIE or no matching
	/// CURIE definition is found, returns the original relation unchanged.
	/// </summary>
	/// <param name="links">The link collection to search for CURIE definitions.</param>
	/// <param name="relation">The relation string to expand (e.g., "acme:widgets").</param>
	/// <returns>
	/// The expanded URI if a matching CURIE is found, otherwise the original relation string.
	/// Returns an empty string if the relation parameter is null or empty.
	/// </returns>
	/// <remarks>
	/// CURIEs allow shortening long relation URIs. For example, with a CURIE definition:
	/// { "name": "acme", "href": "https://docs.acme.com/relations/{rel}", "templated": true }
	/// The relation "acme:widgets" expands to "https://docs.acme.com/relations/widgets".
	///
	/// Edge cases handled:
	/// - Relations without a colon separator are returned unchanged
	/// - Undefined CURIE prefixes return the original relation
	/// - Missing "curies" link relation returns the original relation
	/// - CURIE templates without the {rel} token return the original relation
	/// </remarks>
	public static string ExpandCurieRelation(this LinkCollection links, string relation)
	{
		if (string.IsNullOrEmpty(relation))
			return string.Empty;

		// Parse the relation to extract prefix and suffix
		var colonIndex = relation.IndexOf(':');
		if (colonIndex <= 0)
			return relation;

		var prefix = relation.Substring(0, colonIndex);
		var suffix = colonIndex < relation.Length - 1 ? relation.Substring(colonIndex + 1) : string.Empty;

		// Look up the CURIE definition in the "curies" link relation
		var curiesLinkObjects = links?.GetLinkObjects("curies");
		if (curiesLinkObjects == null || !curiesLinkObjects.Any())
			return relation;

		// Find the LinkObject with matching name
		var curieDefinition = curiesLinkObjects.FirstOrDefault(lo => lo.Name?.Equals(prefix) == true);
		if (curieDefinition == null)
			return relation;

		// Replace {rel} token in the template
		var template = curieDefinition.Href;
		if (!template.Contains("{rel}"))
			return relation;

		return template.Replace("{rel}", suffix);
	}
}
