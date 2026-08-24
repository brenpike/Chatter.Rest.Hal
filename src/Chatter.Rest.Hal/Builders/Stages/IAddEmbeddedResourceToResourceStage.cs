using System;

namespace Chatter.Rest.Hal.Builders.Stages;

/// <summary>
/// A stage of the fluent builder from which a named embedded resource entry can be added to the
/// "_embedded" property of the resource being built. Available from both the root-resource and
/// embedded builder contexts.
/// </summary>
public interface IAddEmbeddedResourceToResourceStage
{
	/// <summary>
	/// Adds an embedded resource entry with the supplied name to the resource being built
	/// </summary>
	/// <param name="name">The name that keys the entry within the resource's "_embedded" property</param>
	/// <returns>An <see cref="IAddResourceStage"/> to add one or more resources to the named entry</returns>
	/// <remarks>
	/// https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-4.1.2
	///
	/// 4.1.2. The reserved "_embedded" property is OPTIONAL.
	///
	/// It is an object whose property names are link relation types (as
	/// defined by [RFC5988]) and values are either a Resource Object or an
	/// array of Resource Objects.
	/// </remarks>
	IAddResourceStage AddEmbedded(string name);
}
