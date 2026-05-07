#if !FRB
using System;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.RoundedRectangleRuntime"/>.
/// This derived class exists so existing user code with <c>using MonoGameGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
/// <remarks>
/// The shim file lives under <c>Runtimes/GumShapes/GueDeriving/Shims/</c> rather than alongside
/// the other MonoGameGum.GueDeriving shims because <c>RoundedRectangleRuntime</c>'s real source
/// is file-linked from SkiaGum into the Apos.Shapes-backed projects (MonoGameGumShapes,
/// KniGumShapes). Placing the shim here scopes it to those projects only, matching the type's
/// actual compilation surface.
/// </remarks>
[Obsolete("Use Gum.GueDeriving.RoundedRectangleRuntime instead. The MonoGameGum.GueDeriving namespace will be removed in a future release.")]
public class RoundedRectangleRuntime : Gum.GueDeriving.RoundedRectangleRuntime
{
    /// <inheritdoc/>
    public RoundedRectangleRuntime(bool fullInstantiation = true) : base(fullInstantiation)
    {
    }
}
#endif
