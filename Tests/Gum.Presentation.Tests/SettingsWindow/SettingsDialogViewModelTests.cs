using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Settings;
using Gum.SettingsWindow;
using Gum.ToolStates;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.SettingsWindow;

public class SettingsDialogViewModelTests
{
    private readonly SettingsPageRegistry _registry;
    private readonly Mock<IProjectState> _projectState;

    public SettingsDialogViewModelTests()
    {
        _registry = new SettingsPageRegistry();
        _projectState = new Mock<IProjectState>();
        _projectState.SetupGet(x => x.GumProjectSave).Returns(new GumProjectSave());
    }

    private SettingsDialogViewModel CreateViewModel()
    {
        Mock<IPluginManager> pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(x => x.GetAllPluginSummaries()).Returns(new List<PluginSummary>());

        Mock<IOptionsMonitor<ThemeSettings>> themeSettings = new Mock<IOptionsMonitor<ThemeSettings>>();
        themeSettings.SetupGet(x => x.CurrentValue).Returns(new ThemeSettings());

        Mock<IProjectManager> projectManager = new Mock<IProjectManager>();

        return new SettingsDialogViewModel(
            _registry,
            _projectState.Object,
            new GeneralSettingsViewModel(projectManager.Object, new WeakReferenceMessenger(), Mock.Of<IFileSystemRevealService>()),
            new ThemingDialogViewModel(Mock.Of<IThemingService>(), themeSettings.Object),
            new PluginsDialogViewModel(Mock.Of<IDialogService>(), pluginManager.Object, Mock.Of<IClipboardService>()));
    }

    private static SettingsPage CreatePage(string id, string title, SettingsSection section, int order = 0, Action? activated = null)
    {
        return new SettingsPage(id, title, section, new object(), order) { Activated = activated };
    }

    private static List<string> PageTitles(SettingsDialogViewModel viewModel) =>
        viewModel.NavigationEntries.Where(entry => entry.Page != null).Select(entry => entry.Title).ToList();

    [Fact]
    public void Constructor_CloseOnlyButtons()
    {
        SettingsDialogViewModel viewModel = CreateViewModel();

        viewModel.AffirmativeText.ShouldBe("Close");
        viewModel.NegativeText.ShouldBeNull();
    }

    [Fact]
    public void NavigationEntries_WithProject_ListsApplicationThenProjectPages()
    {
        _registry.Register(CreatePage("canvas", "Canvas", SettingsSection.Project));
        SettingsDialogViewModel viewModel = CreateViewModel();

        List<string> entries = viewModel.NavigationEntries.Select(entry => entry.Title).ToList();

        entries.ShouldBe(new[] { "Application", "General", "Appearance", "Plugins", "Project", "Canvas" });
    }

    [Fact]
    public void NavigationEntries_WithoutProject_OmitsProjectPagesAndHeader()
    {
        _projectState.SetupGet(x => x.GumProjectSave).Returns((GumProjectSave?)null);
        _registry.Register(CreatePage("canvas", "Canvas", SettingsSection.Project));
        SettingsDialogViewModel viewModel = CreateViewModel();

        List<string> entries = viewModel.NavigationEntries.Select(entry => entry.Title).ToList();

        entries.ShouldBe(new[] { "Application", "General", "Appearance", "Plugins" });
    }

    [Fact]
    public void NavigationEntries_RegisteredPagesSortByOrderWithinTheirSection()
    {
        _registry.Register(CreatePage("loc", "Localization", SettingsSection.Project, order: 3));
        _registry.Register(CreatePage("canvas", "Canvas", SettingsSection.Project, order: 1));
        _registry.Register(CreatePage("keys", "Keyboard shortcuts", SettingsSection.Application, order: 2));
        SettingsDialogViewModel viewModel = CreateViewModel();

        PageTitles(viewModel).ShouldBe(new[] { "General", "Appearance", "Keyboard shortcuts", "Plugins", "Canvas", "Localization" });
    }

    [Fact]
    public void Register_SameIdTwice_ReplacesThePage()
    {
        _registry.Register(CreatePage("canvas", "Canvas", SettingsSection.Project));
        _registry.Register(CreatePage("canvas", "Canvas 2", SettingsSection.Project));

        _registry.Pages.Count.ShouldBe(1);
        _registry.Pages[0].Title.ShouldBe("Canvas 2");
    }

    [Fact]
    public void SelectedPage_Initially_IsTheFirstApplicationPage()
    {
        SettingsDialogViewModel viewModel = CreateViewModel();

        viewModel.SelectedPage!.Id.ShouldBe(SettingsPageIds.General);
    }

    [Fact]
    public void SelectPage_WithRegisteredId_SelectsAndActivatesIt()
    {
        int activations = 0;
        _registry.Register(CreatePage("canvas", "Canvas", SettingsSection.Project, activated: () => activations++));
        SettingsDialogViewModel viewModel = CreateViewModel();

        viewModel.SelectPage("canvas");

        viewModel.SelectedPage!.Id.ShouldBe("canvas");
        activations.ShouldBe(1);
    }

    [Fact]
    public void SelectPage_WithUnknownId_KeepsTheCurrentPage()
    {
        SettingsDialogViewModel viewModel = CreateViewModel();

        viewModel.SelectPage("nope");

        viewModel.SelectedPage!.Id.ShouldBe(SettingsPageIds.General);
    }

    [Fact]
    public void SelectPage_ForProjectPageWithoutProject_KeepsTheCurrentPage()
    {
        _projectState.SetupGet(x => x.GumProjectSave).Returns((GumProjectSave?)null);
        _registry.Register(CreatePage("canvas", "Canvas", SettingsSection.Project));
        SettingsDialogViewModel viewModel = CreateViewModel();

        viewModel.SelectPage("canvas");

        viewModel.SelectedPage!.Id.ShouldBe(SettingsPageIds.General);
    }

    [Fact]
    public void SelectedEntry_SetToAHeader_IsIgnored()
    {
        SettingsDialogViewModel viewModel = CreateViewModel();
        SettingsNavigationEntry header = viewModel.NavigationEntries.First(entry => entry.Page == null);

        viewModel.SelectedEntry = header;

        viewModel.SelectedPage!.Id.ShouldBe(SettingsPageIds.General);
        viewModel.SelectedEntry!.Page.ShouldNotBeNull();
    }

    [Fact]
    public void SelectedScopeText_ForApplicationPage_SaysItAppliesToTheMachine()
    {
        SettingsDialogViewModel viewModel = CreateViewModel();

        viewModel.SelectedScopeText.ShouldBe("Applies to this machine, whatever project is open.");
    }

    [Fact]
    public void SelectedScopeText_ForProjectPage_NamesTheProjectFile()
    {
        _projectState.SetupGet(x => x.GumProjectSave).Returns(new GumProjectSave { FullFileName = "C:/games/MyGame/MyGame.gumx" });
        _registry.Register(CreatePage("canvas", "Canvas", SettingsSection.Project));
        SettingsDialogViewModel viewModel = CreateViewModel();

        viewModel.SelectPage("canvas");

        viewModel.SelectedScopeText.ShouldBe("Saved in MyGame.gumx.");
    }

    [Fact]
    public void SelectedScopeText_ForProjectPageOfUnsavedProject_SaysSavedInTheProject()
    {
        _registry.Register(CreatePage("canvas", "Canvas", SettingsSection.Project));
        SettingsDialogViewModel viewModel = CreateViewModel();

        viewModel.SelectPage("canvas");

        viewModel.SelectedScopeText.ShouldBe("Saved in the project.");
    }

    [Fact]
    public void SearchText_FiltersPagesByTitle()
    {
        _registry.Register(CreatePage("canvas", "Canvas", SettingsSection.Project));
        SettingsDialogViewModel viewModel = CreateViewModel();

        viewModel.SearchText = "canv";

        PageTitles(viewModel).ShouldBe(new[] { "Canvas" });
    }

    [Fact]
    public void SearchText_MatchesPageKeywords()
    {
        _registry.Register(new SettingsPage("canvas", "Canvas", SettingsSection.Project, new object(), 0)
        {
            Keywords = new[] { "checker", "guides" },
        });
        SettingsDialogViewModel viewModel = CreateViewModel();

        viewModel.SearchText = "GUIDES";

        PageTitles(viewModel).ShouldBe(new[] { "Canvas" });
    }

    [Fact]
    public void SearchText_DropsHeaderOfSectionWithNoMatches()
    {
        _registry.Register(CreatePage("canvas", "Canvas", SettingsSection.Project));
        SettingsDialogViewModel viewModel = CreateViewModel();

        viewModel.SearchText = "plug";

        viewModel.NavigationEntries.Select(entry => entry.Title).ToList().ShouldBe(new[] { "Application", "Plugins" });
    }

    [Fact]
    public void SearchText_WhenSelectedPageIsFilteredOut_SelectsFirstMatch()
    {
        SettingsDialogViewModel viewModel = CreateViewModel();

        viewModel.SearchText = "plug";

        viewModel.SelectedPage!.Id.ShouldBe(SettingsPageIds.Plugins);
    }

    [Fact]
    public void SearchText_Cleared_RestoresAllPagesAndKeepsSelection()
    {
        _registry.Register(CreatePage("canvas", "Canvas", SettingsSection.Project));
        SettingsDialogViewModel viewModel = CreateViewModel();
        viewModel.SelectPage("canvas");
        viewModel.SearchText = "canv";

        viewModel.SearchText = "";

        PageTitles(viewModel).Count.ShouldBe(4);
        viewModel.SelectedPage!.Id.ShouldBe("canvas");
    }
}
