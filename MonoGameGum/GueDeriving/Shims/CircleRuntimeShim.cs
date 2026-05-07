#if !FRB
using System;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.CircleRuntime"/>.
/// This derived class exists so existing user code with <c>using MonoGameGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.GueDeriving.CircleRuntime instead. The MonoGameGum.GueDeriving namespace will be removed in a future release.")]
public class CircleRuntime : Gum.GueDeriving.CircleRuntime
{
    /// <inheritdoc/>
    public CircleRuntime(bool fullInstantiation = true) : base(fullInstantiation)
    {
    }
}
#endif
