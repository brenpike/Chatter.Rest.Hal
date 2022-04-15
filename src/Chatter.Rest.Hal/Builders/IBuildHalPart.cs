namespace Chatter.Rest.Hal.Builders;

public interface IBuildHalPart<out THalPart> where THalPart : class, IHalPart
{
	IBuildHalPart<IHalPart>? Parent { get; }
	IBuildHalPart<TParent>? FindParent<TParent>() where TParent : class, IHalPart;
	IBuildHalPart<IHalPart> FindRoot();
	THalPart BuildPart();
}
