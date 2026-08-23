using System.Text.Json;
using Chatter.Rest.Hal;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

/// <summary>
/// A resource constructed with a <see cref="JsonElement"/> state supports projection onto several
/// state types, the same multi-type projection contract parsed resources have.
/// </summary>
public class JsonElementStateProjectionTests
{
	private sealed class StateA
	{
		public string? Name { get; set; }
	}

	private sealed class StateB
	{
		public int Count { get; set; }
	}

	[Fact]
	public void SecondProjectionOntoDifferentTypeStillMaterializes()
	{
		using var doc = JsonDocument.Parse("{\"name\":\"widget\",\"count\":3}");
		var resource = new Resource(doc.RootElement.Clone());

		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var a = resource.State<StateA>(options);
		var b = resource.State<StateB>(options);

		Assert.NotNull(a);
		Assert.Equal("widget", a!.Name);
		Assert.NotNull(b);
		Assert.Equal(3, b!.Count);
	}
}
