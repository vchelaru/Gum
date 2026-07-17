using Gum.Plugins;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for PluginsDialogViewModel/PluginItemViewModel, relocated out
/// of Gum.csproj into the headless Gum.Presentation assembly (ADR-0005, #3754). The three static
/// PluginManager calls it used to make directly (AllPluginContainers/ShutDownPlugin/ReenablePlugin)
/// are now IPluginManager.GetAllPluginSummaries/DisableUserPlugin/TryEnablePlugin, so this test
/// exercises that seam via a mock rather than the concrete PluginManager. Its View
/// (PluginsDialogView) stays in the Gum tool assembly, paired via [Dialog(typeof(...))] - see
/// DialogViewResolverTests (GumToolUnitTests) for the cross-assembly resolution pin.
/// </summary>
public class PluginsDialogViewModelTests
{
    [Fact]
    public void Constructor_PopulatesOneItemPerPluginSummary()
    {
        object handle = new();
        PluginSummary summary = new("My Plugin", "My Plugin", true, false, handle);
        Mock<IPluginManager> pluginManager = new();
        pluginManager.Setup(p => p.GetAllPluginSummaries()).Returns([summary]);
        Mock<IDialogService> dialogService = new();

        PluginsDialogViewModel viewModel = new(dialogService.Object, pluginManager.Object);

        viewModel.Plugins.Count.ShouldBe(1);
        viewModel.Plugins[0].DisplayText.ShouldBe("My Plugin");
        viewModel.Plugins[0].IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void IsEnabled_SetFalse_DisablesPluginByHandle_AndAdoptsReturnedSummary()
    {
        object handle = new();
        PluginSummary initial = new("My Plugin", "My Plugin", true, false, handle);
        PluginSummary disabled = new("My Plugin", "My Plugin", false, false, handle);
        Mock<IPluginManager> pluginManager = new();
        pluginManager.Setup(p => p.GetAllPluginSummaries()).Returns([initial]);
        pluginManager.Setup(p => p.DisableUserPlugin(handle)).Returns(disabled);
        Mock<IDialogService> dialogService = new();
        PluginsDialogViewModel viewModel = new(dialogService.Object, pluginManager.Object);

        viewModel.Plugins[0].IsEnabled = false;

        pluginManager.Verify(p => p.DisableUserPlugin(handle), Times.Once);
        viewModel.Plugins[0].IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void IsEnabled_SetTrue_WithoutFailureDetails_EnablesWithoutPrompting()
    {
        object handle = new();
        PluginSummary disabled = new("My Plugin", "My Plugin", false, false, handle);
        PluginSummary reenabled = new("My Plugin", "My Plugin", true, false, handle);
        Mock<IPluginManager> pluginManager = new();
        pluginManager.Setup(p => p.GetAllPluginSummaries()).Returns([disabled]);
        pluginManager.Setup(p => p.TryEnablePlugin(handle)).Returns(reenabled);
        Mock<IDialogService> dialogService = new();
        PluginsDialogViewModel viewModel = new(dialogService.Object, pluginManager.Object);

        viewModel.Plugins[0].IsEnabled = true;

        dialogService.Verify(
            d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
            Times.Never);
        pluginManager.Verify(p => p.TryEnablePlugin(handle), Times.Once);
        viewModel.Plugins[0].IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void IsEnabled_SetTrue_WithFailureDetails_PromptsAndSkipsEnable_WhenDeclined()
    {
        object handle = new();
        PluginSummary crashed = new("My Plugin", "My Plugin(Failed in StartUp)", false, true, handle);
        Mock<IPluginManager> pluginManager = new();
        pluginManager.Setup(p => p.GetAllPluginSummaries()).Returns([crashed]);
        Mock<IDialogService> dialogService = new();
        dialogService
            .Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()))
            .Returns(MessageDialogResult.Negative);
        PluginsDialogViewModel viewModel = new(dialogService.Object, pluginManager.Object);

        viewModel.Plugins[0].IsEnabled = true;

        pluginManager.Verify(p => p.TryEnablePlugin(It.IsAny<object>()), Times.Never);
        viewModel.Plugins[0].IsEnabled.ShouldBeFalse();
    }
}
