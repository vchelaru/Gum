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

    /// <summary>
    /// Whether <see cref="Activity"/> currently has work to do. While this is false the element does not
    /// keep its subtree from being skipped as idle, so a control that is only sometimes active
    /// (for example an open drop-down) must call <c>InvalidateSubtreeCaches</c> on its visual whenever
    /// this turns true. Defaults to true: the control is called every frame.
    /// </summary>
    bool IsActivityNeeded => true;
}
