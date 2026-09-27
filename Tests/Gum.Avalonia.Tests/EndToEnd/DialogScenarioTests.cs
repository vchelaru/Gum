using Avalonia.Headless.XUnit;
using Gum.Avalonia.Shell;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.ImportPlugin.ViewModel;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels;
using Gum.Services.Dialogs;
using GumFormsPlugin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the dialogs the main menu and the Project tree open (inventory area DLG,
/// and the FILE items they serve): each picks the menu item, answers the dialog as a user fills it
/// in, checks the project the tool now has and what it saved, and ends with the shared oracles.
/// Creating or loading a whole project records no undo, so none of these undo back.
/// </summary>
[Trait("Category", "EndToEnd")]
public class DialogScenarioTests
{
    private static readonly TimeSpan AsyncWork = TimeSpan.FromSeconds(60);

    private static IServiceProvider Services => TestAppBuilder.Services;

    #region New project and Forms

    [AvaloniaFact]
    [Trait("Feature", "DLG-004")]
    [Trait("Feature", "FILE-001")]
    public void NewProject_WithoutForms_SavesAProjectWithAStartingScreen()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Dialogs.AnswerNext<NewProjectDialogViewModel>(dialog =>
        {
            dialog.IsIncludeFormsControls = false;
            return true;
        });
        tree.Dialogs.AnswerNextSaveFile(tree.Project.ProjectFilePath);
        // The folder holds the project the harness started with.
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);

        tree.PickMainMenu("File", "New Project");
        tree.WaitUntil(() => tree.SelectedState.SelectedScreen?.Name == NewProjectLogic.StartingScreenName, AsyncWork, "the new project's starting screen");

        tree.Dialogs.Messages.Single().ShouldContain("is not empty");
        GumProjectSave project = Services.GetRequiredService<IProjectManager>().GumProjectSave.ShouldNotBeNull();
        // The editor tab, which sits out headlessly, fills the canvas sizes of every project the tool opens.
        project.CustomCanvasSizes ??= new List<CustomCanvasSize>();
        tree.SaveAll();
        project.FullFileName.ShouldBe(tree.Project.ProjectFilePath);
        project.Screens.Select(screen => screen.Name).ShouldBe(new[] { NewProjectLogic.StartingScreenName });
        project.Components.ShouldBeEmpty();
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Screens", NewProjectLogic.StartingScreenName + ".gusx")).ShouldBeTrue();
        tree.ChildTexts(tree.RootNode("Screens")).ShouldBe(new[] { NewProjectLogic.StartingScreenName });

        tree.AssertOracles();
    }

    [AvaloniaFact(Skip = "#5304: the test output has no staged Forms themes")]
    [Trait("Feature", "DLG-004")]
    [Trait("Feature", "FILE-001")]
    public void NewProject_WithFormsAndTheDemoScreen_ImportsTheThemesComponents()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Dialogs.AnswerNext<NewProjectDialogViewModel>(dialog =>
        {
            dialog.IsIncludeFormsControls = true;
            dialog.IsIncludeDemoScreenGum = true;
            return true;
        });
        tree.Dialogs.AnswerNextSaveFile(tree.Project.ProjectFilePath);

        tree.PickMainMenu("File", "New Project");
        tree.WaitUntil(() => tree.SelectedState.SelectedScreen?.Name == NewProjectLogic.StartingScreenName, AsyncWork, "the new project's starting screen");

        GumProjectSave project = Services.GetRequiredService<IProjectManager>().GumProjectSave.ShouldNotBeNull();
        project.Components.ShouldNotBeEmpty("the Forms theme brings its components");
        project.Screens.Count.ShouldBeGreaterThan(1, "the demo screen comes with the starting screen");
        foreach (ComponentSave component in project.Components)
        {
            File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", component.Name.Replace('/', Path.DirectorySeparatorChar) + ".gucx"))
                .ShouldBeTrue($"{component.Name} was saved");
        }

        tree.AssertOracles();
    }

    [AvaloniaFact(Skip = "#5304: the Forms plugin is not loaded and the test output has no staged Forms themes")]
    [Trait("Feature", "DLG-021")]
    public void AddForms_FromTheContentMenu_ImportsTheThemesComponents_IntoAnExistingProject()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        tree.Dialogs.AnswerNext<AddFormsViewModel>(dialog =>
        {
            dialog.IsIncludeDemoScreenGum = false;
            return true;
        });

        tree.PickMainMenu("Content", "Add Forms Components");
        tree.WaitUntil(() => tree.Project.Project.Components.Count > 1, AsyncWork, "the Forms components");
        // The import adds its components one by one; let it finish.
        int count = -1;
        tree.WaitUntil(() =>
        {
            int now = tree.Project.Project.Components.Count;
            bool settled = now == count;
            count = now;
            Thread.Sleep(50);
            return settled;
        }, AsyncWork, "the Forms import to finish");

        tree.Project.Project.Components.ShouldContain(component => component.Name == "Card");
        tree.Project.Project.Screens.ShouldBeEmpty("no demo screen was asked for");
        tree.RootNode("Components").Nodes.Count.ShouldBeGreaterThan(1);

        tree.AssertOracles();
    }

    #endregion

    #region Loading and importing

    [AvaloniaFact]
    [Trait("Feature", "DLG-014")]
    [Trait("Feature", "FILE-002")]
    [Trait("Feature", "FILE-004")]
    public void LoadProject_ThenLoadRecent_More_ReopensAProjectOpenedEarlier()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        // Reopening it puts it on the recent list.
        tree.Project.SaveAndReload();
        string other = Path.Combine(Path.GetTempPath(), "GumDialogScenarios", Guid.NewGuid().ToString("N"));
        CopyProjectFiles(tree.Project.ProjectFolder, other);
        string otherProject = Path.Combine(other, Path.GetFileName(tree.Project.ProjectFilePath));
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();

        tree.Dialogs.AnswerNextOpenFile(otherProject);
        tree.PickMainMenu("File", "Load Project...");
        tree.WaitUntil(() => projectManager.GumProjectSave?.FullFileName is { } loaded && new FilePath(loaded) == new FilePath(otherProject), AsyncWork, "the picked project to load");
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Card" });

        tree.Dialogs.AnswerNext<LoadRecentViewModel>(dialog =>
        {
            dialog.SelectedItem = dialog.FilteredItems.Single(item => new FilePath(item.FullPath) == new FilePath(tree.Project.ProjectFilePath));
            return true;
        });
        tree.PickMainMenu("File", "Load Recent", "More...");
        tree.WaitUntil(() => projectManager.GumProjectSave?.FullFileName is { } loaded
            && new FilePath(loaded) == new FilePath(tree.Project.ProjectFilePath), AsyncWork, "the recent project to load");

        projectManager.GumProjectSave!.Components.Select(component => component.Name).ShouldBe(new[] { "Card" });
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Card" });
        TryDeleteFolder(other);

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-009")]
    [Trait("Feature", "TREE-019")]
    public void ImportComponents_FromTheComponentsFolderMenu_AddsAComponentFileTheProjectDidNotList()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        string cardFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Card.gucx");
        string strayFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Badge.gucx");
        File.WriteAllText(strayFile, File.ReadAllText(cardFile).Replace("<Name>Card</Name>", "<Name>Badge</Name>"));

        tree.RightClick(tree.RootNode("Components"));
        tree.Dialogs.AnswerNext<ImportComponentDialog>(dialog =>
        {
            dialog.UnfilteredFiles.Select(Path.GetFileName).ShouldBe(new[] { "Badge.gucx" }, "only the file the project does not list is offered");
            dialog.SelectedFiles.Add(dialog.FilteredFiles.Single());
            return true;
        });
        tree.PickMenu("Import Components");

        tree.Project.Project.Components.Select(component => component.Name).OrderBy(name => name).ShouldBe(new[] { "Badge", "Card" });
        tree.ChildTexts(tree.RootNode("Components")).ShouldContain("Badge");
        File.ReadAllText(tree.Project.ProjectFilePath).ShouldContain("Badge");

        tree.AssertOracles();
    }

    #endregion

    #region Tool settings

    [AvaloniaFact]
    [Trait("Feature", "DLG-013")]
    public void Theming_AppliesAColorLive_CancelRestoresIt_AndOkKeepsIt_WithoutTouchingTheProject()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        IThemingService theming = Services.GetRequiredService<IThemingService>();
        System.Drawing.Color? original = theming.CheckerA;
        ProjectFileSnapshot start = tree.SnapshotFiles();
        System.Drawing.Color picked = System.Drawing.Color.FromArgb(255, 12, 34, 56);
        try
        {
            tree.Dialogs.AnswerNext<ThemingDialogViewModel>(dialog =>
            {
                dialog.CheckerAColor = picked;
                theming.EffectiveSettings.CheckerA.ShouldBe(picked, "the color applies while the dialog is open");
                return false;
            });
            tree.PickMainMenu("View", "Theming");
            theming.CheckerA.ShouldBe(original, "Cancel puts the color back");

            tree.Dialogs.AnswerNext<ThemingDialogViewModel>(dialog =>
            {
                dialog.CheckerAColor = picked;
                return true;
            });
            tree.PickMainMenu("View", "Theming");
            theming.CheckerA.ShouldBe(picked);
            Services.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Gum.Settings.ThemeSettings>>().CurrentValue.CheckerA.ShouldBe(picked, "OK keeps the setting");
        }
        finally
        {
            theming.CheckerA = original;
        }

        tree.SnapshotFiles().ShouldMatch(start, "theming is a tool setting, not project data");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-015")]
    public void ManagePlugins_ListsTheLoadedPlugins_AndOkChangesNothing()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        PluginManager pluginManager = Services.GetRequiredService<PluginManager>();
        ProjectFileSnapshot start = tree.SnapshotFiles();
        List<(string Text, bool IsEnabled)> listed = new List<(string, bool)>();
        tree.Dialogs.AnswerNext<PluginsDialogViewModel>(dialog =>
        {
            listed.AddRange(dialog.Plugins.Select(plugin => (plugin.DisplayText, plugin.IsEnabled)));
            return true;
        });

        tree.PickMainMenu("Plugins", "Manage Plugins");

        listed.Count.ShouldBe(pluginManager.PluginContainers.Count);
        listed.ShouldContain(plugin => plugin.Text.StartsWith("State Animation Plugin", StringComparison.Ordinal) && plugin.IsEnabled);
        tree.SnapshotFiles().ShouldMatch(start, "looking at the plugins is not a project edit");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-015")]
    public void ManagePlugins_TurningAPluginOffAndOnAgain_AddsItsMenuItemOnce()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        PluginManager pluginManager = Services.GetRequiredService<PluginManager>();
        ProjectFileSnapshot start = tree.SnapshotFiles();
        tree.Dialogs.AnswerNext<PluginsDialogViewModel>(dialog =>
        {
            PluginItemViewModel htmlImport = dialog.Plugins.Single(plugin => plugin.DisplayText.StartsWith("HTML to Gum", StringComparison.Ordinal));
            htmlImport.IsEnabled = false;
            htmlImport.IsEnabled.ShouldBeFalse();
            PluginContainerFor(pluginManager, "HTML to Gum").IsEnabled.ShouldBeFalse();
            htmlImport.IsEnabled = true;
            return true;
        });

        tree.PickMainMenu("Plugins", "Manage Plugins");

        PluginContainerFor(pluginManager, "HTML to Gum").IsEnabled.ShouldBeTrue();
        Services.GetRequiredService<Gum.Menus.MenuModel>().GetItem("Content")!.Items.Single(item => item.Header == "Import").Items
            .Count(item => item.Header == "HTML…").ShouldBe(1, "turning the plugin back on adds its menu item once");
        tree.SnapshotFiles().ShouldMatch(start, "managing plugins is not a project edit");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-015")]
    public void ManagePlugins_TurningAPluginOff_HidesItsTab_AndOnShowsItAgain()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AvaloniaTabManager tabs = (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();
        tree.PickMainMenu("View", "View Animations");
        AvaloniaPluginTab animations = tabs.AllTabs.Single(tab => tab.Title == "Animations" && tab.IsVisible);
        try
        {
            tree.Dialogs.AnswerNext<PluginsDialogViewModel>(dialog =>
            {
                PluginItemViewModel stateAnimation = dialog.Plugins.Single(plugin => plugin.DisplayText.StartsWith("State Animation Plugin", StringComparison.Ordinal));
                stateAnimation.IsEnabled = false;
                animations.IsVisible.ShouldBeFalse("the tab of a plugin that is off is hidden");
                Services.GetRequiredService<Gum.Menus.MenuModel>().GetItem("View")!.Items
                    .Single(item => item.Header == "Hide Animations" || item.Header == "View Animations")
                    .IsEnabled.ShouldBeFalse("the menu item of a plugin that is off is disabled");
                stateAnimation.IsEnabled = true;
                return true;
            });

            tree.PickMainMenu("Plugins", "Manage Plugins");

            animations.IsVisible.ShouldBeTrue("turning the plugin back on shows the tab it had showing");
            tree.AssertOracles();
        }
        finally
        {
            animations.Hide();
        }
    }

    [AvaloniaFact]
    [Trait("Feature", "PROP-007")]
    [Trait("Feature", "PROP-012")]
    [Trait("Feature", "PROP-015")]
    [Trait("Feature", "PROP-019")]
    public void ProjectProperties_FromTheEditMenu_SaveEachChangeIntoTheProjectFile_AndCloseHidesTheTab()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AvaloniaTabManager tabs = (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();
        AvaloniaPluginTab tab = tabs.AllTabs.Single(candidate => candidate.Title == "Project Properties");
        ProjectPropertiesViewModel properties = (ProjectPropertiesViewModel)((global::Avalonia.Controls.Control)tab.Content).DataContext!;
        try
        {
            tree.PickMainMenu("Edit", "Properties");
            tab.IsVisible.ShouldBeTrue();

            properties.RestrictToUnitValues = true;
            properties.ShowLocalization = !properties.ShowLocalization;
            bool showLocalization = properties.ShowLocalization;
            properties.FontSpacingHorizontal = 2;
            properties.FontSpacingVertical = 3;
            tree.WaitUntil(() => SavedProject(tree).FontSpacingVertical == 3, AsyncWork, "the font spacing to save");

            GumProjectSave saved = SavedProject(tree);
            saved.RestrictToUnitValues.ShouldBeTrue();
            saved.ShowLocalizationInGum.ShouldBe(showLocalization);
            (saved.FontSpacingHorizontal, saved.FontSpacingVertical).ShouldBe((2, 3));

            // What the tab's Close button calls (ProjectPropertiesViewTests).
            properties.RequestClose();
            tab.IsVisible.ShouldBeFalse();

            tree.AssertOracles();
            // The reload refilled the tab from the saved file.
            (properties.RestrictToUnitValues, properties.FontSpacingHorizontal, properties.FontSpacingVertical).ShouldBe((true, 2, 3));
        }
        finally
        {
            tab.Hide();
        }
    }

    #endregion

    private static GumProjectSave SavedProject(ProjectTreeHarness tree) =>
        GumProjectSave.Load(tree.Project.ProjectFilePath, out _) ?? throw new InvalidOperationException("The project file did not load.");

    private static PluginContainer PluginContainerFor(PluginManager pluginManager, string nameFragment) =>
        pluginManager.PluginContainers.Values.Single(container => container.Name.Contains(nameFragment, StringComparison.Ordinal));

    private static void TryDeleteFolder(string folder)
    {
        try
        {
            Directory.Delete(folder, recursive: true);
        }
        catch (IOException)
        {
            // A file watcher may still hold it; the temp folder is cleaned up later.
        }
    }

    private static void CopyProjectFiles(string from, string to)
    {
        foreach (string file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(from, file);
            if (relative.StartsWith("UserData", StringComparison.Ordinal))
            {
                continue;
            }
            string target = Path.Combine(to, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }
}
