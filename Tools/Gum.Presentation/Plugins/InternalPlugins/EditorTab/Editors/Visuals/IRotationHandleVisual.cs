namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// The subset of the tool-side (XNALIKE-rendering) <c>RotationHandleVisual</c> that
/// <see cref="Handlers.RotationInputHandler"/> needs to make its rotation decisions, exposed
/// without referencing the underlying <c>LineCircle</c>-based drawing type (XNALIKE-only,
/// unreachable from headless <c>Gum.Presentation</c>).
/// </summary>
public interface IRotationHandleVisual
{
    bool HandleVisible { get; }

    bool HandleHasCursorOver(float worldX, float worldY);
}
