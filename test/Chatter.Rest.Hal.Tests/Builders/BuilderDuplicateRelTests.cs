using System.Linq;
using System.Text.Json;
using Chatter.Rest.Hal.Builders;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Builders;

/// <summary>
/// Regression coverage for issue #104: repeated relations produced one Link per call, which the
/// converter wrote as duplicate JSON member names in "_links".
/// </summary>
public class BuilderDuplicateRelTests
{
	[Fact]
	public void Repeated_AddLink_For_The_Same_Rel_Merges_Into_One_Link()
	{
		// Verified failing sequence from #104: this produced {"_links":{"a":{...},"a":{...}}}.
		var resource = ResourceBuilder.New()
			.AddLink("a").AddLinkObject("/1")
			.AddLink("a").AddLinkObject("/2")
			.Build();

		resource.Should().NotBeNull();

		var a = resource!.Links.Should().ContainSingle(l => l.Rel == "a").Subject;
		a.LinkObjects.Select(lo => lo.Href).Should().BeEquivalentTo(new[] { "/1", "/2" });

		var json = JsonSerializer.Serialize(resource);
		using var doc = JsonDocument.Parse(json);
		var links = doc.RootElement.GetProperty("_links");

		links.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(new[] { "a" });
		links.GetProperty("a").ValueKind.Should().Be(JsonValueKind.Array);
		links.GetProperty("a").EnumerateArray().Select(e => e.GetProperty("href").GetString())
			 .Should().BeEquivalentTo(new[] { "/1", "/2" });
	}

	[Fact]
	public void Repeated_AddSelf_Merges_Into_One_Self_Link()
	{
		var resource = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/1")
			.AddSelf().AddLinkObject("/2")
			.Build();

		resource.Should().NotBeNull();

		var self = resource!.Links.Should().ContainSingle(l => l.Rel == "self").Subject;
		self.LinkObjects.Select(lo => lo.Href).Should().BeEquivalentTo(new[] { "/1", "/2" });

		var json = JsonSerializer.Serialize(resource);
		using var doc = JsonDocument.Parse(json);
		doc.RootElement.GetProperty("_links").EnumerateObject().Select(p => p.Name)
		   .Should().BeEquivalentTo(new[] { "self" });
	}

	[Fact]
	public void Repeated_AddCuries_Extends_The_Single_Curies_Array()
	{
		var resource = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/orders")
			.AddCuries().AddLinkObject("http://example.com/docs/{rel}", "ex")
			.AddCuries().AddLinkObject("http://acme.com/docs/{rel}", "acme")
			.Build();

		resource.Should().NotBeNull();

		var curies = resource!.Links.Should().ContainSingle(l => l.Rel == "curies").Subject;
		curies.LinkObjects.Select(lo => lo.Name).Should().BeEquivalentTo(new[] { "ex", "acme" });

		var json = JsonSerializer.Serialize(resource);
		using var doc = JsonDocument.Parse(json);
		var curiesElement = doc.RootElement.GetProperty("_links").GetProperty("curies");

		curiesElement.ValueKind.Should().Be(JsonValueKind.Array);
		curiesElement.EnumerateArray().Select(e => e.GetProperty("name").GetString())
					 .Should().BeEquivalentTo(new[] { "ex", "acme" });
	}

	[Fact]
	public void AddLink_Self_And_AddSelf_Share_The_Same_Link()
	{
		var resource = ResourceBuilder.New()
			.AddLink("self").AddLinkObject("/1")
			.AddSelf().AddLinkObject("/2")
			.Build();

		resource.Should().NotBeNull();

		var self = resource!.Links.Should().ContainSingle(l => l.Rel == "self").Subject;
		self.LinkObjects.Select(lo => lo.Href).Should().BeEquivalentTo(new[] { "/1", "/2" });
	}

	[Fact]
	public void Merging_Preserves_First_Insertion_Order_And_Leaves_Other_Rels_Alone()
	{
		var resource = ResourceBuilder.New()
			.AddLink("a").AddLinkObject("/a1")
			.AddLink("b").AddLinkObject("/b1")
			.AddLink("a").AddLinkObject("/a2")
			.Build();

		resource.Should().NotBeNull();
		resource!.Links.Select(l => l.Rel).Should().ContainInOrder("a", "b");
		resource.Links.Should().HaveCount(2);
		resource.Links.Single(l => l.Rel == "b").LinkObjects.Should().HaveCount(1);
	}

	[Fact]
	public void Rels_Are_Merged_Case_Sensitively()
	{
		// HAL relations are case-sensitive, so "a" and "A" stay distinct links.
		var resource = ResourceBuilder.New()
			.AddLink("a").AddLinkObject("/lower")
			.AddLink("A").AddLinkObject("/upper")
			.Build();

		resource.Should().NotBeNull();
		resource!.Links.Select(l => l.Rel).Should().BeEquivalentTo(new[] { "a", "A" });
	}

	[Fact]
	public void Repeated_Rels_On_An_Embedded_Resource_Also_Merge()
	{
		var resource = ResourceBuilder.New()
			.AddEmbedded("orders")
				.AddResource(new { id = 1 })
					.AddLink("a").AddLinkObject("/1")
					.AddLink("a").AddLinkObject("/2")
			.Build();

		resource.Should().NotBeNull();

		var order = resource!.Embedded.Single(e => e.Name == "orders").Resources.Single();
		var a = order.Links.Should().ContainSingle(l => l.Rel == "a").Subject;
		a.LinkObjects.Select(lo => lo.Href).Should().BeEquivalentTo(new[] { "/1", "/2" });
	}

	[Fact]
	public void AsArray_On_A_Merged_Rel_Still_Applies()
	{
		var resource = ResourceBuilder.New()
			.AddLink("a").AddLinkObject("/1")
			.AddLink("a").AsArray().AddLinkObject("/2")
			.Build();

		resource.Should().NotBeNull();
		resource!.Links.Single(l => l.Rel == "a").IsArray.Should().BeTrue();
	}
}
