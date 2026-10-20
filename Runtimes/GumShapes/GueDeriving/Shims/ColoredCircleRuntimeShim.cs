#if !FRB
using System;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.ColoredCircleRuntime"/>.
/// This derived class exists so existing user code with <c>using Gum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
// Obsoleted 2026-05-07. Warning now; error:true no earlier than May 2027; remove no earlier than November 2027.
[Obsolete("Use Gum.GueDeriving.ColoredCircleRuntime instead. The MonoGameGum.GueDeriving namespace becomes an error no earlier than May 2027 and is removed no earlier than November 2027.")]
public class ColoredCircleRuntime : Gum.GueDeriving.ColoredCircleRuntime
{
    /// <inheritdoc/>
    public ColoredCircleRuntime(bool fullInstantiation = true) : base(fullInstantiation)
    {
    }
}
#endif
