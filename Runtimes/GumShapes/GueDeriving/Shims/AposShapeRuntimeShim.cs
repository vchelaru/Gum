#if !FRB
using System;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.GueDeriving.AposShapeRuntime"/>.
/// This derived class exists so existing user code with <c>using Gum.GueDeriving;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.GueDeriving.AposShapeRuntime instead. The MonoGameGum.GueDeriving namespace will be removed in a future release.")]
public abstract class AposShapeRuntime : Gum.GueDeriving.AposShapeRuntime
{
}
#endif
