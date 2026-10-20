#if XNALIKE && !FRB
using System;

namespace MonoGameGum.Renderables;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.Renderables.DefaultStrokedRectangleRenderable"/>.
/// This derived class exists so existing user code with <c>using MonoGameGum.Renderables;</c>
/// continues to compile during the deprecation window.
/// </summary>
// Obsoleted 2026-06-13. Warning now; error:true no earlier than May 2027; remove no earlier than November 2027.
[Obsolete("Use Gum.Renderables.DefaultStrokedRectangleRenderable instead. The MonoGameGum.Renderables namespace becomes an error no earlier than May 2027 and is removed no earlier than November 2027.")]
public class DefaultStrokedRectangleRenderable : Gum.Renderables.DefaultStrokedRectangleRenderable
{
}
#endif
