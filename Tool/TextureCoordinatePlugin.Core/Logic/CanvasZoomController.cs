using FlatRedBall.SpecializedXnaControls;
using TextureCoordinateSelectionPlugin.RegionSelection;
using Gum.Plugins.InternalPlugins.EditorTab.Services;

namespace TextureCoordinateSelectionPlugin.Logic;

/// <summary>
/// The texture-coordinate canvas's zoom steps as the shared <see cref="CameraController"/> sees
/// them: each step moves to the next configured zoom level around the center of the view.
/// </summary>
public class CanvasZoomController : IZoomController
{
    private readonly ImageRegionSelectionCore _canvas;

    /// <summary>Creates the adapter over <paramref name="canvas"/>.</summary>
    public CanvasZoomController(ImageRegionSelectionCore canvas)
    {
        _canvas = canvas;
    }

    /// <inheritdoc/>
    public void ZoomIn() => _canvas.HandleZoom(ZoomDirection.ZoomIn);

    /// <inheritdoc/>
    public void ZoomOut() => _canvas.HandleZoom(ZoomDirection.ZoomOut);
}
