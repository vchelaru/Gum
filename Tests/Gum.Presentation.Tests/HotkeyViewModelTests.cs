using Gum.Managers;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;
using Gum.Input;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for HotkeyViewModel, relocated out of Gum.csproj into the
/// headless Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM whose single injected
/// interface (IHotkeyManager) is already headless (Gum.Input.GumKey/GumKeyEventArgs, not
/// System.Windows.Forms.Keys).
/// </summary>
public class HotkeyViewModelTests
{
    [Fact]
    public void Constructor_PopulatesItems_FromHotkeyManager()
    {
        Mock<IHotkeyManager> hotkeyManager = new();
        hotkeyManager.Setup(x => x.Delete).Returns(KeyCombination.Pressed(GumKey.Delete));
        hotkeyManager.Setup(x => x.Copy).Returns(KeyCombination.Ctrl(GumKey.C));

        HotkeyViewModel viewModel = new(hotkeyManager.Object);

        viewModel.Items.ShouldNotBeEmpty();
        viewModel.Items.ShouldContain(item => item.Display == "Delete: Delete");
        viewModel.Items.ShouldContain(item => item.Display == "Copy: Ctrl+C");
    }
}
