using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

/// <summary>
/// HAL-content equality: shape flags that cannot affect the serialized document are ignored, and
/// reading state never changes a resource's hash code.
/// </summary>
public class EqualityShapeNormalizationTests
{
	private sealed class NarrowState
	{
		public string? Name { get; set; }
	}

	[Fact]
	public void IsArrayIgnoredWhenLinkHoldsMultipleObjects()
	{
		var a = new Link("items") { IsArray = false };
		a.LinkObjects.Add(new LinkObject("/1"));
		a.LinkObjects.Add(new LinkObject("/2"));

		var b = new Link("items") { IsArray = true };
		b.LinkObjects.Add(new LinkObject("/1"));
		b.LinkObjects.Add(new LinkObject("/2"));

		Assert.Equal(a, b);
		Assert.Equal(a.GetHashCode(), b.GetHashCode());
	}

	[Fact]
	public void IsArrayStillCountsWhenLinkHoldsOneObject()
	{
		var a = new Link("self") { IsArray = false };
		a.LinkObjects.Add(new LinkObject("/1"));

		var b = new Link("self") { IsArray = true };
		b.LinkObjects.Add(new LinkObject("/1"));

		Assert.NotEqual(a, b);
	}

	[Fact]
	public void WhenWritingNullMakesNullStatePropertiesAbsentInEquality()
	{
		var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

		var withNull = Resource.Parse("{\"x\":null}", options);
		var without = Resource.Parse("{}", options);

		// Both serialize as {} under WhenWritingNull, so they are the same HAL content.
		Assert.Equal(without, withNull);
		Assert.Equal(without!.GetHashCode(), withNull!.GetHashCode());
	}

	[Fact]
	public void NullStatePropertiesStillCountUnderDefaultOptions()
	{
		var withNull = Resource.Parse("{\"x\":null}");
		var without = Resource.Parse("{}");

		// Default options write the null, so the documents differ and so must equality.
		Assert.NotEqual(without, withNull);
	}

	[Fact]
	public void NullAndWhitespaceOptionalLinkObjectValuesCompareEqual()
	{
		// The converter omits null and whitespace-only optional values alike, so both forms
		// serialize to identical HAL and must compare equal.
		var a = new LinkObject("/orders") { Title = null, Name = "  " };
		var b = new LinkObject("/orders") { Title = " ", Name = null };

		Assert.Equal(a, b);
		Assert.Equal(a.GetHashCode(), b.GetHashCode());

		var c = new LinkObject("/orders") { Title = "Orders" };
		Assert.NotEqual(a, c);
	}

	[Fact]
	public void ForceWriteAsCollectionIgnoredWhenEmptyOrMultiple()
	{
		var a = new EmbeddedResource("orders") { ForceWriteAsCollection = false };
		var b = new EmbeddedResource("orders") { ForceWriteAsCollection = true };

		Assert.Equal(a, b);
		Assert.Equal(a.GetHashCode(), b.GetHashCode());
	}

	[Fact]
	public void MutatingProjectedStateAffectsNeitherEqualityNorSerialization()
	{
		var a = Resource.Parse("{\"name\":\"a\"}");
		var b = Resource.Parse("{\"name\":\"a\"}");

		var projected = a!.State<MutableState>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		projected!.Name = "b";

		// Projections are detached: the resource still serializes and compares as its original JSON.
		Assert.Equal(b, a);
		Assert.Equal(JsonSerializer.Serialize(b), JsonSerializer.Serialize(a));
	}

	private sealed class MutableState
	{
		public string? Name { get; set; }
	}

	[Fact]
	public void ReadingStateDoesNotChangeHashCodeForJsonElementResources()
	{
		using var doc = JsonDocument.Parse("{\"name\":\"widget\",\"extra\":1}");
		var resource = new Resource(doc.RootElement.Clone());
		var set = new HashSet<Resource> { resource };

		var before = resource.GetHashCode();
		// NarrowState drops the "extra" property; the key must still come from the original element.
		_ = resource.State<NarrowState>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		Assert.Equal(before, resource.GetHashCode());
		Assert.Contains(resource, set);
	}
}
