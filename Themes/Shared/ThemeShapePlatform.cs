#if !RAYLIB
using MonoGameAndGum.Renderables;
#endif

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
    /// where the shape runtimes require it. No-op on raylib, which renders <c>RectangleRuntime</c> /
    /// <c>CircleRuntime</c> natively (no <c>ShapeRenderer</c>). Guards against double-initialization
    /// so a host that already set shapes up (or another theme) stays safe.
    /// </summary>
    public static void InitializeShapeRenderer()
    {
#if !RAYLIB
        if (!ShapeRenderer.Self.IsInitialized)
        {
            ShapeRenderer.Self.Initialize();
        }
#endif
    }
}
