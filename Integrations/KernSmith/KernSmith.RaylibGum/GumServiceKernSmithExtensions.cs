using System;
using RaylibGum.Renderables;
using KernSmith;
using KernSmith.Rasterizer;
using KernSmith.Gum;

namespace Gum;

/// <summary>
/// Enables KernSmith in-memory font generation for a raylib Gum game.
/// </summary>
public static class GumServiceKernSmithExtensions
{
    /// <summary>
    /// Makes Gum rasterize fonts in memory with KernSmith instead of loading pre-generated
    /// <c>.fnt</c> and <c>.png</c> files. Call once, after <c>Initialize</c>. Replaces
    /// <c>CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator()</c>.
    /// </summary>
    /// <param name="gumService">The service to enable KernSmith for.</param>
    /// <param name="backend">
    /// Optional rasterizer backend override. When null, the default (FreeType) is used.
    /// </param>
    /// <returns><paramref name="gumService"/>, so calls chain.</returns>
    public static T UseKernSmith<T>(this T gumService, RasterizerBackend? backend = null) where T : GumService
    {
        if (gumService == null)
        {
            throw new ArgumentNullException(nameof(gumService));
        }

        CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator(backend);
        return gumService;
    }
}
