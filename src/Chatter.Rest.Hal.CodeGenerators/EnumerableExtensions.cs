using System;
using System.Collections.Generic;
using System.Linq;

namespace Chatter.Rest.Hal.CodeGenerators;

public static class EnumerableExtensions
{
    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> enumerable) where T : class
    {
        if (enumerable is null) throw new ArgumentNullException(nameof(enumerable));
        return enumerable.Where(e => e != null).Select(e => e!);
    }
}