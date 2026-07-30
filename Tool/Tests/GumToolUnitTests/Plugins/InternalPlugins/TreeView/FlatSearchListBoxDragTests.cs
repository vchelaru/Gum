using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using Shouldly;
using System.Windows.Forms;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.TreeView;

// Issue #4123: dragging a Project-panel search result must produce the same drag payload shape a
// real tree node does, so the Wireframe drop side (MultiSelectTreeView.ExtractDraggedNodes, which
// reads TreeNode.Tag) handles it identically with no drop-side changes.
public class FlatSearchListBoxDragTests : BaseTestClass
{
    [Fact]
    public void CreateDragNode_ReturnsTreeNodeTaggedWithBackingObject()
    {
        ComponentSave backingObject = new ComponentSave { Name = "MyComponent" };
        SearchItemViewModel item = new SearchItemViewModel { BackingObject = backingObject };

        TreeNode dragNode = FlatSearchListBox.CreateDragNode(item);

        dragNode.Tag.ShouldBeSameAs(backingObject);
    }
}
