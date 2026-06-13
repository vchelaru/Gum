#if !FRB
using System;

namespace MonoGameGum.Forms.DefaultVisuals;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.Forms.DefaultVisuals.DefaultTextBoxRuntime"/>.
/// This derived class exists so existing user code with <c>using MonoGameGum.Forms.DefaultVisuals;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.Forms.DefaultVisuals.DefaultTextBoxRuntime instead. The MonoGameGum.Forms.DefaultVisuals namespace will be removed in a future release.")]
public class DefaultTextBoxRuntime : Gum.Forms.DefaultVisuals.DefaultTextBoxRuntime
{
    /// <inheritdoc/>
    public DefaultTextBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
    }
}
#endif
