#if !FRB
using System;

namespace SkiaGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.ContainerRuntime"/>.
/// This derived class exists so existing user code with <c>using SkiaGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.GueDeriving.ContainerRuntime instead. The SkiaGum.GueDeriving namespace will be removed in a future release.")]
public class ContainerRuntime : Gum.GueDeriving.ContainerRuntime
{
    /// <inheritdoc/>
    public ContainerRuntime() : base()
    {
    }

    /// <inheritdoc/>
    public ContainerRuntime(bool fullInstantiation = true) : base(fullInstantiation)
    {
    }
}
#endif
