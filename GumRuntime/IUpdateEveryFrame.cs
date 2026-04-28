namespace Gum.Wireframe;

/// <summary>
/// Implemented by Forms controls (or other objects assigned to
/// <see cref="InteractiveGue.FormsControlAsObject"/>) that need a per-frame
/// callback regardless of focus. <see cref="Activity"/> is invoked from
/// <see cref="GraphicalUiElement.AnimateSelf"/>, so it follows the same
/// visibility and tree-membership rules as sprite animation: hidden or
/// detached elements do not tick.
/// </summary>
public interface IUpdateEveryFrame
{
    void Activity(double secondDifference);
}
