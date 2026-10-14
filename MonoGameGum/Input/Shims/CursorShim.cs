#if MONOGAME || KNI || XNA4 || FNA
#define XNALIKE
#endif

#if XNALIKE
using System;

namespace MonoGameGum.Input;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.Input.Cursor"/>.
/// This derived class exists so existing user code with <c>using MonoGameGum.Input;</c>
/// continues to compile during the deprecation window. Cursors returned by Gum
/// (for example <c>GumService.Default.Cursor</c>) are <see cref="Gum.Input.Cursor"/> instances, so
/// declare locals as that type or with <c>var</c>.
/// </summary>
/// <remarks>
/// <c>CursorExtensions</c> has no shim, because a duplicate extension class would make every call
/// ambiguous in files that import both namespaces. Add <c>using Gum.Input;</c> for those calls.
/// </remarks>
[Obsolete("Use Gum.Input.Cursor instead. The MonoGameGum.Input namespace will be removed in a future release.")]
public class Cursor : Gum.Input.Cursor
{
    /// <inheritdoc/>
    public Cursor(Microsoft.Xna.Framework.GameWindow? gameWindow) : base(gameWindow)
    {
    }
}
#endif
