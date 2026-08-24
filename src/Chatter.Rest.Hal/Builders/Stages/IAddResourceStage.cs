using Chatter.Rest.Hal.Builders.Stages.Embedded;
using System;
using System.Collections.Generic;

namespace Chatter.Rest.Hal.Builders.Stages;

/// <summary>
/// A stage of the fluent builder, returned by
/// <see cref="IAddEmbeddedResourceToResourceStage.AddEmbedded(string)"/>, from which individual
/// resources or a batch of resources can be added to the named "_embedded" entry.
/// </summary>
public interface IAddResourceStage
{
	/// <summary>
	/// Adds a resource with no state to the "_embedded" entry being built
	/// </summary>
	/// <returns>An <see cref="IEmbeddedResourceCreationStage"/> to build the new embedded resource</returns>
	IEmbeddedResourceCreationStage AddResource();
	/// <summary>
	/// Adds a resource with the supplied state to the "_embedded" entry being built
	/// </summary>
	/// <param name="state">The OPTIONAL state object whose properties become the embedded resource's state</param>
	/// <returns>An <see cref="IEmbeddedResourceCreationStage"/> to build the new embedded resource</returns>
	IEmbeddedResourceCreationStage AddResource(object? state);
	/// <summary>
	/// Adds one resource per item of <paramref name="resources"/> to the "_embedded" entry being
	/// built, each item becoming that resource's state. Also forces the entry to serialize as a
	/// JSON array even when it contains a single resource.
	/// </summary>
	/// <typeparam name="T">The type of the items in <paramref name="resources"/></typeparam>
	/// <param name="resources">The items to add as embedded resources</param>
	/// <param name="builder">An OPTIONAL callback invoked with each item and its
	/// <see cref="IEmbeddedResourceCreationStage"/> to configure per-resource links, curies, and
	/// embedded resources</param>
	/// <returns>An <see cref="IEmbeddedResourceCreationStage"/> to continue building. Unlike
	/// <see cref="AddResource()"/>, the returned stage is the collection builder, NOT one of the
	/// added resources: link/curies/embed calls chained on it target the resource that owns the
	/// "_embedded" entry. Per-resource configuration happens only inside the
	/// <paramref name="builder"/> callback</returns>
	IEmbeddedResourceCreationStage AddResources<T>(IEnumerable<T> resources, Action<T, IEmbeddedResourceCreationStage>? builder = null);
}
