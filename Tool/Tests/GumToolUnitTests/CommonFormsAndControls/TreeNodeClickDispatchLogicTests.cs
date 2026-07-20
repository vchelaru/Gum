using CommonFormsAndControls;
using Shouldly;
using System.Windows.Forms;
using Xunit;

namespace GumToolUnitTests.CommonFormsAndControls;

public class TreeNodeClickDispatchLogicTests : BaseTestClass
{
    private readonly TreeNodeClickDispatchLogic _logic;

    public TreeNodeClickDispatchLogicTests()
    {
        _logic = new TreeNodeClickDispatchLogic();
    }

    [Fact]
    public void GetReaction_NullNodeAndAlwaysHaveOneNodeSelectedFalse_ReturnsDeselectAll()
    {
        TreeNodeClickReaction reaction = _logic.GetReaction(
            hasClickedNode: false, hasExistingSelection: true, alwaysHaveOneNodeSelected: false,
            Keys.None, MultiSelectBehavior.CtrlDown);

        reaction.ShouldBe(TreeNodeClickReaction.DeselectAll);
    }

    [Fact]
    public void GetReaction_NullNodeAndAlwaysHaveOneNodeSelectedTrue_ReturnsNone()
    {
        // A null node normally deselects everything, but AlwaysHaveOneNodeSelected forbids an
        // empty selection, so nothing should happen.
        TreeNodeClickReaction reaction = _logic.GetReaction(
            hasClickedNode: false, hasExistingSelection: true, alwaysHaveOneNodeSelected: true,
            Keys.None, MultiSelectBehavior.CtrlDown);

        reaction.ShouldBe(TreeNodeClickReaction.None);
    }

    [Fact]
    public void GetReaction_NoExistingSelection_ReturnsToggleSelection()
    {
        // With nothing currently selected, even a plain click toggles the clicked node on.
        TreeNodeClickReaction reaction = _logic.GetReaction(
            hasClickedNode: true, hasExistingSelection: false, alwaysHaveOneNodeSelected: false,
            Keys.None, MultiSelectBehavior.CtrlDown);

        reaction.ShouldBe(TreeNodeClickReaction.ToggleSelection);
    }

    [Fact]
    public void GetReaction_ControlModifier_ReturnsToggleSelection()
    {
        TreeNodeClickReaction reaction = _logic.GetReaction(
            hasClickedNode: true, hasExistingSelection: true, alwaysHaveOneNodeSelected: false,
            Keys.Control, MultiSelectBehavior.CtrlDown);

        reaction.ShouldBe(TreeNodeClickReaction.ToggleSelection);
    }

    [Fact]
    public void GetReaction_RegularClickBehavior_ReturnsToggleSelection()
    {
        // MultiSelectBehavior.RegularClick always toggles, even without a modifier.
        TreeNodeClickReaction reaction = _logic.GetReaction(
            hasClickedNode: true, hasExistingSelection: true, alwaysHaveOneNodeSelected: false,
            Keys.None, MultiSelectBehavior.RegularClick);

        reaction.ShouldBe(TreeNodeClickReaction.ToggleSelection);
    }

    [Fact]
    public void GetReaction_ShiftModifier_ReturnsRangeSelect()
    {
        TreeNodeClickReaction reaction = _logic.GetReaction(
            hasClickedNode: true, hasExistingSelection: true, alwaysHaveOneNodeSelected: false,
            Keys.Shift, MultiSelectBehavior.CtrlDown);

        reaction.ShouldBe(TreeNodeClickReaction.RangeSelect);
    }

    [Fact]
    public void GetReaction_NoModifierWithExistingSelection_ReturnsSingleSelect()
    {
        TreeNodeClickReaction reaction = _logic.GetReaction(
            hasClickedNode: true, hasExistingSelection: true, alwaysHaveOneNodeSelected: false,
            Keys.None, MultiSelectBehavior.CtrlDown);

        reaction.ShouldBe(TreeNodeClickReaction.SingleSelect);
    }
}
