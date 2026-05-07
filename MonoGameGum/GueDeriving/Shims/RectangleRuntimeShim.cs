#if !FRB
using System;
using RenderingLibrary;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.RectangleRuntime"/>.
/// This derived class exists so existing user code with <c>using MonoGameGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.GueDeriving.RectangleRuntime instead. The MonoGameGum.GueDeriving namespace will be removed in a future release.")]
public class RectangleRuntime : Gum.GueDeriving.RectangleRuntime
{
    /// <inheritdoc/>
    public RectangleRuntime(bool fullInstantiation = true, SystemManagers systemManagers = null)
        : base(fullInstantiation, systemManagers)
    {
    }
}
#endif
