using System;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.CodeGenerators.Tests;

/// <summary>
/// Real-build coverage for issue #106: these assertions only pass if the generator produced usable
/// members on the generic and nested targets compiled into this very assembly.
/// </summary>
public class GeneratedTypeShapeTests
{
	[Fact]
	public void Generic_Target_Has_Hal_Members()
	{
		var responseType = typeof(GenericPersonResponse<string>);

		responseType.GetProperty("Links")!.PropertyType.Should().Be(typeof(Chatter.Rest.Hal.LinkCollection));
		responseType.GetProperty("Embedded")!.PropertyType.Should().Be(typeof(Chatter.Rest.Hal.EmbeddedResourceCollection));
	}

	[Fact]
	public void Generic_Target_Has_No_Arity_Zero_Companion()
	{
		var assembly = typeof(GenericPersonResponse<string>).Assembly;

		assembly.GetType("Chatter.Rest.Hal.CodeGenerators.Tests.GenericPersonResponse", throwOnError: false)
			.Should().BeNull("the generator must not emit a non-generic companion for a generic target");
	}

	[Fact]
	public void Nested_Targets_With_The_Same_Name_Both_Have_Hal_Members()
	{
		typeof(ResponseContainerA.InnerResponse).GetProperty("Links").Should().NotBeNull();
		typeof(ResponseContainerA.InnerResponse).GetProperty("Embedded").Should().NotBeNull();
		typeof(ResponseContainerB.InnerResponse).GetProperty("Links").Should().NotBeNull();
		typeof(ResponseContainerB.InnerResponse).GetProperty("Embedded").Should().NotBeNull();
	}

	[Fact]
	public void Nested_Target_Has_No_TopLevel_Companion()
	{
		var assembly = typeof(ResponseContainerA.InnerResponse).Assembly;

		assembly.GetType("Chatter.Rest.Hal.CodeGenerators.Tests.InnerResponse", throwOnError: false)
			.Should().BeNull("the generator must not emit a stray top-level companion for a nested target");
	}

	[Fact]
	public void Generated_Members_Carry_The_Hal_Reserved_Property_Names()
	{
		var linksAttribute = typeof(Person).GetProperty("Links")!
			.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), inherit: false);
		var embeddedAttribute = typeof(Person).GetProperty("Embedded")!
			.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), inherit: false);

		((System.Text.Json.Serialization.JsonPropertyNameAttribute)linksAttribute[0]).Name.Should().Be("_links");
		((System.Text.Json.Serialization.JsonPropertyNameAttribute)embeddedAttribute[0]).Name.Should().Be("_embedded");
	}
}
