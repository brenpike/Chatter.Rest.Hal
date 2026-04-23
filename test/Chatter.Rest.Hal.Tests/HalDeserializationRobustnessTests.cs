using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests
{
	public class HalDeserializationRobustnessTests
	{
		[Fact]
		public void Missing__links_Property_Produces_Empty_LinksCollection()
		{
			var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>("{}");

			Assert.NotNull(res);
			Assert.Empty(res!.Links);
		}

		[Fact]
		public void Invalid_LinkObject_Shape_Returns_Null_LinkObject()
		{
			var json = "{ \"_links\": { \"self\": { \"title\": \"NoHref\" } } }";
			var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(json);

			Assert.NotNull(res);
			Assert.Single(res!.Links);
			var l = res.Links.Single();

			Assert.Equal("self", l.Rel);
			Assert.Empty(l.LinkObjects);
		}

		[Fact]
		public void Extra_Random_Properties_Are_Preserved_In_State()
		{
			var json = "{ \"foo\": \"bar\" }";
			var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(json);

			Assert.NotNull(res);

			var node = res!.As<JsonNode>();
			Assert.NotNull(node);

			var foo = node["foo"]?.GetValue<string>();
			Assert.Equal("bar", foo);
		}

		[Theory]
		[InlineData("[]")]
		[InlineData("[{\"foo\": \"bar\"}]")]
		public void Root_Array_Is_Rejected_Or_Returns_Null(string invalidJson)
		{
			// HAL spec states the root MUST be a Resource Object (JSON object), not an array.
			// Test that attempting to deserialize an array root either throws an exception or returns null.
			// Current behavior: deserialization returns a Resource but accessing properties throws InvalidOperationException.

			var exceptionThrown = false;

			try
			{
				var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(invalidJson);

				if (res == null)
				{
					// Null is acceptable behavior for invalid root types
					Assert.True(true, "Deserialization returned null for array root as expected");
					return;
				}

				// Try to access Links property to see if it throws
				_ = res.Links;

				// If we get here, deserialization succeeded unexpectedly
				Assert.True(false, "Expected deserialization to throw or return null for array root");
			}
			catch (JsonException)
			{
				exceptionThrown = true;
			}
			catch (InvalidOperationException)
			{
				exceptionThrown = true;
			}

			Assert.True(exceptionThrown, "Expected JsonException or InvalidOperationException for array root");
		}

		[Theory]
		[InlineData("\"string\"")]
		[InlineData("\"\"")]
		public void Root_String_Is_Rejected_Or_Returns_Null(string invalidJson)
		{
			// HAL spec states the root MUST be a Resource Object (JSON object), not a string.
			// Test that attempting to deserialize a string root either throws an exception or returns null.
			// Current behavior: deserialization returns a Resource but accessing properties throws InvalidOperationException.

			var exceptionThrown = false;

			try
			{
				var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(invalidJson);

				if (res == null)
				{
					// Null is acceptable behavior for invalid root types
					Assert.True(true, "Deserialization returned null for string root as expected");
					return;
				}

				// Try to access Links property to see if it throws
				_ = res.Links;

				// If we get here, deserialization succeeded unexpectedly
				Assert.True(false, "Expected deserialization to throw or return null for string root");
			}
			catch (JsonException)
			{
				exceptionThrown = true;
			}
			catch (InvalidOperationException)
			{
				exceptionThrown = true;
			}

			Assert.True(exceptionThrown, "Expected JsonException or InvalidOperationException for string root");
		}

		[Theory]
		[InlineData("42")]
		[InlineData("3.14")]
		[InlineData("-1")]
		[InlineData("0")]
		public void Root_Number_Is_Rejected_Or_Returns_Null(string invalidJson)
		{
			// HAL spec states the root MUST be a Resource Object (JSON object), not a number.
			// Test that attempting to deserialize a number root either throws an exception or returns null.
			// Current behavior: deserialization returns a Resource but accessing properties throws InvalidOperationException.

			var exceptionThrown = false;

			try
			{
				var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(invalidJson);

				if (res == null)
				{
					// Null is acceptable behavior for invalid root types
					Assert.True(true, "Deserialization returned null for number root as expected");
					return;
				}

				// Try to access Links property to see if it throws
				_ = res.Links;

				// If we get here, deserialization succeeded unexpectedly
				Assert.True(false, "Expected deserialization to throw or return null for number root");
			}
			catch (JsonException)
			{
				exceptionThrown = true;
			}
			catch (InvalidOperationException)
			{
				exceptionThrown = true;
			}

			Assert.True(exceptionThrown, "Expected JsonException or InvalidOperationException for number root");
		}

		[Theory]
		[InlineData("true")]
		[InlineData("false")]
		public void Root_Boolean_Is_Rejected_Or_Returns_Null(string invalidJson)
		{
			// HAL spec states the root MUST be a Resource Object (JSON object), not a boolean.
			// Test that attempting to deserialize a boolean root either throws an exception or returns null.
			// Current behavior: deserialization returns a Resource but accessing properties throws InvalidOperationException.

			var exceptionThrown = false;

			try
			{
				var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(invalidJson);

				if (res == null)
				{
					// Null is acceptable behavior for invalid root types
					Assert.True(true, "Deserialization returned null for boolean root as expected");
					return;
				}

				// Try to access Links property to see if it throws
				_ = res.Links;

				// If we get here, deserialization succeeded unexpectedly
				Assert.True(false, "Expected deserialization to throw or return null for boolean root");
			}
			catch (JsonException)
			{
				exceptionThrown = true;
			}
			catch (InvalidOperationException)
			{
				exceptionThrown = true;
			}

			Assert.True(exceptionThrown, "Expected JsonException or InvalidOperationException for boolean root");
		}

		[Fact]
		public void Root_Null_Is_Rejected_Or_Returns_Null()
		{
			// HAL spec states the root MUST be a Resource Object (JSON object), not null.
			// Test that attempting to deserialize a null root either throws an exception or returns null.
			// Current behavior: deserialization correctly returns null.

			var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>("null");

			// Null is the expected and acceptable behavior for null root
			Assert.Null(res);
		}
	}
}
