using Gum.Managers;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;
using Gum.Input;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>The Hotkeys tab rows, rendered with the platform's modifier names.</summary>
public class HotkeyViewModelTests
{
    [Fact]
    public void Constructor_PopulatesItems_FromHotkeyManager()
    {
        Mock<IHotkeyManager> hotkeyManager = new();
        hotkeyManager.Setup(x => x.Delete).Returns(KeyCombination.Pressed(GumKey.Delete));
        hotkeyManager.Setup(x => x.Copy).Returns(KeyCombination.Ctrl(GumKey.C));

        HotkeyViewModel viewModel = new(hotkeyManager.Object, new KeyCombinationFormatter(KeyDisplayStyle.Windows));

        viewModel.Items.ShouldNotBeEmpty();
        viewModel.Items.ShouldContain(item => item.Display == "Delete: Delete");
        viewModel.Items.ShouldContain(item => item.Display == "Copy: Ctrl+C");
    }

    [Fact]
    public void Constructor_OnMacOS_ShowsTheCommandKey()
    {
        Mock<IHotkeyManager> hotkeyManager = new();
        hotkeyManager.Setup(x => x.Copy).Returns(KeyCombination.Ctrl(GumKey.C));

        HotkeyViewModel viewModel = new(hotkeyManager.Object, new KeyCombinationFormatter(KeyDisplayStyle.MacOS));

        viewModel.Items.ShouldContain(item => item.Display == "Copy: ⌘C");
    }

    [Fact]
    public void Constructor_ListsEveryBindingTheManagerExposes()
    {
        Mock<IHotkeyManager> hotkeyManager = new();
        hotkeyManager.Setup(x => x.MultiSelect).Returns(KeyCombination.Shift());
        hotkeyManager.Setup(x => x.ZoomCameraInAlternative).Returns(KeyCombination.Ctrl(GumKey.Oemplus));
        hotkeyManager.Setup(x => x.Rename).Returns(KeyCombination.Pressed(GumKey.F2));

        HotkeyViewModel viewModel = new(hotkeyManager.Object, new KeyCombinationFormatter(KeyDisplayStyle.Windows));

        viewModel.Items.ShouldContain(item => item.Display == "Multi-select (click): Shift");
        viewModel.Items.ShouldContain(item => item.Display == "Zoom In (Alternative): Ctrl+=");
        viewModel.Items.ShouldContain(item => item.Display == "Rename: F2");
    }
}
