namespace Chatter.Rest.Hal.CodeGenerators;

public static class EnumerableExtensions
{
	public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> enumerable) where T : class =>
		enumerable.Where(static e => e != null).Select(static e => e!);
}