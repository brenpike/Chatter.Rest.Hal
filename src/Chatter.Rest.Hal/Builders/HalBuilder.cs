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
	/// <remarks>
	/// This constructor is intentionally public to allow external subclassing for custom
	/// builder implementations that extend the HAL builder hierarchy.
	/// </remarks>
	public HalBuilder(IBuildHalPart<IHalPart>? parent) => Parent = parent;

	/// <summary>
	/// Gets the parent builder in the builder hierarchy.
	/// </summary>
	public IBuildHalPart<IHalPart>? Parent { get; }

	/// <summary>
	/// Finds the nearest ancestor builder that produces the specified HAL type.
	/// </summary>
	/// <typeparam name="TParent">The type of HAL domain object to find.</typeparam>
	/// <returns>The ancestor builder, or null if not found.</returns>
	/// <remarks>
	/// The search starts at <see cref="Parent"/>: this builder is never its own parent, so a
	/// builder that happens to satisfy <see cref="IBuildHalPart{THalPart}"/> for
	/// <typeparamref name="TParent"/> can no longer resolve to itself. Builders that declare
	/// <typeparamref name="TParent"/> unbuildable via <see cref="IDeclareUnbuildableHalParts"/>
	/// are skipped so a lookup never resolves to a builder whose BuildPart() throws.
	/// </remarks>
	public IBuildHalPart<TParent>? FindParent<TParent>() where TParent : class, IHalPart
	{
		for (var candidate = Parent; candidate is not null; candidate = candidate.Parent)
		{
			if (candidate is IBuildHalPart<TParent> match && !DeclaresUnbuildable<TParent>(candidate))
			{
				return match;
			}
		}

		return null;
	}

	private static bool DeclaresUnbuildable<TParent>(IBuildHalPart<IHalPart> candidate) where TParent : class, IHalPart
		=> candidate is IDeclareUnbuildableHalParts declaring && declaring.CannotBuild(typeof(TParent));

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
