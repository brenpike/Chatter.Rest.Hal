namespace Chatter.Rest.Hal.Builders;

public interface IBuildResource
{
	public IBuildResource AddLink(IBuildLink linkBuilder);
	public IBuildResource AddEmbedded(IBuildEmbeddedResource embeddedBuilder);
	public Resource Build();
}
