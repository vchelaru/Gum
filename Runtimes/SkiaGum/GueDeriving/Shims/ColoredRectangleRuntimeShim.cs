#if !FRB
using System;

namespace SkiaGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.ColoredRectangleRuntime"/>.
/// This derived class exists so existing user code with <c>using SkiaGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.GueDeriving.ColoredRectangleRuntime instead. The SkiaGum.GueDeriving namespace will be removed in a future release.")]
public class ColoredRectangleRuntime : Gum.GueDeriving.ColoredRectangleRuntime
{
    /// <inheritdoc/>
    public ColoredRectangleRuntime(bool fullInstantiation = true) : base(fullInstantiation)
    {
    }
}
#endif
