using System;

namespace Gum.Themes;

/// <summary>
/// Shape-specific platform shim, source-linked only into themes whose visuals use Apos.Shapes-backed
/// runtimes (rounded corners, circles, drop shadows). It is intentionally separate from
/// <see cref="ThemePlatform"/>: that shim is linked into every theme including NineSlice-only ones
/// (e.g. Editor), and referencing the Apos.Shapes <c>ShapeRenderer</c> from there would force those
/// themes to take the Apos.Shapes package dependency they don't need.
/// </summary>
internal static class ThemeShapePlatform
{
    /// <summary>
    /// Initializes the Apos.Shapes <c>ShapeRenderer</c> on the XNA-like backends (MonoGame/KNI),
    /// where the shape runtimes require it. No-op on raylib and Skia, which both render
    /// <c>RectangleRuntime</c> / <c>CircleRuntime</c> natively (no Apos.Shapes <c>ShapeRenderer</c>
    /// involved -- #3671). Calls <c>UseShapes()</c>, which does nothing when shapes are already
    /// set up, so a host that did it (or another theme) stays safe.
    /// </summary>
    public static void InitializeShapeRenderer()
    {
#if RAYLIB || SKIA
        // No-op -- see summary above.
#else
        // UseShapes does nothing when shapes are already set up, so a host that did it (or another theme) stays safe.
        GumService service = global::RenderingLibrary.IGumService.Default as GumService
            ?? throw new InvalidOperationException(
                "Gum must be initialized (GumService.Initialize) before applying a theme.");
        service.UseShapes();
#endif
    }
}
