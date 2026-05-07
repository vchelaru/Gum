#if !FRB
using System;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.ArcRuntime"/>.
/// This derived class exists so existing user code with <c>using Gum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.GueDeriving.ArcRuntime instead. The MonoGameGum.GueDeriving namespace will be removed in a future release.")]
public class ArcRuntime : Gum.GueDeriving.ArcRuntime
{
    /// <inheritdoc/>
    public ArcRuntime(bool fullInstantiation = true) : base(fullInstantiation)
    {
    }
}
#endif
