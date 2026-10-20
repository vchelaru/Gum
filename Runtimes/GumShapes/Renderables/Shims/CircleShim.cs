#if !FRB
using System;

namespace MonoGameAndGum.Renderables;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.Renderables.Circle"/>.
/// This derived class exists so existing user code that constructs the old name continues to
/// compile during the deprecation window. Circles created by Gum are
/// <see cref="Gum.Renderables.Circle"/> instances, so declare locals as that type.
/// </summary>
// Obsoleted 2026-10-20. Warning now; error:true no earlier than May 2027; remove no earlier than November 2027.
[Obsolete("Use Gum.Renderables.Circle instead. The MonoGameAndGum.Renderables namespace becomes an error no earlier than May 2027 and is removed no earlier than November 2027.")]
public class Circle : Gum.Renderables.Circle
{
}
#endif
