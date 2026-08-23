using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Chatter.Rest.Hal;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

/// <summary>
/// Hardening coverage for deeply nested untrusted documents: raised MaxDepth must not push the
/// duplicate pre-scan, the rebuild path, or resource materialization into stack overflow, and a
/// deeply nested <c>_embedded</c> chain must not be re-parsed once per ancestor.
/// </summary>
public class DeepNestingHardeningTests
{
	private static string BuildEmbeddedChain(int depth, string leafStateJson)
	{
		var sb = new StringBuilder();
		for (var i = 0; i < depth; i++)
		{
			sb.Append("{\"_embedded\":{\"child\":");
		}
		sb.Append(leafStateJson);
		for (var i = 0; i < depth; i++)
		{
			sb.Append("}}");
		}
		return sb.ToString();
	}

	private static JsonSerializerOptions DeepOptions(int maxDepth)
	{
		var options = new JsonSerializerOptions { MaxDepth = maxDepth };
		options.AddHalConverters();
		return options;
	}

	[Fact]
	public void RaisedMaxDepthDeepChainParsesWithoutStackOverflow()
	{
		// Each _embedded level consumes two JSON depth levels; 1500 levels needs MaxDepth ~3000.
		var json = BuildEmbeddedChain(1500, "{\"id\":1}");

		var resource = JsonSerializer.Deserialize<Resource>(json, DeepOptions(4096));

		Assert.NotNull(resource);
	}

	[Fact]
	public void RaisedMaxDepthDeepChainWithDuplicateKeysRebuildsWithoutStackOverflow()
	{
		// Duplicate keys at the leaf force the pre-scan to report true and the rebuild path to run
		// over the full depth; both must complete iteratively.
		var json = BuildEmbeddedChain(1000, "{\"id\":1,\"id\":2}");

		var resource = JsonSerializer.Deserialize<Resource>(json, DeepOptions(4096));

		Assert.NotNull(resource);
	}

	[Fact]
	public void DuplicateKeysStillNormalizeLastWinsAfterIterativeRebuild()
	{
		var options = DeepOptions(64);
		var resource = JsonSerializer.Deserialize<Resource>(
			"{\"a\":{\"x\":1,\"x\":2},\"b\":[{\"y\":1,\"y\":3}]}", options);

		Assert.NotNull(resource);
		var roundTripped = JsonSerializer.Serialize(resource, options);
		Assert.Contains("\"x\":2", roundTripped, StringComparison.Ordinal);
		Assert.DoesNotContain("\"x\":1", roundTripped, StringComparison.Ordinal);
		Assert.Contains("\"y\":3", roundTripped, StringComparison.Ordinal);
	}

	[Fact]
	public void DeepChainWithLargeLeafStateParsesInLinearTimeBudget()
	{
		// Regression guard for the O(depth × size) amplification: a large state value near the
		// leaf must not be re-serialized once per ancestor. With the amplification present, 500
		// ancestors over a ~200KB leaf re-processed ~100MB and took seconds; the node-walking
		// path stays well inside a generous wall-clock budget.
		var largeLeaf = "{\"data\":\"" + new string('x', 200_000) + "\"}";
		var json = BuildEmbeddedChain(500, largeLeaf);
		var options = DeepOptions(2048);

		var stopwatch = Stopwatch.StartNew();
		var resource = JsonSerializer.Deserialize<Resource>(json, options);
		stopwatch.Stop();

		Assert.NotNull(resource);
		Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5),
			$"Deep chain with large leaf took {stopwatch.Elapsed}; amplification suspected.");
	}

	[Fact]
	public void DeepChainRemainsFullyTraversable()
	{
		var json = BuildEmbeddedChain(50, "{\"id\":42}");
		var resource = JsonSerializer.Deserialize<Resource>(json, DeepOptions(256));

		var current = resource;
		for (var i = 0; i < 50; i++)
		{
			Assert.NotNull(current);
			var embedded = current!.Embedded;
			Assert.NotNull(embedded);
			Assert.True(embedded!.TryGetByName("child", out var child));
			current = child!.Resources[0];
		}

		Assert.NotNull(current);
	}
}
