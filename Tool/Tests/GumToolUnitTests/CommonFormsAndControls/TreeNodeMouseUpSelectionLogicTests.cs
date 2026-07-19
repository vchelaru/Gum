using CommonFormsAndControls;
using Shouldly;
using System.Windows.Forms;
using Xunit;

namespace GumToolUnitTests.CommonFormsAndControls;

public class TreeNodeMouseUpSelectionLogicTests : BaseTestClass
{
    private readonly TreeNodeMouseUpSelectionLogic _logic;

    public TreeNodeMouseUpSelectionLogicTests()
    {
        _logic = new TreeNodeMouseUpSelectionLogic();
    }

    [Fact]
    public void ShouldSelect_RightButton_ReturnsFalse()
    {
        // Pins pre-existing behavior: right-click opens the context menu, it must not select.
        bool shouldSelect = _logic.ShouldSelect(
            Keys.None, MultiSelectBehavior.CtrlDown, isNodeInMultiSelection: false,
            isSelectingOnPush: false, MouseButtons.Right);

        shouldSelect.ShouldBeFalse();
    }

    [Fact]
    public void ShouldSelect_XButton1_ReturnsFalse()
    {
        bool shouldSelect = _logic.ShouldSelect(
            Keys.None, MultiSelectBehavior.CtrlDown, isNodeInMultiSelection: false,
            isSelectingOnPush: false, MouseButtons.XButton1);

        shouldSelect.ShouldBeFalse();
    }

    [Fact]
    public void ShouldSelect_ModifierKeyHeld_ReturnsFalse()
    {
        // Mouse-down already handled selection under a modifier; mouse-up must not re-select.
        bool shouldSelect = _logic.ShouldSelect(
            Keys.Control, MultiSelectBehavior.CtrlDown, isNodeInMultiSelection: true,
            isSelectingOnPush: false, MouseButtons.Left);

        shouldSelect.ShouldBeFalse();
    }

    [Fact]
    public void ShouldSelect_RegularClickBehavior_ReturnsFalse()
    {
        // MultiSelectBehavior.RegularClick means mouse-down already selected; mouse-up must not.
        bool shouldSelect = _logic.ShouldSelect(
            Keys.None, MultiSelectBehavior.RegularClick, isNodeInMultiSelection: true,
            isSelectingOnPush: false, MouseButtons.Left);

        shouldSelect.ShouldBeFalse();
    }

    [Fact]
    public void ShouldSelect_NodeAlreadyMultiSelected_ReturnsTrue()
    {
        // A potential drag on an already-selected node defers the actual select to mouse-up.
        bool shouldSelect = _logic.ShouldSelect(
            Keys.None, MultiSelectBehavior.CtrlDown, isNodeInMultiSelection: true,
            isSelectingOnPush: true, MouseButtons.Left);

        shouldSelect.ShouldBeTrue();
    }

    [Fact]
    public void ShouldSelect_NotSelectingOnPush_ReturnsTrue()
    {
        // Gum configures IsSelectingOnPush = false so clicks (not pushes) select.
        bool shouldSelect = _logic.ShouldSelect(
            Keys.None, MultiSelectBehavior.CtrlDown, isNodeInMultiSelection: false,
            isSelectingOnPush: false, MouseButtons.Left);

        shouldSelect.ShouldBeTrue();
    }

    [Fact]
    public void ShouldSelect_SelectingOnPushAndNotAlreadySelected_ReturnsFalse()
    {
        // Selection already happened on push; mouse-up has nothing left to do.
        bool shouldSelect = _logic.ShouldSelect(
            Keys.None, MultiSelectBehavior.CtrlDown, isNodeInMultiSelection: false,
            isSelectingOnPush: true, MouseButtons.Left);

        shouldSelect.ShouldBeFalse();
    }
}
