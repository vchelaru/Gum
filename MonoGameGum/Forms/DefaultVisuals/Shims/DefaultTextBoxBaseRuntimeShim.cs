#if !FRB
using System;

namespace MonoGameGum.Forms.DefaultVisuals;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.Forms.DefaultVisuals.DefaultTextBoxBaseRuntime"/>.
/// This derived class exists so existing user code with <c>using MonoGameGum.Forms.DefaultVisuals;</c>
/// continues to compile during the deprecation window. It is abstract, mirroring the real type.
/// </summary>
[Obsolete("Use Gum.Forms.DefaultVisuals.DefaultTextBoxBaseRuntime instead. The MonoGameGum.Forms.DefaultVisuals namespace will be removed in a future release.")]
public abstract class DefaultTextBoxBaseRuntime : Gum.Forms.DefaultVisuals.DefaultTextBoxBaseRuntime
{
    /// <inheritdoc/>
    protected DefaultTextBoxBaseRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
    }
}
#endif
