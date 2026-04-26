using System.Text.Json;
using System.Linq;
using Xunit;
using Chatter.Rest.Hal.Builders;

namespace Chatter.Rest.Hal.Tests;

public class ResourceBehaviorTests
{
	public class SimpleState
	{
		public int Value { get; set; }
		public string? Name { get; set; }
	}

	[Fact]
	public void State_Should_Cache_Deserialized_Object_When_Starting_From_JsonElement()
	{
		var json = JsonSerializer.Serialize(new SimpleState { Value = 42, Name = "the-answer" });
		var je = JsonSerializer.Deserialize<object>(json);
		var res = ResourceBuilder.WithState(je!).Build();

		var first = res!.State<SimpleState>();
		var second = res.State<SimpleState>();

		Assert.NotNull(first);
		Assert.NotNull(second);
		Assert.Same(first, second); // should be cached after first deserialization
	}

	[Fact]
	public void As_Should_RoundTrip_Resource_To_Typed_State()
	{
		var state = new SimpleState { Value = 7, Name = "seven" };
		var res = ResourceBuilder.WithState(state).Build()!;

		// Convert the entire resource into the simple state type. This uses Resource.As<T>()
		var asState = res.As<SimpleState>();

		Assert.NotNull(asState);
		Assert.Equal(state.Value, asState!.Value);
		Assert.Equal(state.Name, asState.Name);
	}

	[Fact]
	public void Resource_Serialization_And_Deserialization_Should_Preserve_Links_And_Embedded()
	{
		var res = ResourceBuilder.WithState(new SimpleState { Value = 1, Name = "one" })
			.AddLink("self").AddLinkObject("/items/1")
			.AddEmbedded("child").AddResource(new SimpleState { Value = 2, Name = "two" })
			.Build()!;

		var json = JsonSerializer.Serialize(res);

		// deserialize back to Resource and validate
		var round = JsonSerializer.Deserialize<Resource>(json);
		Assert.NotNull(round);
		Assert.Single(round!.Links);
		Assert.Equal("self", round.Links.Single().Rel);
		Assert.Single(round.Links.Single().LinkObjects);
		Assert.Single(round.Embedded);
		Assert.Equal("child", round.Embedded.Single().Name);
		Assert.Single(round.Embedded.Single().Resources);
	}

	// --- As<T>(JsonSerializerOptions?) tests ---

	private record AsDto(string Name, int Value);

	[Fact]
	public void As_Should_Respect_Custom_Options_When_Converting_Resource_To_Typed_Object()
	{
		// Arrange: build a Resource with state that has a custom name mapping
		var state = new { name = "test", value = 42 };
		var resource = new Resource(state);
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

		// Serialize with options, then deserialize As<T> with same options
		var result = resource.As<AsDto>(options);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("test", result!.Name);
		Assert.Equal(42, result.Value);
	}

	[Fact]
	public void As_With_Null_Options_Should_Behave_Same_As_Parameterless_Overload()
	{
		// Arrange
		var state = new { Name = "hello", Value = 99 };
		var r1 = new Resource(state);
		var r2 = new Resource(state);

		// Act
		var withNull = r1.As<AsDto>(null);
		var parameterless = r2.As<AsDto>();

		// Assert
		Assert.NotNull(withNull);
		Assert.NotNull(parameterless);
		Assert.Equal(withNull!.Name, parameterless!.Name);
		Assert.Equal(withNull.Value, parameterless.Value);
	}
}