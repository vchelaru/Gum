using System.Runtime.ExceptionServices;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Managers;
using Gum.ProjectServices;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// File > New Project end to end through the head's real dialog window: every choice is made with the dialog's own
/// controls (typing, check boxes, drop-downs, Browse, OK, Cancel), and the result is read from disk, the project
/// the tool then has loaded and the Project tree. The C# project runs the real creation with a stand-in for
/// <c>dotnet restore</c>.
/// </summary>
[Trait("Category", "EndToEnd")]
public class NewProjectHappyPathScenarioTests
{
    private static readonly TimeSpan AsyncWork = TimeSpan.FromSeconds(90);

    private static IServiceProvider Services => TestAppBuilder.Services;

    private static IProjectManager ProjectManager => Services.GetRequiredService<IProjectManager>();

    // A folder to create the project in, made ahead as Browse would pick an existing one.
    private static string MakeLocation(ProjectTreeHarness tree)
    {
        string location = Path.Combine(tree.Project.ProjectFolder, "Out");
        Directory.CreateDirectory(location);
        return location;
    }

    // Opens File > New Project; the script runs in the dialog's window and must close it. The menu action is async,
    // so a failure in the script would only fault its task: it is rethrown here instead.
    private static void RunDialog(ProjectTreeHarness tree, Action<NewProjectDialogDriver> script)
    {
        Exception? failure = null;
        tree.Dialogs.AnswerNextInWindow<NewProjectDialogViewModel>(window =>
        {
            try
            {
                script(new NewProjectDialogDriver(window));
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
        });
        tree.PickMainMenu("File", "New Project");
        if (failure != null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static void WaitForNewProject(ProjectTreeHarness tree) =>
        tree.WaitUntil(() => tree.SelectedState.SelectedScreen?.Name == NewProjectLogic.StartingScreenName, AsyncWork, "the new project's starting screen");

    private static GumProjectSave StagedTheme(string name) => FormsAndImportScenarioTests.StagedTheme(name);

    private static List<string> TopLevelNames(IEnumerable<string> elementNames) =>
        elementNames.Select(name => name.Split('/')[0]).Distinct().ToList();

    // The theme's components are in the project, listed by the tree, and saved as files in the project's format.
    private static void AssertThemeComponentsArePresent(ProjectTreeHarness tree, GumProjectSave project, string theme, string gumFolder)
    {
        GumProjectSave staged = StagedTheme(theme);
        List<string> componentNames = project.Components.Select(component => component.Name).ToList();
        staged.Components.ShouldNotBeEmpty();
        staged.Components.Select(component => component.Name).ShouldBeSubsetOf(componentNames);
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(TopLevelNames(componentNames), ignoreOrder: true);

        string extension = project.FullFileName!.EndsWith(".gumj") ? GumProjectSave.ComponentJsonExtension : GumProjectSave.ComponentExtension;
        foreach (string name in staged.Components.Select(component => component.Name))
        {
            File.Exists(Path.Combine(gumFolder, "Components", name + "." + extension)).ShouldBeTrue($"{name} was saved");
        }
    }

    private static void AssertSameFile(string? actual, string expected) =>
        new FilePath(actual.ShouldNotBeNull()).ShouldBe(new FilePath(expected));

    #region Gum-only

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Feature", "DLG-004")]
    [Trait("Feature", "FILE-001")]
    public void GumOnly_WithTheDefaults_CreatesTheProjectFile_AndLoadsItWithTheStartingScreenAndTheFormsComponents(bool useXml)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string location = MakeLocation(tree);
        string extension = useXml ? ".gumx" : ".gumj";
        string expectedFile = Path.Combine(location, "Fresh", "Fresh" + extension);

        RunDialog(tree, dialog =>
        {
            dialog.NameBox.Text.ShouldBe("MyGame", "the name starts as MyGame");
            dialog.FileFormatBox.SelectedItem.ShouldBe(NewProjectDialogViewModel.JsonFormat, "JSON is the default format");
            dialog.CheckBoxLabeled(NewProjectDialogText.IncludeForms).IsChecked.ShouldBe(true, "Forms controls are on by default");
            dialog.CheckBoxLabeled(NewProjectDialogText.IncludeDemo).IsChecked.ShouldBe(true, "the demo screen is on by default");
            dialog.CheckBoxLabeled(NewProjectDialogText.CreateCodeProject).IsChecked.ShouldBe(false, "no C# project by default");

            dialog.TypeName("Fresh");
            dialog.TypeLocation(location);
            if (useXml)
            {
                dialog.ChooseFileFormat(NewProjectDialogViewModel.XmlFormat);
            }
            dialog.ClickOk();
        });
        WaitForNewProject(tree);

        File.Exists(expectedFile).ShouldBeTrue("the project file is on disk");
        GumProjectSave project = ProjectManager.GumProjectSave.ShouldNotBeNull();
        AssertSameFile(project.FullFileName, expectedFile);
        tree.SaveAll();
        string gumFolder = Path.GetDirectoryName(expectedFile)!;
        project.Screens.Select(screen => screen.Name).ShouldBe(new[] { NewProjectLogic.StartingScreenName }, "no demo screen by default");
        tree.ChildTexts(tree.RootNode("Screens")).ShouldBe(new[] { NewProjectLogic.StartingScreenName });
        File.Exists(Path.Combine(gumFolder, "Screens", NewProjectLogic.StartingScreenName + (useXml ? ".gusx" : ".gusj"))).ShouldBeTrue();
        AssertThemeComponentsArePresent(tree, project, "Standard", gumFolder);

        tree.Project.FollowProjectFile(project.FullFileName!);
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-004")]
    [Trait("Feature", "FILE-001")]
    public void GumOnly_WithFormsOff_CreatesAnEmptyProjectWithJustTheStartingScreen()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string location = MakeLocation(tree);

        RunDialog(tree, dialog =>
        {
            dialog.TypeName("Plain");
            dialog.TypeLocation(location);
            dialog.SetChecked(NewProjectDialogText.IncludeForms, false);
            dialog.ThemeBox.IsEffectivelyVisible.ShouldBeFalse("the theme choice goes away with the Forms controls");
            dialog.ClickOk();
        });
        WaitForNewProject(tree);

        string expectedFile = Path.Combine(location, "Plain", "Plain.gumj");
        GumProjectSave project = ProjectManager.GumProjectSave.ShouldNotBeNull();
        AssertSameFile(project.FullFileName, expectedFile);
        File.Exists(expectedFile).ShouldBeTrue();
        project.Screens.Select(screen => screen.Name).ShouldBe(new[] { NewProjectLogic.StartingScreenName });
        project.Components.ShouldBeEmpty();
        tree.ChildTexts(tree.RootNode("Screens")).ShouldBe(new[] { NewProjectLogic.StartingScreenName });
        tree.ChildTexts(tree.RootNode("Components")).ShouldBeEmpty();

        tree.SaveAll();
        tree.Project.FollowProjectFile(project.FullFileName!);
        tree.AssertOracles();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Feature", "DLG-004")]
    [Trait("Feature", "FILE-001")]
    public void GumOnly_WithTheBubblegumTheme_ImportsItsComponentsAndFonts_AndTheDemoScreenOnlyWhenTicked(bool includeDemo)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string location = MakeLocation(tree);

        RunDialog(tree, dialog =>
        {
            dialog.TypeName("Themed");
            dialog.TypeLocation(location);
            dialog.ChooseTheme("Bubblegum");
            dialog.SetChecked(NewProjectDialogText.IncludeDemo, includeDemo);
            dialog.ClickOk();
        });
        WaitForNewProject(tree);

        GumProjectSave project = ProjectManager.GumProjectSave.ShouldNotBeNull();
        string gumFolder = Path.Combine(location, "Themed");
        AssertSameFile(project.FullFileName, Path.Combine(gumFolder, "Themed.gumj"));
        tree.SaveAll();
        AssertThemeComponentsArePresent(tree, project, "Bubblegum", gumFolder);
        Directory.GetFiles(Path.Combine(gumFolder, "Fonts"), "*.txt").ShouldNotBeEmpty("the theme's fonts come along with their license text");

        List<string> screens = project.Screens.Select(screen => screen.Name).ToList();
        screens.ShouldContain(NewProjectLogic.StartingScreenName);
        if (includeDemo)
        {
            screens.Where(name => name.EndsWith("DemoScreenGum")).ShouldHaveSingleItem();
            Directory.GetFiles(gumFolder, "DemoScreenGum.*", SearchOption.AllDirectories).ShouldNotBeEmpty();
        }
        else
        {
            screens.ShouldBe(new[] { NewProjectLogic.StartingScreenName });
            Directory.GetFiles(gumFolder, "DemoScreenGum.*", SearchOption.AllDirectories).ShouldBeEmpty();
        }

        tree.Project.FollowProjectFile(project.FullFileName!);
        tree.AssertOracles();
    }

    #endregion

    #region C# project

    [AvaloniaTheory]
    [InlineData("monogame", true)]
    [InlineData("raylib", false)]
    [InlineData("kni", true)]
    [Trait("Feature", "DLG-004")]
    [Trait("Feature", "FILE-001")]
    public void CodeProject_WithAFakeRestore_CreatesTheCodeProjectFiles_AndLoadsTheGumProjectUnderContent(string platform, bool writesGame1)
    {
        SwitchableNewProjectService service = Services.GetRequiredService<SwitchableNewProjectService>();
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string location = MakeLocation(tree);
        string gameFolder = Path.Combine(location, "Game");
        string csproj = Path.Combine(gameFolder, "Game.csproj");
        List<string> restored = new List<string>();
        service.Restore = (csprojPath, onOutputLine, _) =>
        {
            restored.Add(csprojPath);
            onOutputLine?.Invoke("Restored (stand-in)");
            return Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
        };
        List<string> shownRows = new List<string>();
        try
        {
            RunDialog(tree, dialog =>
            {
                dialog.TypeName("Game");
                dialog.TypeLocation(location);
                dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
                dialog.ChoosePlatform(platform);
                dialog.CheckBoxLabeled(NewProjectDialogText.RestorePackages).IsChecked.ShouldBe(true, "restore is on by default");
                shownRows = dialog.OpenFolders();
                dialog.ClickOk();
            });
            WaitForNewProject(tree);
        }
        finally
        {
            service.Restore = null;
        }

        // The Folders section showed this layout before OK.
        string separator = Path.DirectorySeparatorChar.ToString();
        shownRows.ShouldContain(NewProjectDisplayPath.AsFolder(gameFolder));
        shownRows.ShouldContain("Content" + separator);
        shownRows.ShouldContain("GumProject" + separator);
        shownRows.ShouldContain("Screens" + separator);
        shownRows.ShouldContain("GumProject.gumj");

        restored.ShouldHaveSingleItem("the restore ran once").ShouldBe(csproj, "on the new .csproj");
        File.Exists(csproj).ShouldBeTrue();
        File.Exists(Path.Combine(gameFolder, "Program.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(gameFolder, "Game1.cs")).ShouldBe(writesGame1);
        string gumProjectFile = Path.Combine(gameFolder, "Content", "GumProject", "GumProject.gumj");
        File.Exists(gumProjectFile).ShouldBeTrue();
        File.ReadAllText(csproj).ShouldContain("Gum", Case.Insensitive, "the code project references Gum");

        GumProjectSave project = ProjectManager.GumProjectSave.ShouldNotBeNull();
        AssertSameFile(project.FullFileName, gumProjectFile);
        project.Screens.Select(screen => screen.Name).ShouldContain(NewProjectLogic.StartingScreenName);
        project.Screens.Select(screen => screen.Name).ShouldNotContain(name => DemoScreenRule.IsDemoScreenName(name), "the demo screen is off by default");
        tree.ChildTexts(tree.RootNode("Screens")).ShouldContain(NewProjectLogic.StartingScreenName);
        tree.ChildTexts(tree.RootNode("Components")).ShouldNotBeEmpty("the Forms controls came along");
        tree.Dialogs.Messages.ShouldBeEmpty("nothing went wrong");
    }

    #endregion

    #region Cancel, Browse, command line and replacing the open project

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Feature", "DLG-004")]
    [Trait("Feature", "FILE-001")]
    public void Cancel_AfterFillingTheDialogIn_KeepsTheOpenProjectLoaded_AndCreatesNothing(bool withCodeProject)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        GumProjectSave openProject = ProjectManager.GumProjectSave.ShouldNotBeNull();
        string location = Path.Combine(tree.Project.ProjectFolder, "NeverCreated");

        RunDialog(tree, dialog =>
        {
            dialog.TypeName("Game");
            dialog.TypeLocation(location);
            if (withCodeProject)
            {
                dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
            }
            dialog.ClickCancel();
        });

        ProjectManager.GumProjectSave.ShouldBeSameAs(openProject);
        openProject.Components.Select(component => component.Name).ShouldBe(new[] { "Card" });
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Card" });
        Directory.Exists(location).ShouldBeFalse("Cancel creates no folder");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-004")]
    [Trait("Feature", "FILE-001")]
    public void Browse_PutsThePickedFolderInLocation_AndTheCommandLine_AndACancelledPickChangesNothing()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string initial = MakeLocation(tree);
        string picked = Path.Combine(tree.Project.ProjectFolder, "Picked");
        Directory.CreateDirectory(picked);
        tree.Dialogs.AnswerNextFolder(picked);
        tree.Dialogs.AnswerNextFolder(null);

        RunDialog(tree, dialog =>
        {
            dialog.TypeName("Browsed");
            dialog.TypeLocation(initial);

            dialog.ClickBrowse();
            dialog.LocationBox.Text.ShouldBe(NewProjectDisplayPath.Normalize(picked));
            dialog.ViewModel.Location.ShouldBe(NewProjectDisplayPath.Normalize(picked));
            dialog.OpenCommandLine().ShouldContain(Path.Combine(picked, "Browsed", "Browsed.gumj"));

            dialog.ClickBrowse();
            dialog.LocationBox.Text.ShouldBe(NewProjectDisplayPath.Normalize(picked), "a cancelled picker leaves Location alone");
            dialog.ClickCancel();
        });

        tree.Dialogs.FolderRequests.Count.ShouldBe(2);
        tree.Dialogs.FolderRequests[0]!.InitialDirectory.ShouldBe(initial, "the picker opens on the current Location");
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-004")]
    [Trait("Feature", "FILE-001")]
    public void CommandLine_FollowsEachGroupOfChoices()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string location = MakeLocation(tree);
        string gumOnly = Path.Combine(location, "Cmd", "Cmd");
        string gameFolder = Path.Combine(location, "Cmd");

        RunDialog(tree, dialog =>
        {
            dialog.TypeName("Cmd");
            dialog.TypeLocation(location);

            // Name and location: the Gum project file.
            dialog.OpenCommandLine().ShouldBe($"gumcli new \"{gumOnly}.gumj\"", "the defaults add no option");

            // File format: the extension in the path.
            dialog.ChooseFileFormat(NewProjectDialogViewModel.XmlFormat);
            dialog.CommandLine.ShouldBe($"gumcli new \"{gumOnly}.gumx\"");
            dialog.ChooseFileFormat(NewProjectDialogViewModel.JsonFormat);

            // Theme.
            dialog.ChooseTheme("Bubblegum");
            dialog.CommandLine.ShouldBe($"gumcli new \"{gumOnly}.gumj\" --theme Bubblegum");
            dialog.ChooseTheme("Standard");

            // Forms.
            dialog.SetChecked(NewProjectDialogText.IncludeForms, false);
            dialog.CommandLine.ShouldBe($"gumcli new \"{gumOnly}.gumj\" --template empty");
            dialog.SetChecked(NewProjectDialogText.IncludeForms, true);

            // C# project and platform: the game folder and --platform.
            dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
            // Include recommended packages is on by default, so the line chains the adds and a final restore.
            string csproj = Path.Combine(gameFolder, "Cmd.csproj");
            string Add(string package) => $" && dotnet add \"{csproj}\" package {package} --no-restore";
            dialog.CommandLine.ShouldBe(
                $"gumcli new \"{gameFolder}\" --platform monogame --no-restore"
                + Add("KernSmith.MonoGameGum") + Add("Gum.Expressions")
                + $" && dotnet restore \"{csproj}\"");
            dialog.ChoosePlatform("raylib");
            dialog.CommandLine.ShouldBe(
                $"gumcli new \"{gameFolder}\" --platform raylib --no-restore"
                + Add("KernSmith.RaylibGum") + Add("Gum.Expressions") + $" && dotnet restore \"{csproj}\"");

            // Restore.
            dialog.SetChecked(NewProjectDialogText.RestorePackages, false);
            dialog.CommandLine.ShouldBe(
                $"gumcli new \"{gameFolder}\" --platform raylib --no-restore" + Add("KernSmith.RaylibGum") + Add("Gum.Expressions"));
            dialog.SetChecked(NewProjectDialogText.RestorePackages, true);

            // Recommended packages off: the plain command again.
            dialog.SetChecked(NewProjectDialogText.RecommendedPackages, false);
            dialog.CommandLine.ShouldBe($"gumcli new \"{gameFolder}\" --platform raylib");
            dialog.SetChecked(NewProjectDialogText.RecommendedPackages, true);

            // Local Gum source: KNI with Skia always links it.
            dialog.ChoosePlatform("kniskia");
            dialog.CommandLine.ShouldBe($"gumcli new \"{gameFolder}\" --platform kniskia --source-linked");

            dialog.ClickCancel();
        });
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Feature", "DLG-004")]
    [Trait("Feature", "FILE-001")]
    public void NewProject_OverAnOpenProject_ReplacesItOnlyAfterOk(bool withCodeProject)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        GumProjectSave openProject = ProjectManager.GumProjectSave.ShouldNotBeNull();
        string location = MakeLocation(tree);

        RunDialog(tree, dialog =>
        {
            dialog.TypeName("Replacement");
            dialog.TypeLocation(location);
            if (withCodeProject)
            {
                dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
                dialog.SetChecked(NewProjectDialogText.RestorePackages, false);
            }

            ProjectManager.GumProjectSave.ShouldBeSameAs(openProject, "the open project stays loaded while the dialog is open");
            tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Card" });
            dialog.ClickOk();
        });
        WaitForNewProject(tree);

        GumProjectSave replacement = ProjectManager.GumProjectSave.ShouldNotBeNull();
        replacement.ShouldNotBeSameAs(openProject);
        string expectedFile = withCodeProject
            ? Path.Combine(location, "Replacement", "Content", "GumProject", "GumProject.gumj")
            : Path.Combine(location, "Replacement", "Replacement.gumj");
        AssertSameFile(replacement.FullFileName, expectedFile);
        replacement.Components.Select(component => component.Name).ShouldNotContain("Card");
        tree.ChildTexts(tree.RootNode("Components")).ShouldNotContain("Card");
        tree.ChildTexts(tree.RootNode("Screens")).ShouldContain(NewProjectLogic.StartingScreenName);
    }

    #endregion
}
