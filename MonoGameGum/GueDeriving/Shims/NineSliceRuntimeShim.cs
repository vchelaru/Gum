#if !FRB
using System;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.NineSliceRuntime"/>.
/// This derived class exists so existing user code with <c>using MonoGameGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
// Obsoleted 2026-05-07. Warning now; error:true no earlier than May 2027; remove no earlier than November 2027.
[Obsolete("Use Gum.GueDeriving.NineSliceRuntime instead. The MonoGameGum.GueDeriving namespace becomes an error no earlier than May 2027 and is removed no earlier than November 2027.")]
public class NineSliceRuntime : Gum.GueDeriving.NineSliceRuntime
{
    /// <inheritdoc/>
    public NineSliceRuntime(bool fullInstantiation = true) : base(fullInstantiation)
    {
    }
}
#endif
