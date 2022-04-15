namespace Chatter.Rest.Hal.CodeGenerators.Tests;

[HalResponse]
public partial class Person
{
	public int Age { get; set; } = default!;
	public string[] Friends { get; set; } = default!;
	public string Name { get; set; } = default!;
}