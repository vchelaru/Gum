#if !FRB
using System;
using RenderingLibrary;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.PolygonRuntime"/>.
/// This derived class exists so existing user code with <c>using MonoGameGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.GueDeriving.PolygonRuntime instead. The MonoGameGum.GueDeriving namespace will be removed in a future release.")]
public class PolygonRuntime : Gum.GueDeriving.PolygonRuntime
{
    /// <inheritdoc/>
    public PolygonRuntime(bool fullInstantiation = true, SystemManagers? systemManagers = null)
        : base(fullInstantiation, systemManagers)
    {
    }
}
#endif
