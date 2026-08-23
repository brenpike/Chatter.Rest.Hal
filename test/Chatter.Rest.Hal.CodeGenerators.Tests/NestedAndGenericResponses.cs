namespace Chatter.Rest.Hal.CodeGenerators.Tests;

/// <summary>
/// A generic [HalResponse] target. The generator must repeat the type parameter list so this very
/// type - not an arity-0 companion - receives the HAL members.
/// </summary>
[HalResponse]
public partial class GenericPersonResponse<TPayload>
{
	public TPayload? Payload { get; set; }
}

/// <summary>
/// Two nested [HalResponse] targets that share a simple name. Both must be generated: they are
/// distinct types, separated only by their containing type.
/// </summary>
public partial class ResponseContainerA
{
	[HalResponse]
	public partial class InnerResponse
	{
		public string Name { get; set; } = default!;
	}
}

public partial class ResponseContainerB
{
	[HalResponse]
	public partial class InnerResponse
	{
		public int Value { get; set; }
	}
}
