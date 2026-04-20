namespace Chatter.Rest.Hal.Builders;

/// <summary>
/// Defines the contract for builders that construct HAL domain types.
/// </summary>
/// <typeparam name="THalPart">The type of HAL domain object this builder produces.</typeparam>
public interface IBuildHalPart<out THalPart> where THalPart : class, IHalPart
{
	/// <summary>
	/// Gets the parent builder in the builder hierarchy.
	/// </summary>
	IBuildHalPart<IHalPart>? Parent { get; }

	/// <summary>
	/// Finds the nearest parent builder that produces the specified HAL type.
	/// </summary>
	/// <typeparam name="TParent">The type of HAL domain object to find.</typeparam>
	/// <returns>The parent builder, or null if not found.</returns>
	IBuildHalPart<TParent>? FindParent<TParent>() where TParent : class, IHalPart;

	/// <summary>
	/// Finds the root builder in the builder hierarchy.
	/// </summary>
	/// <returns>The root builder.</returns>
	IBuildHalPart<IHalPart> FindRoot();

	/// <summary>
	/// Builds the HAL domain object represented by this builder.
	/// </summary>
	/// <returns>The constructed HAL domain object.</returns>
	THalPart BuildPart();
}
