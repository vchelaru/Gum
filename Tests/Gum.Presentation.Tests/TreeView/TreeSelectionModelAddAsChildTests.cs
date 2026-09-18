using Gum.Controls;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

public class TreeSelectionModelAddAsChildTests : BaseTestClass
{
    [Fact]
    public void HandlePointerReleased_ControlShiftClickOnOtherNode_RaisesEventAndLeavesSelectionUnchanged()
    {
        // #4837: Ctrl+Shift-click must not run the ordinary click-select reaction - it hands off
        // to the caller instead of reassigning the tree's own selection.
        TreeModifierKeys currentModifiers = TreeModifierKeys.None;
        GumTreeNodeCollection nodes = new GumTreeNodeCollection();
        GumTreeNode nodeA = new GumTreeNode();
        GumTreeNode nodeB = new GumTreeNode();
        nodes.Add(nodeA);
        nodes.Add(nodeB);

        TreeSelectionModel model = new TreeSelectionModel(nodes, () => currentModifiers, _ => { })
        {
            IsSelectingOnPush = false,
            MultiSelectBehavior = MultiSelectBehavior.CtrlDown,
        };

        model.HandlePointerPressed(nodeA, TreePointerButton.Left);
        model.HandlePointerReleased(nodeA, TreePointerButton.Left);
        model.SelectedNode.ShouldBeSameAs(nodeA);

        (GumTreeNode Clicked, GumTreeNode PreviousSelection)? raised = null;
        model.AddAsChildOfSelectionRequested += (clicked, previousSelection) => raised = (clicked, previousSelection);
        bool afterClickSelectRaised = false;
        model.AfterClickSelect += _ => afterClickSelectRaised = true;

        currentModifiers = TreeModifierKeys.Control | TreeModifierKeys.Shift;
        model.HandlePointerPressed(nodeB, TreePointerButton.Left);
        model.HandlePointerReleased(nodeB, TreePointerButton.Left);

        raised.ShouldNotBeNull();
        raised!.Value.Clicked.ShouldBeSameAs(nodeB);
        raised!.Value.PreviousSelection.ShouldBeSameAs(nodeA);
        model.SelectedNode.ShouldBeSameAs(nodeA);
        afterClickSelectRaised.ShouldBeFalse();
    }

    [Fact]
    public void HandlePointerReleased_ControlShiftClickOnAlreadySelectedNode_DoesNotRaiseEvent()
    {
        TreeModifierKeys currentModifiers = TreeModifierKeys.None;
        GumTreeNodeCollection nodes = new GumTreeNodeCollection();
        GumTreeNode nodeA = new GumTreeNode();
        nodes.Add(nodeA);

        TreeSelectionModel model = new TreeSelectionModel(nodes, () => currentModifiers, _ => { })
        {
            IsSelectingOnPush = false,
            MultiSelectBehavior = MultiSelectBehavior.CtrlDown,
        };

        model.HandlePointerPressed(nodeA, TreePointerButton.Left);
        model.HandlePointerReleased(nodeA, TreePointerButton.Left);

        bool raised = false;
        model.AddAsChildOfSelectionRequested += (_, _) => raised = true;

        currentModifiers = TreeModifierKeys.Control | TreeModifierKeys.Shift;
        model.HandlePointerPressed(nodeA, TreePointerButton.Left);
        model.HandlePointerReleased(nodeA, TreePointerButton.Left);

        raised.ShouldBeFalse();
    }
}
