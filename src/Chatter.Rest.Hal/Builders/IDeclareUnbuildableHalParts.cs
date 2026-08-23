using System;

namespace Chatter.Rest.Hal.Builders;

/// <summary>
/// Implemented by builders that satisfy <see cref="IBuildHalPart{THalPart}"/> for a HAL part
/// they cannot actually build, usually because a staged interface forces the implementation.
/// </summary>
/// <remarks>
/// <see cref="HalBuilder{THalPart}.FindParent{TParent}"/> skips such builders so an ancestor
/// lookup never resolves to a builder whose BuildPart() throws.
/// </remarks>
internal interface IDeclareUnbuildableHalParts
{
	/// <summary>
	/// Determines whether this builder is unable to build the specified HAL part type.
	/// </summary>
	/// <param name="halPartType">The HAL part type being looked up.</param>
	/// <returns>true if this builder cannot build <paramref name="halPartType"/>; otherwise, false.</returns>
	bool CannotBuild(Type halPartType);
}
