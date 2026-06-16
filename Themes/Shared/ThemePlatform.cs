#if RAYLIB
using RaylibGum.Renderables;
#else
using Gum.Wireframe;
using RenderingLibrary;
#endif
using KernSmith.Gum;
using System;

namespace Gum.Themes;

/// <summary>
/// Isolates the per-backend differences a Gum theme needs at <c>Apply</c> time so each theme's
/// body stays platform-agnostic and source-shared across its MonoGame/KNI and raylib project
/// variants. This file is the single home for the <c>#if RAYLIB</c> branches; the theme classes
/// that link it stay free of platform conditionals.
/// </summary>
internal static class ThemePlatform
{
    /// <summary>
    /// Assigns the backend-appropriate KernSmith in-memory font creator so themed controls can
    /// rasterize their fonts (system or embedded) at runtime without shipping <c>.fnt</c> files.
    /// On MonoGame/KNI the graphics device is resolved from the active Gum renderer, so this must
    /// be called after Gum has been initialized (<c>GumService.Initialize</c>).
    /// </summary>
    public static void WireInMemoryFontCreator()
    {
#if RAYLIB
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();
#else
        var graphicsDevice = SystemManagers.Default?.Renderer?.GraphicsDevice
            ?? throw new InvalidOperationException(
                "Gum must be initialized (GumService.Initialize) before applying a theme.");
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(graphicsDevice);
#endif
    }

    /// <summary>
    /// Registers an embedded TTF (raw bytes) under a family/style with the backend's KernSmith
    /// creator, so a theme's bundled fonts resolve without the consumer copying font files. Both
    /// backends expose the same static <c>RegisterFont</c> surface; this only hides the type name.
    /// </summary>
    public static void RegisterFont(string familyName, byte[] fontData, string? style = null)
    {
#if RAYLIB
        KernSmithRaylibFontCreator.RegisterFont(familyName, fontData, style);
#else
        KernSmithFontCreator.RegisterFont(familyName, fontData, style);
#endif
    }
}
