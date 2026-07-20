namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// The subset of the tool-side (XNALIKE-rendering) <c>ResizeHandlesVisual</c> that
/// <see cref="Handlers.ResizeInputHandler"/> needs to make its resize decisions, exposed without
/// referencing the underlying <c>LineRectangle</c>-based drawing types (XNALIKE-only, unreachable
/// from headless <c>Gum.Presentation</c>).
/// </summary>
public interface IResizeHandlesVisual
{
    bool Visible { get; }

    /// <summary>
    /// The current width of the drawn handle bounds (i.e. the selected object's width), used to
    /// scale a resize side's multiplier.
    /// </summary>
    float HandlesWidth { get; }

    /// <summary>
    /// The current height of the drawn handle bounds (i.e. the selected object's height), used to
    /// scale a resize side's multiplier.
    /// </summary>
    float HandlesHeight { get; }

    /// <summary>
    /// Returns which resize handle (if any) is under the given world coordinates.
    /// </summary>
    ResizeSide GetSideOver(float worldX, float worldY);
}
