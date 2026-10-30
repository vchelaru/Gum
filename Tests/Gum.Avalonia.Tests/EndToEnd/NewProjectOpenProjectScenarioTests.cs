using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ProjectServices;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// New Project with a C# project through the real dialog window: a Cancel during the busy state and a failed file
/// creation both leave the project that was open loaded and untouched.
/// </summary>
public class NewProjectOpenProjectScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    private static void WaitFor(DialogWindowDriver window, Func<bool> condition, string what)
    {
        for (int i = 0; i < 500 && !condition(); i++)
        {
            Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
            window.Layout();
        }
        condition().ShouldBeTrue($"timed out waiting for {what}");
    }

    private static void Configure(NewProjectDialogViewModel viewModel, string location)
    {
        viewModel.ProjectName = "Game";
        viewModel.Location = location;
        viewModel.IncludeCodeProject = true;
        viewModel.RunRestore = false;
    }

    [AvaloniaFact]
    public void CancelDuringTheBusyState_StopsTheCreation_AndLeavesTheOpenProjectLoaded()
    {
        SwitchableNewProjectService service = Services.GetRequiredService<SwitchableNewProjectService>();
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        GumProjectSave openProject = projectManager.GumProjectSave.ShouldNotBeNull();
        service.CreateFiles = async (_, token) =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException)
            {
            }
            return new NewProjectFilesResult { Canceled = true };
        };
        bool? result = null;
        try
        {
            tree.Dialogs.AnswerNextInWindow<NewProjectDialogViewModel>(window =>
            {
                NewProjectDialogViewModel viewModel = (NewProjectDialogViewModel)window.Window.DataContext!;
                Configure(viewModel, Path.Combine(tree.Project.ProjectFolder, "Out"));
                viewModel.AffirmativeCommand.Execute(null);
                WaitFor(window, () => viewModel.IsBusy, "the busy state");

                window.Press(global::Avalonia.Input.Key.Escape, global::Avalonia.Input.PhysicalKey.Escape);
                WaitFor(window, () => !viewModel.IsBusy, "the work to stop");

                window.Result.ShouldBeNull("Cancel while busy stops the work and leaves the dialog open");
                viewModel.IsCodeProjectCreated.ShouldBeFalse();
                projectManager.GumProjectSave.ShouldBeSameAs(openProject);

                window.Press(global::Avalonia.Input.Key.Escape, global::Avalonia.Input.PhysicalKey.Escape);
                result = window.Result;
            });

            tree.PickMainMenu("File", "New Project");
        }
        finally
        {
            service.CreateFiles = null;
        }

        result.ShouldBe(false);
        projectManager.GumProjectSave.ShouldBeSameAs(openProject);
        openProject.Components.Select(component => component.Name).ShouldBe(new[] { "Card" });
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Card" });
    }

    [AvaloniaTheory]
    [InlineData(".gumj")]
    [InlineData(".gumx")]
    public void GumOnly_IntoAFolderWithAProjectAlready_ShowsTheRefusal_AndOkIsOff(string existingExtension)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        GumProjectSave openProject = projectManager.GumProjectSave.ShouldNotBeNull();
        string location = Path.Combine(tree.Project.ProjectFolder, "Out");
        string existing = Path.Combine(location, "Existing", "Existing" + existingExtension);
        Directory.CreateDirectory(Path.GetDirectoryName(existing)!);
        File.WriteAllText(existing, "keep me");
        bool? result = null;

        tree.Dialogs.AnswerNextInWindow<NewProjectDialogViewModel>(window =>
        {
            NewProjectDialogViewModel viewModel = (NewProjectDialogViewModel)window.Window.DataContext!;
            viewModel.ProjectName = "Existing";
            viewModel.Location = location;
            WaitFor(window, () => viewModel.PlanRefresh.IsCompleted && viewModel.IsRefused, "the refusal");

            viewModel.RefusalText.ShouldStartWith("Project already exists");
            window.Find<global::Avalonia.Controls.TextBlock>(text => text.Text == viewModel.RefusalText && text.IsEffectivelyVisible);
            window.AffirmativeButton.IsEffectivelyEnabled.ShouldBeFalse("OK is off while the project exists");
            viewModel.AffirmativeCommand.Execute(null);
            window.Result.ShouldBeNull("OK does nothing while refused");

            window.Press(global::Avalonia.Input.Key.Escape, global::Avalonia.Input.PhysicalKey.Escape);
            result = window.Result;
        });
        tree.PickMainMenu("File", "New Project");

        result.ShouldBe(false);
        File.ReadAllText(existing).ShouldBe("keep me");
        projectManager.GumProjectSave.ShouldBeSameAs(openProject);
    }

    [AvaloniaFact]
    public void AFailedCreation_ShowsTheError_AndLeavesTheOpenProjectLoaded()
    {
        SwitchableNewProjectService service = Services.GetRequiredService<SwitchableNewProjectService>();
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        GumProjectSave openProject = projectManager.GumProjectSave.ShouldNotBeNull();
        service.CreateFiles = (_, _) => Task.FromResult(new NewProjectFilesResult { Success = false, ErrorMessage = "The disk is full." });
        bool? result = null;
        try
        {
            tree.Dialogs.AnswerNextInWindow<NewProjectDialogViewModel>(window =>
            {
                NewProjectDialogViewModel viewModel = (NewProjectDialogViewModel)window.Window.DataContext!;
                Configure(viewModel, Path.Combine(tree.Project.ProjectFolder, "Out"));
                viewModel.AffirmativeCommand.Execute(null);
                WaitFor(window, () => viewModel.HasError, "the error");

                viewModel.ErrorMessage.ShouldBe("The disk is full.");
                window.Result.ShouldBeNull("the dialog stays on the error");
                projectManager.GumProjectSave.ShouldBeSameAs(openProject);

                window.Press(global::Avalonia.Input.Key.Escape, global::Avalonia.Input.PhysicalKey.Escape);
                result = window.Result;
            });

            tree.PickMainMenu("File", "New Project");
        }
        finally
        {
            service.CreateFiles = null;
        }

        result.ShouldBe(false);
        projectManager.GumProjectSave.ShouldBeSameAs(openProject);
        openProject.Components.Select(component => component.Name).ShouldBe(new[] { "Card" });
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Card" });
    }
}
