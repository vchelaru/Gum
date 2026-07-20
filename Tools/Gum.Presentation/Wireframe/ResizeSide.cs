namespace Gum.Wireframe;

/// <summary>
/// Identifies which of the 8 resize handles (corners/edges) is under the cursor or grabbed.
/// </summary>
public enum ResizeSide
{
    None = -1,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left
}
