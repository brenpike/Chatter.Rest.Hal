namespace Chatter.Rest.Hal.CodeGenerators.Tests;

/// <summary>
/// A plain class with no [HalResponse] attribute.
/// The source generator should NOT add Links or Embedded properties to this class.
/// </summary>
public class PlainClass
{
	public string Name { get; set; } = default!;
	public int Value { get; set; }
}
