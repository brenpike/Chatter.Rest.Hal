using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

/// <summary>
/// Options-registered converters take precedence over attribute-wired ones; the internal
/// node-walking fast path must step aside when the caller registered a custom converter for a
/// nested HAL part.
/// </summary>
public class CustomConverterPrecedenceTests
{
	private sealed class CountingLinkCollectionConverter : JsonConverter<LinkCollection>
	{
		internal int ReadCount;

		public override LinkCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			ReadCount++;
			reader.Skip();
			var links = new LinkCollection();
			links.Add(new Link("custom"));
			return links;
		}

		public override void Write(Utf8JsonWriter writer, LinkCollection value, JsonSerializerOptions options)
			=> throw new NotSupportedException();
	}

	[Fact]
	public void CustomLinkCollectionConverterRunsForNestedLinks()
	{
		var counting = new CountingLinkCollectionConverter();
		var options = new JsonSerializerOptions();
		options.Converters.Add(counting);

		var resource = JsonSerializer.Deserialize<Resource>(
			"{\"_links\":{\"self\":{\"href\":\"/a\"}},\"_embedded\":{\"child\":{\"_links\":{\"self\":{\"href\":\"/b\"}}}}}",
			options);

		Assert.NotNull(resource);
		// Root _links plus the embedded child's _links must both dispatch through the custom converter.
		Assert.Equal(2, counting.ReadCount);
		Assert.True(resource!.Links!.TryGetByRel("custom", out _));
	}
}
