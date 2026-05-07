#if !FRB
using System;

namespace SkiaGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.SpriteRuntime"/>.
/// This derived class exists so existing user code with <c>using SkiaGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.GueDeriving.SpriteRuntime instead. The SkiaGum.GueDeriving namespace will be removed in a future release.")]
public class SpriteRuntime : Gum.GueDeriving.SpriteRuntime
{
    /// <inheritdoc/>
    public SpriteRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
    }
}
#endif
