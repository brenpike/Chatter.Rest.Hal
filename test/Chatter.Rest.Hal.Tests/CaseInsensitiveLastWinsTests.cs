using System.Text.Json;
using Chatter.Rest.Hal;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

/// <summary>
/// Under <see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/>, multiple case variants of
/// the same attribute resolve last-wins, consistent with duplicate-key normalization elsewhere.
/// </summary>
public class CaseInsensitiveLastWinsTests
{
	[Fact]
	public void LastCaseVariantWinsWhenNoExactMatchExists()
	{
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		options.AddHalConverters();

		var link = JsonSerializer.Deserialize<Link>(
			"{\"self\":{\"HREF\":\"/first\",\"Href\":\"/second\"}}", options);

		Assert.NotNull(link);
		var href = Assert.Single(link!.LinkObjects).Href;
		Assert.Equal("/second", href);
	}

	[Fact]
	public void ExactMatchStillBeatsCaseVariants()
	{
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		options.AddHalConverters();

		var link = JsonSerializer.Deserialize<Link>(
			"{\"self\":{\"HREF\":\"/variant\",\"href\":\"/exact\"}}", options);

		Assert.NotNull(link);
		var href = Assert.Single(link!.LinkObjects).Href;
		Assert.Equal("/exact", href);
	}
}
