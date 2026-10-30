using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// New Project with a C# project, through the real dialog window and the real creation (no restore): what the
/// Include demo screen choice leaves in the loaded project and on disk, for the plain and a named theme.
/// </summary>
public class NewProjectCodeProjectScenarioTests
{
    private static readonly TimeSpan AsyncWork = TimeSpan.FromSeconds(90);

    // Every folder the tree shows, and the project file it marks, as paths relative to the root.
    private static void Collect(NewProjectLayoutNode node, string prefix, List<string> folders, List<string> mainFiles)
    {
        foreach (NewProjectLayoutNode child in node.Children)
        {
            string path = prefix.Length == 0 ? child.Name : prefix + "/" + child.Name;
            if (child.IsFolder)
            {
                folders.Add(path);
                Collect(child, path, folders, mainFiles);
            }
            else if (child.IsMainFile)
            {
                mainFiles.Add(path);
            }
        }
    }

    [AvaloniaTheory]
    [InlineData("Standard", false)]
    [InlineData("Standard", true)]
    [InlineData("Bubblegum", false)]
    [InlineData("Bubblegum", true)]
    public void DemoScreenChoice_IsAppliedToTheLoadedProjectAndTheFiles(string theme, bool includeDemo)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string location = Path.Combine(tree.Project.ProjectFolder, "Out");
        Directory.CreateDirectory(location);
        List<string> shownFolders = new List<string>();
        List<string> shownMainFiles = new List<string>();
        tree.Dialogs.AnswerNextInWindow<NewProjectDialogViewModel>(window =>
        {
            NewProjectDialogViewModel viewModel = (NewProjectDialogViewModel)window.Window.DataContext!;
            viewModel.ProjectName = "Game";
            viewModel.Location = location;
            viewModel.IncludeCodeProject = true;
            viewModel.RunRestore = false;
            viewModel.IsIncludeFormsControls = true;
            viewModel.IsIncludeDemoScreenGum = includeDemo;
            viewModel.ThemeSelection.SelectedTheme = theme;
            Dispatcher.UIThread.RunJobs();
            Collect(viewModel.FolderTree.Single(), string.Empty, shownFolders, shownMainFiles);
            viewModel.AffirmativeCommand.Execute(null);
            for (int i = 0; i < 1500 && window.Result == null; i++)
            {
                Thread.Sleep(20);
                Dispatcher.UIThread.RunJobs();
                window.Layout();
            }
            if (window.Result == null)
            {
                throw new InvalidOperationException($"The dialog did not close. Busy: {viewModel.IsBusy}. Error: {viewModel.ErrorMessage}. Restore failure: {viewModel.RestoreFailureText}. Validation: {viewModel.ValidationMessage}. Refusal: {viewModel.RefusalText}.");
            }
        });

        tree.PickMainMenu("File", "New Project");
        tree.WaitUntil(() => tree.SelectedState.SelectedScreen?.Name == NewProjectLogic.StartingScreenName, AsyncWork, "the starting screen");
        tree.SaveAll();

        GumProjectSave project = TestAppBuilder.Services.GetRequiredService<IProjectManager>().GumProjectSave.ShouldNotBeNull();
        string gumFolder = Path.Combine(location, "Game", "Content", "GumProject");
        project.FullFileName!.Replace((char)92, '/').ShouldEndWith("Game/Content/GumProject/GumProject.gumj");
        File.Exists(Path.Combine(location, "Game", "Game.csproj")).ShouldBeTrue();

        // The folder tree shows where the project goes; each folder and the marked project file are really there.
        string gameFolder = Path.Combine(location, "Game");
        shownFolders.ShouldContain("Content/GumProject/Screens");
        foreach (string folder in shownFolders)
        {
            Directory.Exists(Path.Combine(gameFolder, folder)).ShouldBeTrue($"{folder} is shown in the dialog");
        }
        shownMainFiles.ShouldBe(new[] { "Content/GumProject/GumProject.gumj" });
        File.Exists(Path.Combine(gameFolder, shownMainFiles.Single())).ShouldBeTrue();

        List<string> demoScreens = project.Screens.Select(screen => screen.Name).Where(name => name.EndsWith("DemoScreenGum")).ToList();
        string[] demoFiles = Directory.GetFiles(gumFolder, "DemoScreenGum.*", SearchOption.AllDirectories);
        if (includeDemo)
        {
            demoScreens.ShouldHaveSingleItem();
            demoFiles.ShouldNotBeEmpty();
        }
        else
        {
            demoScreens.ShouldBeEmpty();
            demoFiles.ShouldBeEmpty();
        }
    }

    [AvaloniaFact]
    public void GumOnly_WithAThemeThatNeedsFontRanges_StillWidensThem_EvenThoughTheDialogDoesNotListTheChange()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string location = Path.Combine(tree.Project.ProjectFolder, "Out");
        Directory.CreateDirectory(location);
        string defaultRanges = new GumProjectSave().FontRanges;
        tree.Dialogs.AnswerNext<NewProjectDialogViewModel>(dialog =>
        {
            dialog.ProjectName = "Themed";
            dialog.Location = location;
            dialog.IsIncludeFormsControls = true;
            dialog.ThemeSelection.SelectedTheme = "Bubblegum";
            dialog.ThemeSelection.HasRequirements.ShouldBeTrue("Bubblegum needs project changes");
            return true;
        });

        tree.PickMainMenu("File", "New Project");
        tree.WaitUntil(() => tree.SelectedState.SelectedScreen?.Name == NewProjectLogic.StartingScreenName, AsyncWork, "the starting screen");

        GumProjectSave project = TestAppBuilder.Services.GetRequiredService<IProjectManager>().GumProjectSave.ShouldNotBeNull();
        project.FontRanges.ShouldNotBe(defaultRanges, "the theme's characters were added to the font ranges");
    }
}
