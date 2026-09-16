using System.Linq;
using Gum.Menus;
using Gum.Services;
using Gum.ViewModels;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Menus;

public class ProjectTitleContextMenuBuilderTests
{
    [Fact]
    public void Build_RevealItem_CallsRevealFile_WhenFileExists()
    {
        Mock<IFileSystemRevealService> revealService = new();
        Mock<IClipboardService> clipboardService = new();

        var items = ProjectTitleContextMenuBuilder.Build(
            @"C:\Projects\MyGame\MyGame.gumx", revealService.Object, clipboardService.Object, fileExists: _ => true);
        ContextMenuItemViewModel revealItem = items.Single(i => i.Text == "View in explorer");
        revealItem.IsEnabled.ShouldBeTrue();

        revealItem.Action!.Invoke();

        revealService.Verify(r => r.RevealFile(@"C:\Projects\MyGame\MyGame.gumx"), Times.Once);
    }

    [Fact]
    public void Build_RevealItem_IsDisabled_WhenFileDoesNotExist()
    {
        var items = ProjectTitleContextMenuBuilder.Build(
            @"C:\Projects\MyGame\MyGame.gumx", Mock.Of<IFileSystemRevealService>(), Mock.Of<IClipboardService>(),
            fileExists: _ => false);

        ContextMenuItemViewModel revealItem = items.Single(i => i.Text == "View in explorer");

        revealItem.IsEnabled.ShouldBeFalse();
        revealItem.Action.ShouldBeNull();
    }

    [Fact]
    public void Build_CopyItem_CallsClipboardSetText_WithFullPath()
    {
        Mock<IClipboardService> clipboardService = new();

        var items = ProjectTitleContextMenuBuilder.Build(
            @"C:\Projects\MyGame\MyGame.gumx", Mock.Of<IFileSystemRevealService>(), clipboardService.Object,
            fileExists: _ => true);
        ContextMenuItemViewModel copyItem = items.Single(i => i.Text == "Copy full path");
        copyItem.IsEnabled.ShouldBeTrue();

        copyItem.Action!.Invoke();

        clipboardService.Verify(c => c.SetText(@"C:\Projects\MyGame\MyGame.gumx"), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Build_BothItems_AreDisabled_WhenPathIsEmpty(string? fullPath)
    {
        var items = ProjectTitleContextMenuBuilder.Build(
            fullPath, Mock.Of<IFileSystemRevealService>(), Mock.Of<IClipboardService>(), fileExists: _ => true);

        items.ShouldAllBe(i => i.IsEnabled == false && i.Action == null);
    }
}
