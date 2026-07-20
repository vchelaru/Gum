namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// The dashed/dotted outline rectangle <c>SelectionManager</c> draws around the currently
/// highlighted (hovered) object, exposed without referencing the underlying XNALIKE-only drawing
/// types (<c>LineRectangle</c>, <c>NineSlice</c>) the tool-side <c>GraphicalOutline</c> uses,
/// unreachable from headless <c>Gum.Presentation</c>.
/// </summary>
public interface IHighlightOutlineVisual
{
    /// <summary>
    /// The object currently being outlined, or null to clear the outline. Setting this recomputes
    /// the outline geometry.
    /// </summary>
    GraphicalUiElement? HighlightedIpso { set; }

    /// <summary>
    /// Recomputes the outline (and any NineSlice split lines) around the current
    /// <see cref="HighlightedIpso"/>. Called every frame so the outline tracks a nudge/resize
    /// immediately.
    /// </summary>
    void UpdateHighlightElements();
}
