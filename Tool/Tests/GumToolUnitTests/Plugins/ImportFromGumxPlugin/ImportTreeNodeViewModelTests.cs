using ImportFromGumxPlugin.ViewModels;
using Shouldly;

namespace GumToolUnitTests.Plugins.ImportFromGumxPlugin;

public class ImportTreeNodeViewModelTests
{
    [Fact]
    public void FolderIsChecked_MixedChildren_ReturnsNull()
    {
        var folder = new ImportTreeNodeViewModel("Components", "Components");
        folder.Children.Add(new ImportTreeNodeViewModel("A", "A", ElementItemType.Component) { IsChecked = true });
        folder.Children.Add(new ImportTreeNodeViewModel("B", "B", ElementItemType.Component) { IsChecked = false });

        folder.IsChecked.ShouldBeNull();
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
}
