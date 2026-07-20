namespace Gum.Wireframe;

/// <summary>
/// Precise (non-bounding-box) hit testing for hover/click selection. Most
/// <see cref="GraphicalUiElement"/>s use the generic rotated-bounding-box
/// <c>IRenderableIpso.HasCursorOver</c> extension method, but a polygon needs an actual
/// point-in-polygon test (<c>LinePolygon.IsPointInside</c>) — a concrete, XNALIKE-only type
/// unreachable from headless <c>Gum.Presentation</c>, so that distinction is pushed behind this
/// seam instead of <c>SelectionManager</c> type-checking for <c>LinePolygon</c> directly.
/// </summary>
public interface IPreciseHitTester
{
    bool HasCursorOver(GraphicalUiElement element, float x, float y);
}
