using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Converters;

public class ResourceConvertersTests
{
	[Fact]
	public void Embedded_Null_Value_Should_Create_Empty_EmbeddedResource()
	{
		var json = "{ \"_embedded\": { \"name\": null } }";

		var res = JsonSerializer.Deserialize<Resource>(json);

		Assert.NotNull(res);
		Assert.Single(res!.Embedded);
		var embedded = res.Embedded.Single();
		Assert.Equal("name", embedded!.Name);
		Assert.Empty(embedded.Resources);
	}

	[Fact]
	public void Embedded_Single_Object_Should_Create_EmbeddedResource_With_Resource()
	{
		var json = "{ \"_embedded\": { \"name\": { \"foo\": 1 } } }";

		var res = JsonSerializer.Deserialize<Resource>(json);
		Assert.NotNull(res);
		Assert.Single(res!.Embedded);
		var embedded = res.Embedded.Single();
		Assert.Equal("name", embedded!.Name);
		Assert.Single(embedded.Resources);
		var child = embedded.Resources.Single();
		// Child state should be available via State<object>() or As<JsonElement>
		var stateObj = child.State<object>();
		Assert.NotNull(stateObj);
	}

	[Fact]
	public void Resource_As_Typed_For_Primitive_Should_Return_Value()
	{
		var json = "123";
		var res = JsonSerializer.Deserialize<Resource>(json);
		Assert.NotNull(res);
		var asObj = res!.As<object>();
		// Deserializing a primitive into object yields a JsonElement boxed as object - verify numeric value
		Assert.Equal(123, ((System.Text.Json.JsonElement)asObj!).GetInt32());
	}

	[Fact]
	public void ResourceConverter_Read_Should_Thread_Options_Into_Resource_For_State_Deserialization()
	{
		// Arrange: camelCase JSON that would fail with default (case-sensitive) options
		var json = """{"firstName":"Alice","lastName":"Smith"}""";
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

		// Act: deserialize using ResourceConverter via JsonSerializer (options threaded in)
		var resource = JsonSerializer.Deserialize<Resource>(json, options);
		Assert.NotNull(resource);

		// State<T>() (parameterless) should use the options that were stored during Read()
		var state = resource!.State<ConverterStateDto>();

		// Assert
		Assert.NotNull(state);
		Assert.Equal("Alice", state!.FirstName);
		Assert.Equal("Smith", state.LastName);
	}

	private record ConverterStateDto(string FirstName, string LastName);
}
