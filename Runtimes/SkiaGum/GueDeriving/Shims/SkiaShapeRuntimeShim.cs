#if !FRB
using System;

namespace SkiaGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.SkiaShapeRuntime"/>.
/// This derived class exists so existing user code with <c>using SkiaGum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
// Obsoleted 2026-05-07. Warning now; error:true no earlier than May 2027; remove no earlier than November 2027.
[Obsolete("Use Gum.GueDeriving.SkiaShapeRuntime instead. The SkiaGum.GueDeriving namespace becomes an error no earlier than May 2027 and is removed no earlier than November 2027.")]
public abstract class SkiaShapeRuntime : Gum.GueDeriving.SkiaShapeRuntime
{
}
#endif
