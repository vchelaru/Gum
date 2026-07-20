using RenderingLibrary;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// The translucent overlay (Sprite/NineSlice/LineRectangle/LinePolygon-shaped) <c>SelectionManager</c>
/// draws over the currently highlighted object, exposed without referencing the underlying
/// XNALIKE-only renderable types the tool-side <c>HighlightManager</c> uses, unreachable from
/// headless <c>Gum.Presentation</c>.
/// </summary>
public interface IHighlightOverlayVisual
{
    /// <summary>The object the overlay is currently shaped around.</summary>
    IPositionedSizedObject? HighlightedIpso { set; }

    /// <summary>Whether the overlay should be drawn at all.</summary>
    bool AreHighlightsVisible { get; set; }

    /// <summary>Hides the overlay shaped for <paramref name="highlightedIpso"/>.</summary>
    void UnhighlightIpso(GraphicalUiElement highlightedIpso);

    /// <summary>Recomputes and shows the overlay shaped around <see cref="HighlightedIpso"/>.</summary>
    void UpdateHighlightObjects();
}
