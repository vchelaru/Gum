#if !FRB
using System;

namespace SkiaGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.SkiaShapeRuntime"/>.
/// This derived class exists so existing user code with <c>using SkiaGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.GueDeriving.SkiaShapeRuntime instead. The SkiaGum.GueDeriving namespace will be removed in a future release.")]
public abstract class SkiaShapeRuntime : Gum.GueDeriving.SkiaShapeRuntime
{
}
#endif
