using System.Text.Json;
using System.Text.Json.Nodes;

namespace Chatter.Rest.Hal.Converters;

internal static class ConverterHelpers
{
	internal static bool IsJsonNull(JsonNode? node)
	{
		if (node is not JsonValue jv) return false;
#if NET8_0_OR_GREATER
		return jv.GetValueKind() == JsonValueKind.Null;
#else
		return jv.ToJsonString() == "null";
#endif
	}
}
