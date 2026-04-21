namespace Chatter.Rest.Hal.CodeGenerators.Tests;

/// <summary>
/// An abstract partial class with [HalResponse].
/// The source generator should generate Links and Embedded properties on abstract classes.
/// </summary>
[HalResponse]
public abstract partial class AbstractPersonResponse
{
	public abstract string Name { get; set; }
}
