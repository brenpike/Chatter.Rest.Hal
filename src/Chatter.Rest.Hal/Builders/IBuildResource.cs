namespace Chatter.Rest.Hal.Builders;

/// <summary>
/// Defines the contract for builders that can produce a complete Resource.
/// </summary>
public interface IBuildResource
{
	/// <summary>
	/// Builds the complete Resource by walking up to the root builder.
	/// </summary>
	/// <returns>The constructed Resource, or null if the root is not a Resource builder.</returns>
	Resource? Build();
}
