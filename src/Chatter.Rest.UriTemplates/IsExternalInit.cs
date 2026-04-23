#if NETSTANDARD2_0
// Polyfill to enable record positional parameters (init-only setters) on netstandard2.0.
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
