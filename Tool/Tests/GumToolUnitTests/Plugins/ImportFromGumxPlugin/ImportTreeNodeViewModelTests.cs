using ImportFromGumxPlugin.ViewModels;
using Shouldly;

namespace GumToolUnitTests.Plugins.ImportFromGumxPlugin;

public class ImportTreeNodeViewModelTests
{
    [Fact]
    public void FolderIsChecked_AllChildrenChecked_ReturnsTrue()
    {
        var folder = new ImportTreeNodeViewModel("Components", "Components");
        folder.Children.Add(new ImportTreeNodeViewModel("A", "A", ElementItemType.Component) { IsChecked = true });
        folder.Children.Add(new ImportTreeNodeViewModel("B", "B", ElementItemType.Component) { IsChecked = true });

        folder.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void FolderIsChecked_AllChildrenUnchecked_ReturnsFalse()
    {
        var folder = new ImportTreeNodeViewModel("Components", "Components");
        folder.Children.Add(new ImportTreeNodeViewModel("A", "A", ElementItemType.Component));
        folder.Children.Add(new ImportTreeNodeViewModel("B", "B", ElementItemType.Component));

        folder.IsChecked.ShouldBe(false);
    }

    [Fact]
    public void FolderIsChecked_MixedChildren_ReturnsFalse()
    {
        var folder = new ImportTreeNodeViewModel("Components", "Components");
        folder.Children.Add(new ImportTreeNodeViewModel("A", "A", ElementItemType.Component) { IsChecked = true });
        folder.Children.Add(new ImportTreeNodeViewModel("B", "B", ElementItemType.Component) { IsChecked = false });

        folder.IsChecked.ShouldBe(false);
    }

    [Fact]
    public void FolderSetChecked_False_UnchecksAllChildren()
    {
        var folder = new ImportTreeNodeViewModel("Components", "Components");
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
        var folder = new ImportTreeNodeViewModel("Components", "Components");
        folder.Children.Add(new ImportTreeNodeViewModel("A", "A", ElementItemType.Component));
        folder.Children.Add(new ImportTreeNodeViewModel("B", "B", ElementItemType.Component));

        folder.IsChecked = true;

        folder.Children[0].IsChecked.ShouldBe(true);
        folder.Children[1].IsChecked.ShouldBe(true);
        folder.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void NestedFolderChecked_GrandchildChange_BubblesUpToGrandparent()
    {
        var grandparent = new ImportTreeNodeViewModel("Root", "Root");
        var parent = new ImportTreeNodeViewModel("Sub", "Sub");
        var grandchild = new ImportTreeNodeViewModel("Leaf", "Sub/Leaf", ElementItemType.Component);

        parent.Children.Add(grandchild);
        grandparent.Children.Add(parent);

        grandchild.IsChecked = true;

        parent.IsChecked.ShouldBe(true);
        grandparent.IsChecked.ShouldBe(true);
    }
}
