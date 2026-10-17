using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.SettingsWindow;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins;

/// <summary>The project's properties are pages of the Settings window, not a tab behind Edit > Properties.</summary>
public class MainPropertiesWindowPluginTests
{
    private readonly SettingsPageRegistry _registry;
    private readonly Mock<IProjectState> _projectState;
    private readonly MenuModel _menu;
    private readonly MainPropertiesWindowPlugin _plugin;

    public MainPropertiesWindowPluginTests()
    {
        _registry = new SettingsPageRegistry();
        _projectState = new Mock<IProjectState>();
        _menu = new MenuModel();

        _plugin = new MainPropertiesWindowPlugin(
            Mock.Of<IWireframeCommands>(),
            Mock.Of<IWireframeObjectManager>(),
            _projectState.Object,
            Mock.Of<IPluginManager>(),
            Mock.Of<IProjectManager>(),
            new LocalizationService())
        {
            FileCommands = Mock.Of<IFileCommands>(),
            Menu = _menu,
            SettingsPageRegistry = _registry,
        };
        _plugin.StartUp();
    }

    [Fact]
    public void StartUp_RegistersGeneralCanvasAndLocalizationPagesInTheProjectSection()
    {
        _registry.Pages.Select(page => page.Id).ShouldBe(new[]
        {
            SettingsPageIds.ProjectGeneral,
            SettingsPageIds.Canvas,
            SettingsPageIds.Localization,
        }, ignoreOrder: true);
        _registry.Pages.ShouldAllBe(page => page.Section == SettingsSection.Project);
        _registry.Pages.Select(page => ((ProjectPropertiesPageViewModel)page.Content).Section).ShouldBe(new[]
        {
            ProjectPropertiesSection.General,
            ProjectPropertiesSection.Canvas,
            ProjectPropertiesSection.Localization,
        }, ignoreOrder: true);
    }

    [Fact]
    public void StartUp_ThePagesShareOneViewModel()
    {
        _registry.Pages
            .Select(page => ((ProjectPropertiesPageViewModel)page.Content).Properties)
            .Distinct()
            .Count()
            .ShouldBe(1);
    }

    [Fact]
    public void EachPage_OffersItsRowLabelsToTheSearch()
    {
        SettingsPage canvas = _registry.Pages.Single(page => page.Id == SettingsPageIds.Canvas);

        canvas.SearchTerms.ShouldNotBeNull().Invoke().ShouldContain("Canvas Width");
    }

    [Fact]
    public void StartUp_AddsNoEditPropertiesMenuItem()
    {
        _menu.GetItem("Edit").ShouldBeNull();
    }

    [Fact]
    public void ActivatingAPage_FillsTheViewModelFromTheLoadedProject()
    {
        _projectState.SetupGet(x => x.GumProjectSave).Returns(new GumProjectSave { DefaultCanvasWidth = 777 });
        SettingsPage canvas = _registry.Pages.Single(page => page.Id == SettingsPageIds.Canvas);

        canvas.Activated!.Invoke();

        ((ProjectPropertiesPageViewModel)canvas.Content).Properties.CanvasWidth.ShouldBe(777);
    }

    [Fact]
    public void ActivatingAPage_WithNoProjectLoaded_ChangesNothing()
    {
        _projectState.SetupGet(x => x.GumProjectSave).Returns((GumProjectSave?)null);
        SettingsPage canvas = _registry.Pages.Single(page => page.Id == SettingsPageIds.Canvas);

        Should.NotThrow(() => canvas.Activated!.Invoke());
    }
}
