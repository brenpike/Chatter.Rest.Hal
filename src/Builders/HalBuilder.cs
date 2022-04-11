namespace Chatter.Rest.Hal.Builders;

public abstract class HalBuilder<THalPart> : IBuildResource, IBuildHalPart<THalPart> where THalPart : class, IHalPart
{
	public HalBuilder(IBuildHalPart<IHalPart>? parent) => Parent = parent;

	public IBuildHalPart<IHalPart>? Parent { get; }

	public IBuildHalPart<TParent>? FindParent<TParent>() where TParent : class, IHalPart
	{
		if (this is IBuildHalPart<TParent> rootBuilder)
			return rootBuilder;

		if (IsRoot())
			return null;

		if (Parent is IBuildHalPart<TParent> parentBuilder)
			return parentBuilder;

		return Parent!.FindParent<TParent>();
	}

	public IBuildHalPart<IHalPart> FindRoot()
	{
		if (!IsRoot()) return Parent!.FindRoot();
		return this;
	}

	protected bool IsRoot() => Parent == null;
	protected bool IsRoot<TRoot>() where TRoot : class, IHalPart 
		=> Parent == null && this is IBuildHalPart<TRoot>;

	public abstract THalPart BuildPart();

	public Resource? Build()
	{
		if (FindRoot() is IBuildHalPart<Resource> resourceBuilder)
		{
			return resourceBuilder.BuildPart();
		}

		return null;
	}
}
