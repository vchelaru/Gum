#if RAYLIB
using RaylibGum.Renderables;
using KernSmith.Gum;
#elif SKIA
using SkiaGum.Content.Fonts;
#else
using Gum.Wireframe;
using RenderingLibrary;
using KernSmith;
using KernSmith.Gum;
#endif
using System;

namespace Gum.Themes;

/// <summary>
/// Isolates the per-backend differences a Gum theme needs at <c>Apply</c> time so each theme's
/// body stays platform-agnostic and source-shared across its MonoGame/KNI, raylib, and Skia/SilkNet
/// project variants. This file is the single home for the <c>#if RAYLIB</c> / <c>#if SKIA</c>
/// branches; the theme classes that link it stay free of platform conditionals.
/// </summary>
internal static class ThemePlatform
{
#if !RAYLIB && !SKIA
    // Test seams for SelectBackend() below -- swappable so its logic is unit-testable without a real
    // browser host or a compile-time reference to KernSmith.Rasterizers.StbTrueType. Mirrors the
    // established IsBrowserPlatform seam pattern in GumFontGenerator (KernSmith.GumCommon).
    internal static Func<bool> IsBrowserPlatform = OperatingSystem.IsBrowser;
    internal static Func<Type?> ResolveStbTrueTypeRasterizerType = () =>
        Type.GetType("KernSmith.Rasterizers.StbTrueType.StbTrueTypeRasterizer, KernSmith.Rasterizers.StbTrueType");
    internal static Action<Type> ForceStaticConstructor = type =>
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);

    /// <summary>
    /// Picks the KernSmith rasterizer backend a theme's font creator should use. FreeType (the
    /// default, <c>null</c> here) is native code and cannot run on browser-wasm (#4564) -- there,
    /// StbTrueType (the sole pure-C# backend) is required instead. Resolved by name via reflection
    /// rather than a compile-time reference, so desktop/Raylib/SilkNet theme consumers never have to
    /// carry the <c>KernSmith.Rasterizers.StbTrueType</c> package just because it exists (see the
    /// <c>gum-kernsmith-integrations</c> skill's "per-backend package split"). Also force-runs the
    /// backend's static constructor: KernSmith's reflection-based backend auto-discovery is trimmed
    /// away under wasm/AOT publish and would otherwise silently fail to register it.
    /// </summary>
    /// <returns>
    /// <see cref="RasterizerBackend.StbTrueType"/> on browser-wasm when the package is referenced;
    /// otherwise <c>null</c>, which keeps the existing FreeType default -- and, on browser-wasm
    /// without the package, the existing actionable <c>PlatformNotSupportedException</c> from
    /// <c>GumFontGenerator.Generate</c> that names the missing package.
    /// </returns>
    internal static RasterizerBackend? SelectBackend()
    {
        if (!IsBrowserPlatform())
        {
            return null;
        }

        Type? stbType = ResolveStbTrueTypeRasterizerType();
        if (stbType == null)
        {
            return null;
        }

        ForceStaticConstructor(stbType);
        return RasterizerBackend.StbTrueType;
    }
#endif

    /// <summary>
    /// Assigns the backend-appropriate KernSmith in-memory font creator so themed controls can
    /// rasterize their fonts (system or embedded) at runtime without shipping <c>.fnt</c> files.
    /// On MonoGame/KNI the graphics device is resolved from the active Gum renderer, so this must
    /// be called after Gum has been initialized (<c>GumService.Initialize</c>). No-op on Skia/SilkNet:
    /// <c>SystemManagers.Initialize()</c> already routes RichTextKit's font resolution through
    /// <see cref="GumFontMapper"/> (<c>Topten.RichTextKit.FontMapper.Default</c>), so there is no
    /// separate in-memory font creator to wire the way the bitmap-font-atlas backends need (#3671).
    /// </summary>
    public static void WireInMemoryFontCreator()
    {
#if RAYLIB
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();
#elif SKIA
        // No-op -- see summary above.
#else
        var graphicsDevice = SystemManagers.Default?.Renderer?.GraphicsDevice
            ?? throw new InvalidOperationException(
                "Gum must be initialized (GumService.Initialize) before applying a theme.");
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(graphicsDevice, SelectBackend());
#endif
    }

    /// <summary>
    /// Registers an embedded TTF (raw bytes) under a family/style so a theme's bundled fonts
    /// resolve without the consumer copying font files. On XNA-like/raylib this forwards to the
    /// backend's KernSmith creator (both expose the same static <c>RegisterFont</c> surface; this
    /// only hides the type name). On Skia/SilkNet it forwards to <see cref="GumFontMapper.RegisterFont"/>,
    /// which resolves the same family+style vocabulary directly against SkiaSharp's native TTF
    /// loading -- no atlas baking needed (#3671).
    /// </summary>
    public static void RegisterFont(string familyName, byte[] fontData, string? style = null)
    {
#if RAYLIB
        KernSmithRaylibFontCreator.RegisterFont(familyName, fontData, style);
#elif SKIA
        GumFontMapper.RegisterFont(familyName, fontData, style);
#else
        KernSmithFontCreator.RegisterFont(familyName, fontData, style);
#endif
    }
}
