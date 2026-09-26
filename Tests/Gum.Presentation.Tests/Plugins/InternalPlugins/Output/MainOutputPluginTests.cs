using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.Output;
using Moq;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.Output;

public class MainOutputPluginTests
{
    private readonly MainOutputViewModel _viewModel = new(echoToConsole: false);
    private readonly Mock<IPluginTab> _tab = new();
    private readonly MainOutputPlugin _plugin;

    public MainOutputPluginTests()
    {
        Mock<ITabManager> tabManager = new();
        tabManager.Setup(t => t.AddControl(_viewModel, "Output", It.IsAny<TabLocation>()))
            .Returns(_tab.Object);
        _plugin = new MainOutputPlugin(_viewModel) { TabManager = tabManager.Object };
    }

    [Fact]
    public void StartUp_BringsTabForward_WhenAnErrorWasWrittenBeforeTheTabExisted()
    {
        // The plugin catalog reports skipped plugin assemblies before any plugin starts (#5159).
        _viewModel.AddError("Skipped plugin assembly 'Old.dll'");

        _plugin.StartUp();

        _tab.Verify(t => t.Show(), Times.Once);
        _tab.VerifySet(t => t.IsSelected = true, Times.Once);
    }

    [Fact]
    public void StartUp_LeavesTabAlone_WhenOnlyOutputWasWritten()
    {
        _viewModel.AddOutput("Loaded 38 plugin(s)");

        _plugin.StartUp();

        _tab.VerifySet(t => t.IsSelected = true, Times.Never);
    }

    [Fact]
    public void ErrorAfterStartUp_BringsTabForward()
    {
        _plugin.StartUp();

        _viewModel.AddError("bad thing");

        _tab.VerifySet(t => t.IsSelected = true, Times.Once);
    }
}
