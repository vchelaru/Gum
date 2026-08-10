using Gum.DataTypes;
using Gum.Managers;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.TreeView;

public class ElementTreeViewManagerRefreshTests : BaseTestClass
{
    [Fact]
    public void RefreshChildNodes_WhenRefreshReparentsAChild_VisitsEveryChildWithoutThrowing()
    {
        // Refreshing an element whose folder changed moves its node under the folder node that now
        // owns it, mutating the collection being walked. Enumerating it directly threw
        // "Collection was modified" and aborted the whole tree refresh.
        GumTreeNode componentsNode = new GumTreeNode { Text = "Components" };
        GumTreeNode folderNode = new GumTreeNode { Text = "Widgets" };
        componentsNode.AddChild(folderNode);

        GumTreeNode first = new GumTreeNode { Tag = new ComponentSave { Name = "First" } };
        GumTreeNode moved = new GumTreeNode { Tag = new ComponentSave { Name = "Moved" } };
        GumTreeNode last = new GumTreeNode { Tag = new ComponentSave { Name = "Last" } };
        componentsNode.AddChild(first);
        componentsNode.AddChild(moved);
        componentsNode.AddChild(last);

        List<GumTreeNode> visited = new List<GumTreeNode>();

        ElementTreeViewManager.RefreshChildNodes(componentsNode, child =>
        {
            visited.Add(child);
            if (child == moved)
            {
                ((ITreeNodeMutable)child).RemoveSelf();
                ((ITreeNodeMutable)folderNode).AddChild((ITreeNodeMutable)child);
            }
        });

        visited.ShouldBe(new[] { folderNode, first, moved, last });
        moved.Parent.ShouldBeSameAs(folderNode);
    }

    [Fact]
    public void RefreshChildNodes_WithNoChildren_DoesNotInvokeTheCallback()
    {
        GumTreeNode node = new GumTreeNode { Text = "Components" };
        List<GumTreeNode> visited = new List<GumTreeNode>();

        ElementTreeViewManager.RefreshChildNodes(node, visited.Add);

        visited.ShouldBeEmpty();
    }
}
