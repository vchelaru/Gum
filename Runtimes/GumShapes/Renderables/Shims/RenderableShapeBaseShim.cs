#if !FRB
using System;

namespace MonoGameAndGum.Renderables;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.Renderables.RenderableShapeBase"/>.
/// This derived class exists so existing user code deriving from the old name continues to
/// compile during the deprecation window. Shapes created by Gum are
/// <see cref="Gum.Renderables.RenderableShapeBase"/> instances, so declare locals as that type.
/// </summary>
// Obsoleted 2026-10-20. Warning now; error:true no earlier than May 2027; remove no earlier than November 2027.
[Obsolete("Use Gum.Renderables.RenderableShapeBase instead. The MonoGameAndGum.Renderables namespace becomes an error no earlier than May 2027 and is removed no earlier than November 2027.")]
public abstract class RenderableShapeBase : Gum.Renderables.RenderableShapeBase
{
}
#endif
