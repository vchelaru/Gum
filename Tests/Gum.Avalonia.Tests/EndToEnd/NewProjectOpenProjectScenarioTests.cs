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

                window.Click(window.Find<global::Avalonia.Controls.Button>(button => button.Name == DialogWindow.NegativeButtonName));
                WaitFor(window, () => !viewModel.IsBusy, "the work to stop");

                window.Result.ShouldBeNull("Cancel while busy stops the work and leaves the dialog open");
                viewModel.IsCodeProjectCreated.ShouldBeFalse();
                projectManager.GumProjectSave.ShouldBeSameAs(openProject);

                window.Click(window.Find<global::Avalonia.Controls.Button>(button => button.Name == DialogWindow.NegativeButtonName));
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

                window.Click(window.Find<global::Avalonia.Controls.Button>(button => button.Name == DialogWindow.NegativeButtonName));
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
