using Gum.Avalonia.Plugins.Fonts;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins;
using Gum.Plugins.FontsPlugin;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.SettingsWindow;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The font settings are a page of the Settings window, not a tab behind Edit > Font Settings.</summary>
public class MainFontsPluginTests
{
    private readonly SettingsPageRegistry _registry;
    private readonly Mock<IProjectState> _projectState;
    private readonly MenuModel _menu;

    public MainFontsPluginTests()
    {
        _registry = new SettingsPageRegistry();
        _projectState = new Mock<IProjectState>();
        _menu = new MenuModel();

        MainFontsPlugin plugin = new MainFontsPlugin(
            Mock.Of<IFontManager>(),
            Mock.Of<IWireframeCommands>(),
            Mock.Of<IDispatcher>(),
            _projectState.Object,
            Mock.Of<IPluginManager>())
        {
            FileCommands = Mock.Of<IFileCommands>(),
            GuiCommands = Mock.Of<IGuiCommands>(),
            DialogService = Mock.Of<IDialogService>(),
            Menu = _menu,
            SettingsPageRegistry = _registry,
        };
        plugin.StartUp();
    }

    [Fact]
    public void StartUp_RegistersAFontsPageInTheProjectSection()
    {
        SettingsPage page = _registry.Pages.ShouldHaveSingleItem();

        page.Id.ShouldBe(SettingsPageIds.Fonts);
        page.Title.ShouldBe("Fonts");
        page.Section.ShouldBe(SettingsSection.Project);
        page.Content.ShouldBeOfType<FontsViewModel>();
    }

    [Fact]
    public void ThePage_OffersItsSettingLabelsToTheSearch()
    {
        SettingsPage page = _registry.Pages.ShouldHaveSingleItem();

        page.Keywords.ShouldContain("Add Font Range");
        page.Keywords.ShouldContain("Font Generator");
        page.Keywords.ShouldContain("Use Font Character File");
    }

    [Fact]
    public void StartUp_AddsNoFontSettingsMenuItem()
    {
        _menu.GetItem("Edit").ShouldBeNull();
    }

    [Fact]
    public void ActivatingThePage_FillsTheViewModelFromTheLoadedProject()
    {
        _projectState.SetupGet(x => x.GumProjectSave).Returns(new GumProjectSave { FontSpacingHorizontal = 7 });
        _projectState.SetupGet(x => x.ProjectDirectory).Returns("C:/project/");
        SettingsPage page = _registry.Pages.Single();

        page.Activated!.Invoke();

        ((FontsViewModel)page.Content).FontSpacingHorizontal.ShouldBe(7);
    }

    [Fact]
    public void ActivatingThePage_WithNoProjectLoaded_ChangesNothing()
    {
        _projectState.SetupGet(x => x.GumProjectSave).Returns((GumProjectSave?)null);
        SettingsPage page = _registry.Pages.Single();

        Should.NotThrow(() => page.Activated!.Invoke());
    }
}
