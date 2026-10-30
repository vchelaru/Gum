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
}
