using System;

namespace Chatter.Rest.Hal;

/// <summary>
/// Marks a partial class as a HAL resource so <c>Chatter.Rest.Hal.CodeGenerators</c> generates a
/// companion partial declaring the reserved HAL members: a <c>Links</c> property serialized as
/// <c>_links</c> and an <c>Embedded</c> property serialized as <c>_embedded</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>Chatter.Rest.Hal.Core</c> is not published as a standalone NuGet package. Since
/// <c>Chatter.Rest.Hal.CodeGenerators</c> 0.4.0 the generator emits its own <c>internal sealed</c>
/// copy of this attribute into every compilation it runs in, so installing the generator package
/// alone is enough for <c>[HalResponse]</c> to resolve. A project that references both this
/// assembly and the generator sees CS0436 (a source-declared type conflicting with an imported
/// type) and should drop the <c>Chatter.Rest.Hal.Core</c> reference.
/// </para>
/// <para>
/// As declared, the attribute is valid only on classes (<see cref="AttributeTargets.Class"/>);
/// <c>AllowMultiple</c> and <c>Inherited</c> are left at their
/// <see cref="AttributeUsageAttribute"/> defaults (<c>false</c> and <c>true</c> respectively).
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class HalResponseAttribute : Attribute
{
}
