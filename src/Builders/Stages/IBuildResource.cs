﻿using Chatter.Rest.Hal.Builders.Stages.Embedded;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders.Stages;

public interface IBuildResource : IEmbeddedResource, IResource, IAddEmbeddedResourceToResourceStage
{
}
