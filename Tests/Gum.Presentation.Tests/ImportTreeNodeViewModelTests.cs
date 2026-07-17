using System.Collections.Generic;
using ImportFromGumxPlugin.ViewModels;
using Shouldly;

namespace Gum.Presentation.Tests;

public class ImportTreeNodeViewModelTests
{
    [Fact]
    public void FolderIsChecked_AllChildrenChecked_ReturnsTrue()
    {
        ImportTreeNodeViewModel folder = new ImportTreeNodeViewModel("Components", "Components");
        folder.Children.Add(new ImportTreeNodeViewModel("A", "A", ElementItemType.Component) { IsChecked = true });
        folder.Children.Add(new ImportTreeNodeViewModel("B", "B", ElementItemType.Component) { IsChecked = true });

        folder.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void FolderIsChecked_AllChildrenUnchecked_ReturnsFalse()
    {
        ImportTreeNodeViewModel folder = new ImportTreeNodeViewModel("Components", "Components");
        folder.Children.Add(new ImportTreeNodeViewModel("A", "A", ElementItemType.Component));
        folder.Children.Add(new ImportTreeNodeViewModel("B", "B", ElementItemType.Component));

        folder.IsChecked.ShouldBe(false);
    }

    [Fact]
    public void FolderIsChecked_MixedChildren_ReturnsFalse()
    {
        ImportTreeNodeViewModel folder = new ImportTreeNodeViewModel("Components", "Components");
        folder.Children.Add(new ImportTreeNodeViewModel("A", "A", ElementItemType.Component) { IsChecked = true });
        folder.Children.Add(new ImportTreeNodeViewModel("B", "B", ElementItemType.Component) { IsChecked = false });

        folder.IsChecked.ShouldBe(false);
    }

    [Fact]
    public void FolderSetChecked_False_UnchecksAllChildren()
    {
        ImportTreeNodeViewModel folder = new ImportTreeNodeViewModel("Components", "Components");
        folder.Children.Add(new ImportTreeNodeViewModel("A", "A", ElementItemType.Component) { IsChecked = true });
        folder.Children.Add(new ImportTreeNodeViewModel("B", "B", ElementItemType.Component) { IsChecked = true });

        folder.IsChecked = false;

        folder.Children[0].IsChecked.ShouldBe(false);
        folder.Children[1].IsChecked.ShouldBe(false);
        folder.IsChecked.ShouldBe(false);
    }

    [Fact]
    public void FolderSetChecked_PropagatesToAllChildren()
    {
        ImportTreeNodeViewModel folder = new ImportTreeNodeViewModel("Components", "Components");
        folder.Children.Add(new ImportTreeNodeViewModel("A", "A", ElementItemType.Component));
        folder.Children.Add(new ImportTreeNodeViewModel("B", "B", ElementItemType.Component));

        folder.IsChecked = true;

        folder.Children[0].IsChecked.ShouldBe(true);
        folder.Children[1].IsChecked.ShouldBe(true);
        folder.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void IsDetailsButtonVisible_NoDiffRows_ReturnsFalse()
    {
        ImportTreeNodeViewModel node = new ImportTreeNodeViewModel("Sprite", "Sprite", ElementItemType.Standard);

        node.IsDetailsButtonVisible.ShouldBe(false);
    }

    [Fact]
    public void IsDetailsButtonVisible_WithDiffRows_ReturnsTrue()
    {
        ImportTreeNodeViewModel node = new ImportTreeNodeViewModel("Sprite", "Sprite", ElementItemType.Standard);
        node.StandardDiffRows = new List<StandardDiffRowViewModel>
        {
            new StandardDiffRowViewModel("Changed", "Rotation · SetsValue: True → False"),
        };

        node.IsDetailsButtonVisible.ShouldBe(true);
    }

    [Fact]
    public void NestedFolderChecked_GrandchildChange_BubblesUpToGrandparent()
    {
        ImportTreeNodeViewModel grandparent = new ImportTreeNodeViewModel("Root", "Root");
        ImportTreeNodeViewModel parent = new ImportTreeNodeViewModel("Sub", "Sub");
        ImportTreeNodeViewModel grandchild = new ImportTreeNodeViewModel("Leaf", "Sub/Leaf", ElementItemType.Component);

        parent.Children.Add(grandchild);
        grandparent.Children.Add(parent);

        grandchild.IsChecked = true;

        parent.IsChecked.ShouldBe(true);
        grandparent.IsChecked.ShouldBe(true);
    }
}
