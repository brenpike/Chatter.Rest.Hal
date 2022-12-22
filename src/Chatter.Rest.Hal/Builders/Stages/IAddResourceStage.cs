using Chatter.Rest.Hal.Builders.Stages.Embedded;
using System;
using System.Collections.Generic;

namespace Chatter.Rest.Hal.Builders.Stages;

public interface IAddResourceStage
{
	IEmbeddedResourceCreationStage AddResource();
	IEmbeddedResourceCreationStage AddResource(object? state);
	IEmbeddedResourceCreationStage AddResources<T>(IEnumerable<T> resources, Action<T, IEmbeddedResourceCreationStage>? builder = null);
}
