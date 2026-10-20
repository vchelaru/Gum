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
// Obsoleted 2026-05-07. Warning now; error:true no earlier than May 2027; remove no earlier than November 2027.
[Obsolete("Use Gum.GueDeriving.RoundedRectangleRuntime instead. The MonoGameGum.GueDeriving namespace becomes an error no earlier than May 2027 and is removed no earlier than November 2027.")]
public class RoundedRectangleRuntime : Gum.GueDeriving.RoundedRectangleRuntime
{
    /// <inheritdoc/>
    public RoundedRectangleRuntime(bool fullInstantiation = true) : base(fullInstantiation)
    {
    }
}
#endif
