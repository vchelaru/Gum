#if !FRB
using System;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.SpriteRuntime"/>.
/// This derived class exists so existing user code with <c>using MonoGameGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.GueDeriving.SpriteRuntime instead. The MonoGameGum.GueDeriving namespace will be removed in a future release.")]
public class SpriteRuntime : Gum.GueDeriving.SpriteRuntime
{
    /// <inheritdoc/>
    public SpriteRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
    }
}
#endif
