using System.Numerics;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Where a <c>[Custom]</c> letter's cell lands when it is scaled about the point its ScaleXOrigin/ScaleYOrigin
/// name, shared by the runtimes so they place a scaled letter the same way (#6550).
/// </summary>
public static class LetterCellScale
{
    /// <summary>
    /// The top-left corner of the cell (<paramref name="left"/>, <paramref name="top"/>, <paramref name="width"/>,
    /// <paramref name="height"/>) after scaling it by (<paramref name="scaleX"/>, <paramref name="scaleY"/>) about
    /// the cell point the origins name. <paramref name="baselineFromTop"/> is the anchor for
    /// <see cref="VerticalAlignment.TextBaseline"/>. Rotation, when the letter has one, turns about this corner.
    /// </summary>
    public static Vector2 GetScaledTopLeft(float left, float top, float width, float height, float baselineFromTop,
        float scaleX, float scaleY, HorizontalAlignment scaleXOrigin, VerticalAlignment scaleYOrigin)
    {
        float anchorX = scaleXOrigin switch
        {
            HorizontalAlignment.Left => 0,
            HorizontalAlignment.Right => width,
            _ => width / 2,
        };
        float anchorY = scaleYOrigin switch
        {
            VerticalAlignment.Top => 0,
            VerticalAlignment.Bottom => height,
            VerticalAlignment.TextBaseline => baselineFromTop,
            _ => height / 2,
        };
        return new Vector2(left + anchorX * (1 - scaleX), top + anchorY * (1 - scaleY));
    }
}
