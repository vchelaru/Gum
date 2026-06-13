#if XNALIKE && !FRB
using System;

namespace MonoGameGum.Renderables;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.Renderables.DefaultStrokedRectangleRenderable"/>.
/// This derived class exists so existing user code with <c>using MonoGameGum.Renderables;</c>
/// continues to compile during the deprecation window.
/// </summary>
[Obsolete("Use Gum.Renderables.DefaultStrokedRectangleRenderable instead. The MonoGameGum.Renderables namespace will be removed in a future release.")]
public class DefaultStrokedRectangleRenderable : Gum.Renderables.DefaultStrokedRectangleRenderable
{
}
#endif
