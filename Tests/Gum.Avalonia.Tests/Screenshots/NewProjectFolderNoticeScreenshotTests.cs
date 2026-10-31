using Gum.Managers;
using Avalonia.Styling;
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
/// The line under Location that says where the project folder goes, in each state and at every zoom level
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter NewProjectFolderNoticeScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class NewProjectFolderNoticeScreenshotTests
{
    private static readonly string Parent = OperatingSystem.IsWindows() ? @"C:\Users\You\Documents\GumProjects" : "/Users/You/Documents/GumProjects";

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void NewFolder(string theme) => Capture("new-folder", theme, viewModel => viewModel.Location = Parent, folderExists: false);

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void ExistingFolder(string theme) => Capture("existing-folder", theme, viewModel => viewModel.Location = Parent, folderExists: true);

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void LocationIsProjectFolder(string theme) => Capture("location-is-project-folder", theme,
        viewModel => viewModel.Location = Path.Combine(Parent, "CodeProj3"), folderExists: true);

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void LocationIsProjectFolderNotYetCreated(string theme) => Capture("location-is-project-folder-new", theme,
        viewModel => viewModel.Location = Path.Combine(Parent, "CodeProj3"), folderExists: false);

    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void LongPathWraps(string theme) => Capture("long-path", theme,
        viewModel => viewModel.Location = Path.Combine(Parent, "SomeVeryLongFolderName", "AnotherVeryLongFolderName", "YetAnotherLongFolderName", "TheLastFolderBeforeTheProject"),
        folderExists: false);

    /// <summary>The path beside the command line, the style reference, with the Folders summary above it.</summary>
    [SkippableTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void BesideTheCommandLine(string theme) => Capture("beside-command-line", theme,
        viewModel => viewModel.Location = Parent, folderExists: false,
        afterShow: window => window.Find<global::Gum.Avalonia.Dialogs.Views.NewProjectDialogView.Disclosure>(
            d => d.Name == global::Gum.Avalonia.Dialogs.Views.NewProjectDialogView.CommandLineExpanderName).IsExpanded = true);

    private static void Capture(string name, string theme, Action<NewProjectDialogViewModel> arrange, bool folderExists, Action<ScreenshotWindow>? afterShow = null)
    {
        PrScreenshot.Run(() =>
        {
            ToolStartup.EnsureInitialized();
            Mock<INewProjectService> service = new();
            service.Setup(x => x.IsSourceLinkedAvailable).Returns(true);
            service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation
            {
                Resolved = new ResolvedNewProject { Name = "CodeProj3", CsprojPath = "g.csproj" },
            });

            Mock<IExistingCodeProjectDetector> detector = new();
            ArrangeFolderExists(detector, folderExists);

            NewProjectDialogViewModel viewModel = new NewProjectDialogViewModel(
                TestAppBuilder.Services.GetRequiredService<ThemeSelectionViewModel>(),
                service.Object, detector.Object, new Mock<IDialogService>().Object, new Mock<INewProjectLocationSettings>().Object)
            {
                ProjectName = "CodeProj3",
            };
            arrange(viewModel);
            using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, theme == "light" ? ThemeVariant.Light : ThemeVariant.Dark);
            for (int i = 0; i < 300 && !viewModel.ChecksRefresh.IsCompleted; i++)
            {
                Thread.Sleep(10);
                global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            }
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            afterShow?.Invoke(window);
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            PrScreenshot.SaveWindowAtZoomLevels(window.Window, $"new-project-folder-{name}-{theme}");
        });
    }

    private static void ArrangeFolderExists(Mock<IExistingCodeProjectDetector> detector, bool folderExists)
    {
        detector.Setup(x => x.TargetExists(It.IsAny<string>(), It.IsAny<string>())).Returns(folderExists);
    }
}
