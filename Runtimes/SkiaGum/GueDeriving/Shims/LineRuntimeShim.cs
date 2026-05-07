#if !FRB
using System;

namespace SkiaGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.LineRuntime"/>.
/// This derived class exists so existing user code with <c>using SkiaGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.GueDeriving.LineRuntime instead. The SkiaGum.GueDeriving namespace will be removed in a future release.")]
public class LineRuntime : Gum.GueDeriving.LineRuntime
{
    /// <inheritdoc/>
    public LineRuntime(bool fullInstantiation = true) : base(fullInstantiation)
    {
    }
}
#endif
