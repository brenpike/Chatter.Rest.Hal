using Chatter.Rest.Hal;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

/// <summary>
/// An empty state object serializes to the same HAL document as no state, so the two must compare
/// equal under HAL-content equality.
/// </summary>
public class EmptyStateEqualityTests
{
	[Fact]
	public void ParsedEmptyObjectEqualsConstructedEmptyResource()
	{
		var parsed = Resource.Parse("{}");
		var constructed = new Resource();

		Assert.Equal(constructed, parsed);
		Assert.Equal(constructed.GetHashCode(), parsed!.GetHashCode());
	}

	[Fact]
	public void EmptyStateStillDiffersFromNonEmptyState()
	{
		var parsed = Resource.Parse("{\"id\":1}");
		var constructed = new Resource();

		Assert.NotEqual(constructed, parsed);
	}
}
