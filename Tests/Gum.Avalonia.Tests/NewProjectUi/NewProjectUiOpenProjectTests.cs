using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Managers;
using Gum.ProjectServices;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Gum.Avalonia.Tests.EndToEnd;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// File > New Project over a project that is already open, through the tool's real File menu and the dialog's own window:
/// the open project is replaced only after the dialog has made the new one, and a failure or a cancel leaves it alone.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiOpenProjectTests
{
    private static readonly TimeSpan AsyncWork = TimeSpan.FromSeconds(90);

    private static IServiceProvider Services => TestAppBuilder.Services;

    private static IProjectManager ProjectManager => Services.GetRequiredService<IProjectManager>();

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
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static string MakeLocation(ProjectTreeHarness tree)
    {
        string location = Path.Combine(tree.Project.ProjectFolder, "Out");
        Directory.CreateDirectory(location);
        return location;
    }

    private static void WaitFor(Func<bool> condition, string what)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > AsyncWork)
            {
                throw new TimeoutException($"Waited for {what}");
            }
            Thread.Sleep(5);
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void ARestoreFailurePanel_LeavesTheOpenProjectLoaded_UntilContinueWithoutRestore()
    {
        SwitchableNewProjectService service = Services.GetRequiredService<SwitchableNewProjectService>();
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        GumProjectSave openProject = ProjectManager.GumProjectSave.ShouldNotBeNull();
        string location = MakeLocation(tree);
        service.Restore = (_, _, _) => Task.FromResult(new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = "offline" });
        try
        {
            RunDialog(tree, window =>
            {
                NewProjectDialogViewModel viewModel = window.ViewModel;
                window.TypeName("Replacement");
                window.TypeLocation(location);
                window.SetChecked(NewProjectDialogText.CreateCodeProject, true);
                window.ClickOkWithoutWaiting();
                WaitFor(() => viewModel.IsRestoreFailed, "the restore failure");

                ProjectManager.GumProjectSave.ShouldBeSameAs(openProject, "the panel is not the end: the open project stays");
                tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Card" });
                Directory.Exists(Path.Combine(location, "Replacement")).ShouldBeTrue("the files exist already");

                window.ClickButton("Continue without restore");
                WaitFor(() => window.Result != null, "the dialog to close");
            });
        }
        finally
        {
            service.Restore = null;
        }
        tree.WaitUntil(() => tree.SelectedState.SelectedScreen?.Name == NewProjectLogic.StartingScreenName, AsyncWork, "the new project");

        ProjectManager.GumProjectSave.ShouldNotBeSameAs(openProject);
        ProjectManager.GumProjectSave!.Components.ShouldNotContain(component => component.Name == "Card");
    }

    [AvaloniaFact]
    public void EscapeOnTheRestoreFailurePanel_KeepsTheOpenProject_AndTheFilesTheCodeProjectWrote()
    {
        SwitchableNewProjectService service = Services.GetRequiredService<SwitchableNewProjectService>();
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        GumProjectSave openProject = ProjectManager.GumProjectSave.ShouldNotBeNull();
        string location = MakeLocation(tree);
        bool? result = null;
        service.Restore = (_, _, _) => Task.FromResult(new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = "offline" });
        try
        {
            RunDialog(tree, window =>
            {
                window.TypeName("Replacement");
                window.TypeLocation(location);
                window.SetChecked(NewProjectDialogText.CreateCodeProject, true);
                window.ClickOkWithoutWaiting();
                WaitFor(() => window.ViewModel.IsRestoreFailed, "the restore failure");
                window.Press(Key.Escape, PhysicalKey.Escape);
                result = window.Result;
            });
        }
        finally
        {
            service.Restore = null;
        }

        result.ShouldBe(false);
        ProjectManager.GumProjectSave.ShouldBeSameAs(openProject);
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Card" });
        File.Exists(Path.Combine(location, "Replacement", "Replacement.csproj")).ShouldBeTrue();
    }

    [AvaloniaFact]
    public void ACreatedProjectThatFailsToLoad_DoesNotEditTheProjectThatWasOpen()
    {
        SwitchableNewProjectService service = Services.GetRequiredService<SwitchableNewProjectService>();
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        tree.SaveAll();
        GumProjectSave openProject = ProjectManager.GumProjectSave.ShouldNotBeNull();
        ProjectFileSnapshot before = tree.SnapshotFiles();
        string location = tree.Project.ProjectFolder + "_new";
        Directory.CreateDirectory(location);
        string broken = Path.Combine(location, "Broken", "Broken.gumj");
        service.CreateFiles = (_, _) =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(broken)!);
            File.WriteAllText(broken, "this is not a gum project");
            return Task.FromResult(new NewProjectFilesResult { Success = true, GumProjectPath = broken });
        };
        tree.Dialogs.AnswerNextMessage(Gum.Services.Dialogs.MessageDialogResult.Affirmative);
        tree.Dialogs.AnswerNextMessage(Gum.Services.Dialogs.MessageDialogResult.Affirmative);
        try
        {
            RunDialog(tree, window =>
            {
                window.TypeName("Broken");
                window.TypeLocation(location);
                window.SetChecked(NewProjectDialogText.CreateCodeProject, true);
                window.SetChecked(NewProjectDialogText.RestorePackages, false);
                window.ClickOk();
            });
            tree.WaitUntil(() => tree.Dialogs.Messages.Count > 0 || tree.SelectedState.SelectedScreen?.Name == NewProjectLogic.StartingScreenName, AsyncWork, "the load to finish");
        }
        finally
        {
            service.CreateFiles = null;
        }
        Directory.Delete(location, recursive: true);

        openProject.Screens.ShouldNotContain(screen => screen.Name == NewProjectLogic.StartingScreenName, "the starting screen was not added to the project that was open");
        openProject.Components.Select(component => component.Name).ShouldBe(new[] { "Card" });
        tree.SnapshotFiles().ShouldMatch(before, "the project that was open keeps its files");
    }

    [AvaloniaFact]
    public void ASecondNewProjectDialog_StartsFromTheDefaults_NotFromTheFirstOnesChoices()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string location = MakeLocation(tree);
        RunDialog(tree, window =>
        {
            window.TypeName("First");
            window.TypeLocation(location);
            window.SetChecked(NewProjectDialogText.IncludeForms, false);
            window.ClickCancel();
        });

        RunDialog(tree, window =>
        {
            window.NameBox.Text.ShouldBe("MyGame");
            window.CheckBoxLabeled(NewProjectDialogText.IncludeForms).IsChecked.ShouldBe(true);
            window.CheckBoxLabeled(NewProjectDialogText.CreateCodeProject).IsChecked.ShouldBe(false);
            window.ClickCancel();
        });
    }
}
