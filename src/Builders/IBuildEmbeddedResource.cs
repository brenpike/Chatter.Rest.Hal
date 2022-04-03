namespace Chatter.Rest.Hal.Builders;

public interface IBuildEmbeddedResource
{
	public IBuildEmbeddedResource AddResource(IBuildResource resourceBuilder);
	public EmbeddedResource Build();
}
