#if MONOGAME || KNI || FNA
#define XNALIKE
#endif

#if XNALIKE && !FRB
using System;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace MonoGameGum.Renderables;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.Renderables.RenderableCreator"/>.
/// A static class cannot be subclassed, so this forwards to the real implementation instead.
/// </summary>
// Obsoleted 2026-10-20. Warning now; error:true no earlier than May 2027; remove no earlier than November 2027.
[Obsolete("Use Gum.Renderables.RenderableCreator instead. The MonoGameGum.Renderables namespace becomes an error no earlier than May 2027 and is removed no earlier than November 2027.")]
public static class RenderableCreator
{
    /// <inheritdoc cref="Gum.Renderables.RenderableCreator.HandleCreateGraphicalComponent"/>
    public static IRenderable? HandleCreateGraphicalComponent(string type, ISystemManagers systemManagers) =>
        Gum.Renderables.RenderableCreator.HandleCreateGraphicalComponent(type, systemManagers);
}
#endif
