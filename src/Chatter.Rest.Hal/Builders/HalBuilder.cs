namespace Chatter.Rest.Hal.Builders;

/// <summary>
/// Abstract base class for all HAL builders, implementing the core builder hierarchy logic.
/// </summary>
/// <typeparam name="THalPart">The type of HAL domain object this builder produces.</typeparam>
public abstract class HalBuilder<THalPart> : IBuildResource, IBuildHalPart<THalPart> where THalPart : class, IHalPart
{
	/// <summary>
	/// Initializes a new instance of the <see cref="HalBuilder{THalPart}"/> class.
	/// </summary>
	/// <param name="parent">The parent builder, or null if this is the root builder.</param>
	public HalBuilder(IBuildHalPart<IHalPart>? parent) => Parent = parent;

	/// <summary>
	/// Gets the parent builder in the builder hierarchy.
	/// </summary>
	public IBuildHalPart<IHalPart>? Parent { get; }

	/// <summary>
	/// Finds the nearest parent builder that produces the specified HAL type.
	/// </summary>
	/// <typeparam name="TParent">The type of HAL domain object to find.</typeparam>
	/// <returns>The parent builder, or null if not found.</returns>
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

	/// <summary>
	/// Finds the root builder in the builder hierarchy.
	/// </summary>
	/// <returns>The root builder.</returns>
	public IBuildHalPart<IHalPart> FindRoot()
	{
		if (!IsRoot()) return Parent!.FindRoot();
		return this;
	}

	/// <summary>
	/// Determines whether this builder is the root of the hierarchy.
	/// </summary>
	/// <returns>true if this is the root builder; otherwise, false.</returns>
	protected bool IsRoot() => Parent == null;

	/// <summary>
	/// Determines whether this builder is the root and produces the specified HAL type.
	/// </summary>
	/// <typeparam name="TRoot">The type of HAL domain object to check.</typeparam>
	/// <returns>true if this is the root builder and produces <typeparamref name="TRoot"/>; otherwise, false.</returns>
	protected bool IsRoot<TRoot>() where TRoot : class, IHalPart
		=> Parent == null && this is IBuildHalPart<TRoot>;

	/// <summary>
	/// Builds the HAL domain object represented by this builder.
	/// </summary>
	/// <returns>The constructed HAL domain object.</returns>
	public abstract THalPart BuildPart();

	/// <summary>
	/// Builds the complete Resource by walking up to the root builder.
	/// </summary>
	/// <returns>The constructed Resource, or null if the root is not a Resource builder.</returns>
	public Resource? Build()
	{
		if (FindRoot() is IBuildHalPart<Resource> resourceBuilder)
		{
			return resourceBuilder.BuildPart();
		}

		return null;
	}
}
