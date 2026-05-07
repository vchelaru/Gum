#if !FRB
using System;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.ColoredCircleRuntime"/>.
/// This derived class exists so existing user code with <c>using Gum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.GueDeriving.ColoredCircleRuntime instead. The MonoGameGum.GueDeriving namespace will be removed in a future release.")]
public class ColoredCircleRuntime : Gum.GueDeriving.ColoredCircleRuntime
{
    /// <inheritdoc/>
    public ColoredCircleRuntime(bool fullInstantiation = true) : base(fullInstantiation)
    {
    }
}
#endif
