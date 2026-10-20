#if !FRB
using System;

namespace SkiaGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.ColoredRectangleRuntime"/>.
/// This derived class exists so existing user code with <c>using SkiaGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
// Obsoleted 2026-05-07. Warning now; error:true no earlier than May 2027; remove no earlier than November 2027.
[Obsolete("Use Gum.GueDeriving.ColoredRectangleRuntime instead. The SkiaGum.GueDeriving namespace becomes an error no earlier than May 2027 and is removed no earlier than November 2027.")]
public class ColoredRectangleRuntime : Gum.GueDeriving.ColoredRectangleRuntime
{
    /// <inheritdoc/>
    public ColoredRectangleRuntime(bool fullInstantiation = true) : base(fullInstantiation)
    {
    }
}
#endif
