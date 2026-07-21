using RenderingLibrary;

namespace TextureCoordinateSelectionPlugin.Logic;

/// <summary>
/// The min/max/viewport/value a scrollbar should show for a given <see cref="Camera"/> axis and
/// content size. Framework-neutral so it can be applied to any scrollbar widget by the view-side
/// caller (e.g. <c>ScrollBarLogicWpf</c>).
/// </summary>
public record ScrollBarRange(double Minimum, double Maximum, double ViewportSize, double Value);

/// <summary>
/// Computes scrollbar ranges from a <see cref="Camera"/>'s position/zoom and the size of the
/// content being scrolled (e.g. the texture being edited in the Texture Coordinate Selection tab).
/// Extracted from <c>ScrollBarLogicWpf</c> (ADR-0005) so the math is testable independent of WPF.
/// </summary>
public class ScrollBarLogic
{
    /// <summary>Computes the horizontal scrollbar range for the camera's X axis.</summary>
    public ScrollBarRange CalculateHorizontalRange(Camera camera, int contentWidth)
    {
        return CalculateRange(camera.X, camera.ClientWidth, camera.Zoom, contentWidth);
    }

    /// <summary>Computes the vertical scrollbar range for the camera's Y axis.</summary>
    public ScrollBarRange CalculateVerticalRange(Camera camera, int contentHeight)
    {
        return CalculateRange(camera.Y, camera.ClientHeight, camera.Zoom, contentHeight);
    }

    private ScrollBarRange CalculateRange(float cameraPosition, int clientSize, float zoom, int contentSize)
    {
        double viewableArea = clientSize / zoom;
        double minimum = -viewableArea / 2;
        double maximum = contentSize + viewableArea / 2;
        maximum = System.Math.Max(minimum, maximum - viewableArea);

        return new ScrollBarRange(minimum, maximum, viewableArea, cameraPosition);
    }
}
