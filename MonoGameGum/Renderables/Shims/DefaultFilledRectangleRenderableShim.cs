#if XNALIKE && !FRB
using System;

namespace MonoGameGum.Renderables;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.Renderables.DefaultFilledRectangleRenderable"/>.
/// This derived class exists so existing user code with <c>using MonoGameGum.Renderables;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.Renderables.DefaultFilledRectangleRenderable instead. The MonoGameGum.Renderables namespace will be removed in a future release.")]
public class DefaultFilledRectangleRenderable : Gum.Renderables.DefaultFilledRectangleRenderable
{
}
#endif
