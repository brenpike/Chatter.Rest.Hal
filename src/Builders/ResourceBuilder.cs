using System.Collections.ObjectModel;

namespace Chatter.Rest.Hal.Builders;

public class ResourceBuilder : IBuildResource
{
	private readonly object? _state;
	private readonly LinkCollection _links = new();
	private readonly EmbeddedResourceCollection _embedded = new();

	private ResourceBuilder(object? state) => _state = state;

	public static IBuildResource New() => new ResourceBuilder(null);
	public static IBuildResource New(object state) => new ResourceBuilder(state);

	public IBuildResource AddLink(IBuildLink linkBuilder)
	{
		_links.Add(linkBuilder.Build());
		return this;
	}

	public IBuildResource AddEmbedded(IBuildEmbeddedResource embeddedBuilder)
	{
		_embedded.Add(embeddedBuilder.Build());
		return this;
	}

	Resource IBuildResource.Build()
	{
		return new Resource(_state)
		{
			Links = _links,
			EmbeddedResources = _embedded
		};
	}
}
