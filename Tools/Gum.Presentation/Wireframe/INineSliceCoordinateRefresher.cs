using RenderingLibrary.Graphics;

namespace Gum.Wireframe;

/// <summary>
/// Refreshes a NineSlice's texture coordinates and sprite sizes before it is highlighted, without
/// <c>SelectionManager</c> referencing the concrete <c>NineSlice</c> type (XNALIKE-only, unreachable
/// from headless <c>Gum.Presentation</c>).
/// </summary>
public interface INineSliceCoordinateRefresher
{
    /// <summary>
    /// If <paramref name="renderableComponent"/> is a NineSlice, refreshes its texture coordinates
    /// and sprite sizes. No-op for any other type (or null).
    /// </summary>
    void RefreshIfNineSlice(IRenderable? renderableComponent);
}
