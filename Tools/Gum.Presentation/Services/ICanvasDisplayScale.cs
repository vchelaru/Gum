namespace Gum.Services;

/// <summary>
/// The OS display scale of the monitor a canvas is on (1 at 100%, 2 on a Retina display). Project
/// content renders at physical pixels and ignores it; the overlay of the editor and Texture
/// Coordinates canvases (handles, outlines, guides, rulers) multiplies its sizes by it so it matches
/// the tool's UI.
/// Only sizes are scaled, never positions or measured values.
/// </summary>
public interface ICanvasDisplayScale
{
    /// <summary>The display scale. The canvas updates it each frame from its host.</summary>
    float DisplayScale { get; set; }

    /// <summary>
    /// Converts an overlay size in device-independent pixels (the size it has at 100% display
    /// scale and 100% zoom) to world units at the given camera zoom.
    /// </summary>
    float ToWorld(float overlaySize, float zoom);
}

/// <inheritdoc/>
public class CanvasDisplayScale : ICanvasDisplayScale
{
    /// <inheritdoc/>
    public float DisplayScale { get; set; }

    /// <summary>Creates a display scale of 1 (100%).</summary>
    public CanvasDisplayScale()
    {
        DisplayScale = 1;
    }

    /// <inheritdoc/>
    public float ToWorld(float overlaySize, float zoom) => overlaySize * DisplayScale / zoom;
}
