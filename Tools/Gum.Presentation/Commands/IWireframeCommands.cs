namespace Gum.Commands;

public interface IWireframeCommands
{
    void Refresh(bool forceLayout = true, bool forceReloadContent = false);
    bool AreRulersVisible { get; set; }
    bool AreCanvasBoundsVisible { get; set; }
    bool IsBackgroundGridVisible { get; set; }
    bool AreHighlightsVisible { get; set; }

    /// <summary>
    /// Whether the grid line overlay is drawn on the canvas. Distinct from <see cref="IsBackgroundGridVisible"/>,
    /// which controls the transparency checkerboard.
    /// </summary>
    bool IsGridOverlayVisible { get; set; }

    /// <summary>
    /// The size, in pixels, of each grid cell drawn by the grid overlay.
    /// </summary>
    int GridSize { get; set; }
}
