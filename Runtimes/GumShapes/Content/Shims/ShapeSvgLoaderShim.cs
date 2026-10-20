#if !FRB
using System;

namespace MonoGameAndGum.Content;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.Content.ShapeSvgLoader"/>.
/// A static class cannot be subclassed, so this forwards to the real implementation (and its
/// shared cache) instead.
/// </summary>
// Obsoleted 2026-10-20. Warning now; error:true no earlier than May 2027; remove no earlier than November 2027.
[Obsolete("Use Gum.Content.ShapeSvgLoader instead. The MonoGameAndGum.Content namespace becomes an error no earlier than May 2027 and is removed no earlier than November 2027.")]
public static class ShapeSvgLoader
{
    /// <inheritdoc cref="Gum.Content.ShapeSvgLoader.Load(string)"/>
    public static Apos.Shapes.ShapeSvg? Load(string fileName) => Gum.Content.ShapeSvgLoader.Load(fileName);
}
#endif
