using System.Drawing;

namespace SokolGum.Animation;

/// <summary>
/// One frame inside an <see cref="AnimationChain"/>: a texture, a source
/// sub-rectangle into that texture, a frame duration, optional flip flags
/// and an optional per-frame position offset.
///
/// Mirrors the shared <c>Gum.Graphics.Animation.AnimationFrame</c> schema
/// but uses SokolGum's <see cref="Texture2D"/> instead of XNA's. Texture
/// coordinates are stored in UV space (0–1) so the source rect is
/// reconstructible if the underlying texture changes size.
/// </summary>
public sealed class AnimationFrame
{
    public Texture2D? Texture { get; set; }
    public string? TextureName { get; set; }

    /// <summary>How long this frame shows, in seconds.</summary>
    public float FrameLength { get; set; } = 0.1f;

    public bool FlipHorizontal { get; set; }
    public bool FlipVertical { get; set; }

    // UV-space source rect (0–1). Resolved to pixel-space via Texture's size
    // when the frame is applied to a Sprite. Defaults cover the whole texture.
    public float LeftCoordinate   { get; set; } = 0f;
    public float RightCoordinate  { get; set; } = 1f;
    public float TopCoordinate    { get; set; } = 0f;
    public float BottomCoordinate { get; set; } = 1f;

    public float RelativeX { get; set; }
    public float RelativeY { get; set; }

    /// <summary>
    /// Resolves the UV-space coordinates to a pixel <see cref="Rectangle"/>
    /// against the current <see cref="Texture"/>. Returns null if no texture
    /// is assigned. Width/Height may be negative when flip flags are set.
    /// </summary>
    public Rectangle? ToPixelSourceRectangle()
    {
        if (Texture is null) return null;
        int w = Texture.Width;
        int h = Texture.Height;
        int x = (int)(LeftCoordinate * w);
        int y = (int)(TopCoordinate * h);
        int rw = (int)((RightCoordinate  - LeftCoordinate) * w);
        int rh = (int)((BottomCoordinate - TopCoordinate)  * h);
        return new Rectangle(x, y, rw, rh);
    }
}
