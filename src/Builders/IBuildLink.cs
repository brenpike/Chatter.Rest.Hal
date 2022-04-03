namespace Chatter.Rest.Hal.Builders;

public interface IBuildLink
{
	public IBuildLink AddLinkObject(IBuildLinkObject linkObjectBuilder);
	public Link Build();
}
