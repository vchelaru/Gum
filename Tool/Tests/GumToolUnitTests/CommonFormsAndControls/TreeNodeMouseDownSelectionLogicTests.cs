using CommonFormsAndControls;
using Shouldly;
using System.Windows.Forms;
using Xunit;

namespace GumToolUnitTests.CommonFormsAndControls;

public class TreeNodeMouseDownSelectionLogicTests : BaseTestClass
{
    private readonly TreeNodeMouseDownSelectionLogic _logic;

    public TreeNodeMouseDownSelectionLogicTests()
    {
        _logic = new TreeNodeMouseDownSelectionLogic();
    }

    [Fact]
    public void ShouldReactToClick_LeftButtonNoModifierNotSelectingOnPush_ReturnsFalse()
    {
        // Neither IsSelectingOnPush nor a Shift/Control/right-click reason to react on press;
        // OnMouseUp handles this case instead.
        bool shouldReact = _logic.ShouldReactToClick(
            isNodeInMultiSelection: false, MouseButtons.Left, Keys.None,
            MultiSelectBehavior.CtrlDown, isSelectingOnPush: false);

        shouldReact.ShouldBeFalse();
    }

    [Fact]
    public void ShouldReactToClick_LeftButtonNoModifierSelectingOnPush_ReturnsTrue()
    {
        bool shouldReact = _logic.ShouldReactToClick(
            isNodeInMultiSelection: false, MouseButtons.Left, Keys.None,
            MultiSelectBehavior.CtrlDown, isSelectingOnPush: true);

        shouldReact.ShouldBeTrue();
    }

    [Fact]
    public void ShouldReactToClick_NodeAlreadyMultiSelectedNoModifier_ReturnsFalse()
    {
        // Potential drag operation - defer the actual (re)select to mouse-up.
        bool shouldReact = _logic.ShouldReactToClick(
            isNodeInMultiSelection: true, MouseButtons.Left, Keys.None,
            MultiSelectBehavior.CtrlDown, isSelectingOnPush: true);

        shouldReact.ShouldBeFalse();
    }

    [Fact]
    public void ShouldReactToClick_NodeAlreadyMultiSelectedRegularClickBehavior_ReturnsTrue()
    {
        // MultiSelectBehavior.RegularClick means a click always (re)selects - no drag deferral.
        bool shouldReact = _logic.ShouldReactToClick(
            isNodeInMultiSelection: true, MouseButtons.Left, Keys.None,
            MultiSelectBehavior.RegularClick, isSelectingOnPush: true);

        shouldReact.ShouldBeTrue();
    }

    [Fact]
    public void ShouldReactToClick_ShiftHeld_ReturnsTrue()
    {
        bool shouldReact = _logic.ShouldReactToClick(
            isNodeInMultiSelection: false, MouseButtons.Left, Keys.Shift,
            MultiSelectBehavior.CtrlDown, isSelectingOnPush: false);

        shouldReact.ShouldBeTrue();
    }

    [Fact]
    public void ShouldReactToClick_ControlHeld_ReturnsTrue()
    {
        bool shouldReact = _logic.ShouldReactToClick(
            isNodeInMultiSelection: false, MouseButtons.Left, Keys.Control,
            MultiSelectBehavior.CtrlDown, isSelectingOnPush: false);

        shouldReact.ShouldBeTrue();
    }

    [Fact]
    public void ShouldReactToClick_RightButtonNoModifier_ReturnsTrue()
    {
        // Right-click selects before the context menu shows, even without IsSelectingOnPush.
        bool shouldReact = _logic.ShouldReactToClick(
            isNodeInMultiSelection: false, MouseButtons.Right, Keys.None,
            MultiSelectBehavior.CtrlDown, isSelectingOnPush: false);

        shouldReact.ShouldBeTrue();
    }

    [Fact]
    public void ShouldReactToClick_RightButtonWithModifierOnMultiSelectedNode_ReturnsFalse()
    {
        // Right-click with a modifier held on an already-multi-selected node opens a context menu
        // without changing selection.
        bool shouldReact = _logic.ShouldReactToClick(
            isNodeInMultiSelection: true, MouseButtons.Right, Keys.Control,
            MultiSelectBehavior.CtrlDown, isSelectingOnPush: false);

        shouldReact.ShouldBeFalse();
    }

    [Fact]
    public void ShouldReactToClick_RightButtonWithModifierOnNodeNotMultiSelected_ReturnsTrue()
    {
        // The right-click-with-modifier deferral only applies when the node is already part of a
        // multi-selection; otherwise the right-click-selects-before-menu rule still applies.
        bool shouldReact = _logic.ShouldReactToClick(
            isNodeInMultiSelection: false, MouseButtons.Right, Keys.Control,
            MultiSelectBehavior.CtrlDown, isSelectingOnPush: false);

        shouldReact.ShouldBeTrue();
    }
}
