using Gum.Controls;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

public class TreeNodeAddAsChildClickLogicTests : BaseTestClass
{
    private readonly TreeNodeAddAsChildClickLogic _logic;

    public TreeNodeAddAsChildClickLogicTests()
    {
        _logic = new TreeNodeAddAsChildClickLogic();
    }

    [Fact]
    public void ShouldAddAsChild_ControlAndShiftHeld_ReturnsTrue()
    {
        bool result = _logic.ShouldAddAsChild(
            hasClickedNode: true, hasExistingSelection: true, clickedNodeIsSelectedNode: false,
            TreePointerButton.Left, TreeModifierKeys.Control | TreeModifierKeys.Shift);

        result.ShouldBeTrue();
    }

    [Fact]
    public void ShouldAddAsChild_OnlyControlHeld_ReturnsFalse()
    {
        // A [Flags] enum check must require both bits, not just Control - a plain Ctrl+click is
        // the pre-existing toggle-selection gesture and must not be reinterpreted.
        bool result = _logic.ShouldAddAsChild(
            hasClickedNode: true, hasExistingSelection: true, clickedNodeIsSelectedNode: false,
            TreePointerButton.Left, TreeModifierKeys.Control);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ShouldAddAsChild_NoExistingSelection_ReturnsFalse()
    {
        bool result = _logic.ShouldAddAsChild(
            hasClickedNode: true, hasExistingSelection: false, clickedNodeIsSelectedNode: false,
            TreePointerButton.Left, TreeModifierKeys.Control | TreeModifierKeys.Shift);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ShouldAddAsChild_ClickedNodeIsAlreadySelected_ReturnsFalse()
    {
        bool result = _logic.ShouldAddAsChild(
            hasClickedNode: true, hasExistingSelection: true, clickedNodeIsSelectedNode: true,
            TreePointerButton.Left, TreeModifierKeys.Control | TreeModifierKeys.Shift);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ShouldAddAsChild_RightButton_ReturnsFalse()
    {
        bool result = _logic.ShouldAddAsChild(
            hasClickedNode: true, hasExistingSelection: true, clickedNodeIsSelectedNode: false,
            TreePointerButton.Right, TreeModifierKeys.Control | TreeModifierKeys.Shift);

        result.ShouldBeFalse();
    }
}
