using Gum.Managers;
using Avalonia.Controls;
using Avalonia.Styling;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The New Project dialog in each state of the approved mock, dark and light, at every zoom level
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter NewProjectDialogScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class NewProjectDialogScreenshotTests
{
    private static readonly string Location = OperatingSystem.IsWindows() ? @"C:\Users\You\Projects" : "/Users/You/Projects";

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Default(string theme) => Capture("default", theme, (_, _) => { });

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void FormsUnchecked(string theme) => Capture("forms-unchecked", theme, (viewModel, _) => viewModel.IsIncludeFormsControls = false);

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void CodeProjectChecked(string theme) => Capture("code-project", theme, (viewModel, _) =>
    {
        viewModel.IncludeCodeProject = true;
        viewModel.PlatformName = "kniskia";
    });

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void CodeProjectStride(string theme) => Capture("code-project-stride", theme, (viewModel, _) =>
    {
        viewModel.IncludeCodeProject = true;
        viewModel.PlatformName = "stride";
    });

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void CodeProjectSilkNet(string theme) => Capture("code-project-silknet", theme, (viewModel, _) =>
    {
        viewModel.IncludeCodeProject = true;
        viewModel.PlatformName = "silknet";
    });

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void CodeProjectMonoGame(string theme) => Capture("code-project-monogame", theme, (viewModel, _) =>
    {
        viewModel.IncludeCodeProject = true;
        viewModel.PlatformName = "monogame";
    });

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void CodeProjectMonoGameCommandLine(string theme) => Capture("code-project-monogame-command-line", theme, (viewModel, window) =>
    {
        viewModel.IncludeCodeProject = true;
        viewModel.PlatformName = "monogame";
        window.Find<NewProjectDialogView.Disclosure>(expander => expander.Name == NewProjectDialogView.CommandLineExpanderName).IsExpanded = true;
    });

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void WarningShowing(string theme) => Capture("warning", theme, (viewModel, _) => viewModel.IncludeCodeProject = true, existingProject: true);

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void TooltipOpen(string theme) => Capture("tooltip", theme, (viewModel, window) =>
    {
        viewModel.IncludeCodeProject = true;
        window.HoverForToolTip(window.Find<FluentIcons.Avalonia.FluentIcon>(icon => icon.Name == NewProjectDialogView.WarningInfoName), 2000);
    }, existingProject: true);

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void CopiedCheck(string theme) => Capture("copied", theme, (viewModel, window) =>
    {
        NewProjectDialogView.CopiedCheckDuration = TimeSpan.FromMinutes(5);
        window.Find<NewProjectDialogView.Disclosure>(d => d.Name == NewProjectDialogView.CommandLineExpanderName).IsExpanded = true;
        window.Click(window.Find<Button>(button => button.Name == NewProjectDialogView.CopyButtonName));
        for (int i = 0; i < 20; i++)
        {
            Thread.Sleep(10);
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        }
    });

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void NonEmptyFolder(string theme) => Capture("not-empty", theme, (viewModel, _) => viewModel.IncludeCodeProject = true,
        existingProject: true, targetNotEmpty: true);

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void FoldersExpanded(string theme) => Capture("folders-expanded", theme, (viewModel, window) =>
    {
        viewModel.IncludeCodeProject = true;
        viewModel.IsIncludeFormsControls = true;
        window.Find<NewProjectDialogView.Disclosure>(expander => expander.Name == NewProjectDialogView.FoldersExpanderName).IsExpanded = true;
    });

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void FoldersExpandedGumOnly(string theme) => Capture("folders-gum-only", theme, (_, window) =>
        window.Find<NewProjectDialogView.Disclosure>(expander => expander.Name == NewProjectDialogView.FoldersExpanderName).IsExpanded = true);

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void FoldersWithScrollBars(string theme) => Capture("folders-scrollbars", theme, (viewModel, window) =>
    {
        viewModel.IncludeCodeProject = true;
        viewModel.Location = Path.Combine(Location, "SomeVeryLongFolderName", "AnotherVeryLongFolderName", "YetAnotherLongFolderName", "Games");
        window.Find<NewProjectDialogView.Disclosure>(d => d.Name == NewProjectDialogView.FoldersExpanderName).IsExpanded = true;
        viewModel.SetFolderTreeExpanded(true);
    });

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void CommandLineExpanded(string theme) => Capture("command-line", theme, (viewModel, window) =>
    {
        viewModel.IncludeCodeProject = true;
        viewModel.IsIncludeDemoScreenGum = false;
        window.Find<NewProjectDialogView.Disclosure>(expander => expander.Name == NewProjectDialogView.CommandLineExpanderName).IsExpanded = true;
    });

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Busy(string theme) => Capture("busy", theme, (viewModel, _) =>
    {
        viewModel.IncludeCodeProject = true;
        viewModel.AffirmativeCommand.Execute(null);
    }, filesResult: null);

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void RestoreFailure(string theme) => Capture("restore-failure", theme, (viewModel, _) =>
    {
        viewModel.IncludeCodeProject = true;
        viewModel.AffirmativeCommand.Execute(null);
        for (int i = 0; i < 300 && !viewModel.IsRestoreFailed; i++)
        {
            Thread.Sleep(10);
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        }
    }, filesResult: new NewProjectFilesResult { Success = true, GumProjectPath = "g.gumj", CsprojPath = "g.csproj" });

    private static void Capture(
        string name,
        string theme,
        Action<NewProjectDialogViewModel, ScreenshotWindow> arrange,
        bool existingProject = false,
        bool targetNotEmpty = false,
        NewProjectFilesResult? filesResult = null)
    {
        PrScreenshot.Run(() =>
        {
            ToolStartup.EnsureInitialized();
            Mock<INewProjectService> service = new();
            service.Setup(x => x.IsSourceLinkedAvailable).Returns(true);
            service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation
            {
                Resolved = new ResolvedNewProject { Name = "MyGame", CsprojPath = "g.csproj" },
            });
            service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
                .Returns(filesResult == null ? new TaskCompletionSource<NewProjectFilesResult>().Task : Task.FromResult(filesResult));
            service.Setup(x => x.AddPackagesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AddPackagesResult { Status = RestoreStatus.Succeeded });
            service.Setup(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = "error NU1101: Unable to find package Gum.MonoGame. No packages exist with this id in source(s): nuget.org" });

            Mock<IExistingCodeProjectDetector> detector = new();
            if (existingProject)
            {
                detector.Setup(x => x.FindExistingCodeProject(It.IsAny<string>())).Returns(@"C:\Users\You\Projects\MyOtherGame.csproj");
            }

            detector.Setup(x => x.TargetExistsAndIsNotEmpty(It.IsAny<string>(), It.IsAny<string>())).Returns(targetNotEmpty);

            NewProjectDialogViewModel viewModel = new NewProjectDialogViewModel(
                TestAppBuilder.Services.GetRequiredService<ThemeSelectionViewModel>(),
                service.Object, detector.Object, new Mock<IDialogService>().Object, new Mock<INewProjectLocationSettings>().Object)
            {
                Location = Location,
            };
            using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, theme == "light" ? ThemeVariant.Light : ThemeVariant.Dark);
            arrange(viewModel, window);
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            PrScreenshot.SaveWindowAtZoomLevels(window.Window, $"new-project-{name}-{theme}");
        });
    }
}
