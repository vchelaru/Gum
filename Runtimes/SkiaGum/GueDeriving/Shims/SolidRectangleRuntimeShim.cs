#if !FRB
using System;

namespace SkiaGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.SolidRectangleRuntime"/>.
/// This derived class exists so existing user code with <c>using SkiaGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.GueDeriving.SolidRectangleRuntime instead. The SkiaGum.GueDeriving namespace will be removed in a future release.")]
public class SolidRectangleRuntime : Gum.GueDeriving.SolidRectangleRuntime
{
    /// <inheritdoc/>
    public SolidRectangleRuntime(bool fullInstantiation = true) : base(fullInstantiation)
    {
    }
}
#endif
