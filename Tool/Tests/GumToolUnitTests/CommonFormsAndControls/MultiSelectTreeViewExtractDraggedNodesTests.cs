using CommonFormsAndControls;
using Gum.Managers;
using Shouldly;
using System.Windows.Forms;
using Xunit;

namespace GumToolUnitTests.CommonFormsAndControls;

// Regression coverage for a real bug: WinForms' DataObject keys stored data by the payload's
// *runtime* type full name, not its static/base type. Control.DoDragDrop wraps a single dragged
// item in `new DataObject(item)` with no explicit format, so if the dragged item's concrete type
// isn't exactly TreeNode, ExtractDraggedNodes's `GetDataPresent(typeof(TreeNode))` check misses it
// entirely - the drop is silently rejected (DragDropEffects.None, the "no entry" cursor) for every
// node ElementTreeViewManager constructs as a TreeNode subclass (e.g. GumTreeNode).
public class MultiSelectTreeViewExtractDraggedNodesTests : BaseTestClass
{
    [Fact]
    public void ExtractDraggedNodes_SingleGumTreeNodeBoxedAsPlainTreeNode_ReturnsTheDraggedNode()
    {
        GumTreeNode draggedNode = new GumTreeNode("Circle1");
        // Mirrors MultiSelectTreeView.Theming.cs's single-node drag start:
        // DoDragDrop((object)nodeToDrag, ...), which wraps it in `new DataObject(dragData)`.
        IDataObject dataObject = new DataObject((object)draggedNode);

        TreeNode[] result = MultiSelectTreeView.ExtractDraggedNodes(dataObject);

        result.ShouldHaveSingleItem();
        result[0].ShouldBeSameAs(draggedNode);
    }

    [Fact]
    public void ExtractDraggedNodes_SinglePlainTreeNodeBoxedAsPlainTreeNode_ReturnsTheDraggedNode()
    {
        TreeNode draggedNode = new TreeNode("Circle1");
        IDataObject dataObject = new DataObject((object)draggedNode);

        TreeNode[] result = MultiSelectTreeView.ExtractDraggedNodes(dataObject);

        result.ShouldHaveSingleItem();
        result[0].ShouldBeSameAs(draggedNode);
    }

    [Fact]
    public void ExtractDraggedNodes_NoDragPayload_ReturnsEmpty()
    {
        IDataObject dataObject = new DataObject();

        TreeNode[] result = MultiSelectTreeView.ExtractDraggedNodes(dataObject);

        result.ShouldBeEmpty();
    }
}
