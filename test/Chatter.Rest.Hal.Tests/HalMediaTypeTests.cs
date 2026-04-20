using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests
{
	public class HalMediaTypeTests
	{
		[Fact]
		public void MediaType_Constant_Is_Correct()
		{
			// Spec Section 4: The media type for HAL is application/hal+json
			Resource.MediaType.Should().Be("application/hal+json");
		}
	}
}
