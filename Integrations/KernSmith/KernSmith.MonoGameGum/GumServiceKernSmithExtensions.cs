using System;
using Gum.Wireframe;
using KernSmith;
using KernSmith.Gum;

namespace Gum;

/// <summary>
/// Enables KernSmith in-memory font generation for a Gum game.
/// </summary>
public static class GumServiceKernSmithExtensions
{
    /// <summary>
    /// Makes Gum rasterize fonts in memory with KernSmith instead of loading pre-generated
    /// <c>.fnt</c> and <c>.png</c> files. Call once, after <c>Initialize</c>, because the font textures are
    /// created on the service's graphics device. Replaces
    /// <c>CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(GraphicsDevice)</c>.
    /// </summary>
    /// <param name="gumService">The initialized service whose graphics device creates the font textures.</param>
    /// <param name="backend">
    /// Optional rasterizer backend override. When null, the default (FreeType) is used. Pass
    /// <c>RasterizerBackend.StbTrueType</c> where native libraries are unavailable, for example Blazor WASM.
    /// </param>
    /// <returns><paramref name="gumService"/>, so calls chain.</returns>
    /// <exception cref="InvalidOperationException">The service has not been initialized, so it has no graphics device.</exception>
    public static T UseKernSmith<T>(this T gumService, RasterizerBackend? backend = null) where T : GumService
    {
        if (gumService == null)
        {
            throw new ArgumentNullException(nameof(gumService));
        }

        if (!gumService.IsInitialized)
        {
            throw new InvalidOperationException(
                "UseKernSmith was called before the GumService was initialized, so it has no graphics device. Call Initialize first.");
        }

        CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(gumService.Renderer.GraphicsDevice, backend);
        return gumService;
    }
}
