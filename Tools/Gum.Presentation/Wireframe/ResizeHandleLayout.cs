using System.Numerics;
using Gum.Services;

namespace Gum.Wireframe;

/// <summary>
/// Sizes and positions the 8 resize handles drawn around a rectangle. The editor canvas
/// (<c>ResizeHandles</c>) and the Texture Coordinates canvas (<c>RectangleSelector</c>) both lay
/// out their handles with this, so a sizing or placement change applies to both.
/// </summary>
public class ResizeHandleLayout
{
    /// <summary>The handle size in device-independent pixels at 100% zoom.</summary>
    public const float HandleSize = 12;

    private readonly ICanvasDisplayScale _displayScale;

    public ResizeHandleLayout(ICanvasDisplayScale displayScale)
    {
        _displayScale = displayScale;
    }

    /// <summary>
    /// How far, in device-independent pixels, the black inner rectangle drawn inside each handle is
    /// inset from the handle's edges.
    /// </summary>
    public const float InnerHandleInset = 1;

    /// <summary>The handle size in world units at the given camera zoom.</summary>
    public float GetHandleWorldSize(float zoom) => _displayScale.ToWorld(HandleSize, zoom);

    /// <summary>The inner handle size in world units at the given camera zoom.</summary>
    public float GetInnerHandleWorldSize(float zoom) => _displayScale.ToWorld(HandleSize - 2 * InnerHandleInset, zoom);

    /// <summary>The inner handle's inset from its handle in world units at the given camera zoom.</summary>
    public float GetInnerHandleWorldInset(float zoom) => _displayScale.ToWorld(InnerHandleInset, zoom);

    /// <summary>
    /// Returns the top-left of the handle for <paramref name="side"/>. Corner handles sit just
    /// outside the rectangle's corners; edge handles sit just outside the edge, centered on it.
    /// </summary>
    public Vector2 GetHandlePosition(ResizeSide side, float left, float top, float width, float height, float handleSize)
    {
        float halfSize = handleSize / 2.0f;
        float right = left + width;
        float bottom = top + height;
        float centerX = left + width / 2.0f - halfSize;
        float centerY = top + height / 2.0f - halfSize;

        return side switch
        {
            ResizeSide.TopLeft => new Vector2(left - handleSize, top - handleSize),
            ResizeSide.Top => new Vector2(centerX, top - handleSize),
            ResizeSide.TopRight => new Vector2(right, top - handleSize),
            ResizeSide.Right => new Vector2(right, centerY),
            ResizeSide.BottomRight => new Vector2(right, bottom),
            ResizeSide.Bottom => new Vector2(centerX, bottom),
            ResizeSide.BottomLeft => new Vector2(left - handleSize, bottom),
            ResizeSide.Left => new Vector2(left - handleSize, centerY),
            _ => throw new System.ArgumentOutOfRangeException(nameof(side), side, null)
        };
    }
}
