using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;

namespace Chatter.Rest.Hal.Tests
{
	/// <summary>
	/// Test helper utilities and builders used across HAL tests.
	/// Provides simple helpers to read JSON fixtures, load Resources and create small Link/Resource instances.
	/// </summary>
	public static class TestHelpers
	{
		private static string FixtureDirectory => Path.Combine(AppContext.BaseDirectory, "Json");

		/// <summary>
		/// Reads a JSON fixture file from the test project's Json folder and returns a JsonNode.
		/// The returned JsonNode is suitable for use with the project's converters and JsonSerializer APIs.
		/// </summary>
		public static JsonNode ReadJsonFixture(string name)
		{
			var path = Path.Combine(FixtureDirectory, name);
			if (!File.Exists(path))
				throw new FileNotFoundException($"Fixture not found: {path}", path);

			var text = File.ReadAllText(path);
			return JsonNode.Parse(text, new JsonNodeOptions { PropertyNameCaseInsensitive = true })!;
		}

		/// <summary>
		/// Loads a Resource from a fixture JSON by name. Useful for assertions against Links/Embedded/State.
		/// </summary>
		public static Chatter.Rest.Hal.Resource LoadResourceFromFixture(string name)
		{
			var node = ReadJsonFixture(name);
			var resource = node.Deserialize<Chatter.Rest.Hal.Resource>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
			if (resource == null)
				throw new InvalidOperationException($"Unable to deserialize fixture '{name}' to Resource.");
			return resource;
		}

		/// <summary>
		/// Asserts that the provided JSON contains the expected fragment when both are normalized.
		/// Normalization is performed by parsing and re-serializing with default (compact) formatting.
		/// </summary>
		public static void AssertJsonContainsNormalized(string json, string expectedFragment)
		{
			var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
			var fragment = JsonNode.Parse(expectedFragment, new JsonNodeOptions { PropertyNameCaseInsensitive = true });

			var normalized = node!.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
			var expectedNormalized = fragment!.ToJsonString(new JsonSerializerOptions { WriteIndented = false });

			normalized.Should().Contain(expectedNormalized);
		}

		/// <summary>
		/// Small factory helper for creating a LinkObject for use in tests.
		/// </summary>
		public static Chatter.Rest.Hal.LinkObject CreateLinkObject(string href, bool? templated = null)
		{
			var lo = new Chatter.Rest.Hal.LinkObject(href);
			lo.Templated = templated;
			return lo;
		}

		/// <summary>
		/// Small factory helper for creating a Link (with a single LinkObject) for use in tests.
		/// </summary>
		public static Chatter.Rest.Hal.Link CreateLink(string rel, string href, bool? templated = null)
		{
			var l = new Chatter.Rest.Hal.Link(rel);
			l.LinkObjects.Add(CreateLinkObject(href, templated));
			return l;
		}

		/// <summary>
		/// Convenience helper to create a Resource that contains a single link relation.
		/// </summary>
		public static Chatter.Rest.Hal.Resource CreateResourceWithLink(string rel, string href, bool? templated = null)
		{
			var resource = new Chatter.Rest.Hal.Resource();
			resource.Links.Add(CreateLink(rel, href, templated));
			return resource;
		}
	}
}
