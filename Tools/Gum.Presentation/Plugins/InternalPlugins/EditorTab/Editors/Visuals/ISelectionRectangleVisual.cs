namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// The marquee/rubber-band rectangle <see cref="RectangleSelector"/> draws while dragging,
/// exposed without referencing the underlying <c>LineRectangle</c> drawing type (XNALIKE-only,
/// unreachable from headless <c>Gum.Presentation</c>). The tool-side implementation owns styling
/// (color, dotted line, etc.) entirely — this seam only carries the geometry/visibility
/// <see cref="RectangleSelector"/> computes.
/// </summary>
public interface ISelectionRectangleVisual
{
    bool Visible { get; set; }

    float X { get; set; }

    float Y { get; set; }

    float Width { get; set; }

    float Height { get; set; }

    /// <summary>
    /// Removes the visual from the layer it was drawn on. Called when the owning
    /// <see cref="RectangleSelector"/> is destroyed.
    /// </summary>
    void Destroy();
}
