using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Shouldly;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using GumFormsPlugin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The New Project dialog driven through its real view and window with a fake service: files and restore run
/// while the dialog stays open, and each failure offers its way forward.
/// </summary>
public class NewProjectDialogFlowTests
{
    private readonly Mock<INewProjectService> _service = new();

    private static NewProjectFilesResult FilesOk() => new NewProjectFilesResult
    {
        Success = true,
        GumProjectPath = @"C:\Users\You\Projects\MyGame\Content\GumProject\GumProject.gumj",
        CsprojPath = @"C:\Users\You\Projects\MyGame\MyGame.csproj",
    };

    private ScreenshotWindow Open(out NewProjectDialogViewModel viewModel)
    {
        _service.Setup(x => x.IsSourceLinkedAvailable).Returns(true);
        _service.Setup(x => x.GetPlan(It.IsAny<NewProjectRequest>())).Returns(new NewProjectPlan("MyGame", new[] { "A.gumj" }, Array.Empty<string>()));
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation
        {
            Resolved = new ResolvedNewProject { Name = "MyGame", CsprojPath = FilesOk().CsprojPath },
        });
        viewModel = new NewProjectDialogViewModel(
            TestAppBuilder.Services.GetRequiredService<ThemeSelectionViewModel>(),
            _service.Object, new Mock<IExistingCodeProjectDetector>().Object, new Mock<IDialogService>().Object)
        {
            Location = @"C:\Users\You\Projects",
            IncludeCodeProject = true,
        };
        return PrScreenshot.ShowDialog(viewModel);
    }

    private static Button ButtonNamed(ScreenshotWindow window, string name) => window.Find<Button>(button => button.Name == name);

    private static Button ButtonWithText(ScreenshotWindow window, string text) =>
        window.Find<Button>(button => button.Content as string == text && button.IsEffectivelyVisible);

    private static void WaitFor(ScreenshotWindow window, Func<bool> condition, string what)
    {
        for (int i = 0; i < 500 && !condition(); i++)
        {
            Thread.Sleep(10);
            ScreenshotWindow.Pump(window.Window);
        }
        condition().ShouldBeTrue($"timed out waiting for {what}");
    }

    private static DialogWindow Dialog(ScreenshotWindow window) => (DialogWindow)window.Window;

    private void SetUpRestore(params RestoreResult[] results)
    {
        Queue<RestoreResult> queue = new Queue<RestoreResult>(results);
        _service.Setup(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(queue.Dequeue()));
    }

    private static RestoreResult RestoreOk() => new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 };

    private static RestoreResult RestoreFailed() => new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = "NU1101: no such package" };

    [AvaloniaFact]
    public void CopyButton_PutsTheExactCommandOnTheClipboard_AndShowsACheck()
    {
        using ScreenshotWindow window = Open(out NewProjectDialogViewModel viewModel);
        window.Find<Gum.Avalonia.Dialogs.Views.NewProjectDialogView.Disclosure>(d => d.Name == Gum.Avalonia.Dialogs.Views.NewProjectDialogView.CommandLineExpanderName).IsExpanded = true;
        FluentIcons.Avalonia.FluentIcon icon = window.Find<Button>(button => button.Name == Gum.Avalonia.Dialogs.Views.NewProjectDialogView.CopyButtonName)
            .GetVisualDescendants().OfType<FluentIcons.Avalonia.FluentIcon>().First();
        icon.Icon.ShouldBe(FluentIcons.Common.Icon.Copy);

        window.Click(window.Find<Button>(button => button.Name == Gum.Avalonia.Dialogs.Views.NewProjectDialogView.CopyButtonName));
        WaitFor(window, () => icon.Icon == FluentIcons.Common.Icon.Checkmark, "the check mark");

        string? copied = window.Window.Clipboard!.TryGetTextAsync().GetAwaiter().GetResult();
        copied.ShouldBe(viewModel.CommandLine);
        copied.ShouldStartWith("gumcli new \"");
    }

    [AvaloniaFact]
    public void Tab_VisitsTheControlsInReadingOrder()
    {
        using ScreenshotWindow window = Open(out NewProjectDialogViewModel viewModel);
        viewModel.IsIncludeDemoScreenGum = true;
        TextBox name = window.Find<TextBox>(box => box.Text == "MyGame");
        name.Focus();
        ScreenshotWindow.Pump(window.Window);

        List<string> visited = new List<string>();
        for (int i = 0; i < 40; i++)
        {
            global::Avalonia.Input.IInputElement? focused = window.Window.FocusManager!.GetFocusedElement();
            string? description = Describe(focused);
            if (description != null)
            {
                visited.Add(description);
            }
            if (description == "Cancel")
            {
                break;
            }
            window.Window.KeyPress(global::Avalonia.Input.Key.Tab, global::Avalonia.Input.RawInputModifiers.None, global::Avalonia.Input.PhysicalKey.Tab, null);
            window.Window.KeyRelease(global::Avalonia.Input.Key.Tab, global::Avalonia.Input.RawInputModifiers.None, global::Avalonia.Input.PhysicalKey.Tab, null);
            ScreenshotWindow.Pump(window.Window);
        }

        visited.ShouldBe(new[]
        {
            "Project name", "File format", "Location", "Browse for a folder", "Include Forms controls (recommended)", "Theme",
            "Include demo screen", "Create C# project", "Platform", "Use local Gum source", "Restore NuGet packages",
            "Files", "Command line", "OK", "Cancel",
        });
    }

    // What a focused control is called, or null for one the reading order leaves out (the info icons, scroll parts).
    private static string? Describe(global::Avalonia.Input.IInputElement? focused)
    {
        string? automationName = focused is Control control ? global::Avalonia.Automation.AutomationProperties.GetName(control) : null;
        return focused switch
        {
            FluentIcons.Avalonia.FluentIcon => null,
            TextBox { Text: "MyGame" } => "Project name",
            TextBox => "Location",
            ComboBox combo when combo.ItemsSource is IEnumerable<string> formats && formats.Contains(NewProjectDialogViewModel.JsonFormat) => "File format",
            ComboBox combo when combo.ItemsSource is IEnumerable<NewProjectPlatformOption> => "Platform",
            ComboBox => "Theme",
            CheckBox { Content: StackPanel label } => label.GetVisualDescendants().OfType<TextBlock>().First().Text,
            CheckBox { Content: string text } => text,
            Button { Name: DialogWindow.AffirmativeButtonName } => "OK",
            Button { Name: DialogWindow.NegativeButtonName } => "Cancel",
            Control when !string.IsNullOrEmpty(automationName) => automationName,
            Button { Content: string text } => text,
            _ => null,
        };
    }

    [AvaloniaFact]
    public void InfoIconsCopyButtonAndBrowseHaveAccessibleNames()
    {
        using ScreenshotWindow window = Open(out _);

        List<string> names = window.FindAll<FluentIcons.Avalonia.FluentIcon>()
            .Select(icon => global::Avalonia.Automation.AutomationProperties.GetName(icon))
            .Where(name => name != null && name.StartsWith("About ")).ToList();

        names.ShouldBe(new[]
        {
            "About Project name", "About File format", "About Location", "About the existing code project warning",
            "About Include Forms controls (recommended)", "About Theme", "About Include demo screen", "About Create C# project",
            "About Platform",
        }, ignoreOrder: true);
        global::Avalonia.Automation.AutomationProperties.GetName(ButtonNamed(window, "PART_CopyCommandLine")).ShouldBe("Copy command line");
        global::Avalonia.Automation.AutomationProperties.GetName(window.Find<Button>(button => button.Content as string == "Browse…")).ShouldBe("Browse for a folder");
    }

    [AvaloniaFact]
    public void Ok_CreatesFilesAndRestores_ThenClosesAccepted()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(RestoreOk());
        using ScreenshotWindow window = Open(out NewProjectDialogViewModel viewModel);

        window.Click(ButtonNamed(window, DialogWindow.AffirmativeButtonName));
        WaitFor(window, () => Dialog(window).Result != null, "the dialog to close");

        Dialog(window).Result.ShouldBe(true);
        viewModel.CreatedGumProjectPath.ShouldBe(FilesOk().GumProjectPath);
        _service.Verify(x => x.RestoreAsync(FilesOk().CsprojPath!, It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [AvaloniaFact]
    public void FileCreationFailure_StaysOnTheDialog_AndShowsTheError()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NewProjectFilesResult { Success = false, ErrorMessage = "The folder is not writable." });
        using ScreenshotWindow window = Open(out NewProjectDialogViewModel viewModel);

        window.Click(ButtonNamed(window, DialogWindow.AffirmativeButtonName));
        WaitFor(window, () => viewModel.HasError, "the error");

        Dialog(window).Result.ShouldBeNull();
        window.Find<TextBlock>(text => text.Text == "The folder is not writable." && text.IsEffectivelyVisible);
        ButtonNamed(window, DialogWindow.AffirmativeButtonName).IsEffectivelyEnabled.ShouldBeTrue();
        _service.Verify(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [AvaloniaFact]
    public void RestoreFailure_Retry_RunsOnlyTheRestore_ThenCloses()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(RestoreFailed(), RestoreOk());
        using ScreenshotWindow window = Open(out NewProjectDialogViewModel viewModel);
        window.Click(ButtonNamed(window, DialogWindow.AffirmativeButtonName));
        WaitFor(window, () => viewModel.IsRestoreFailed, "the restore failure");
        window.Find<TextBlock>(text => text.Text == "NU1101: no such package" && text.IsEffectivelyVisible);

        window.Click(ButtonWithText(window, "Retry"));
        WaitFor(window, () => Dialog(window).Result != null, "the dialog to close");

        Dialog(window).Result.ShouldBe(true);
        _service.Verify(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()), Times.Once);
        _service.Verify(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [AvaloniaFact]
    public void RestoreFailure_ContinueWithoutRestore_ClosesAccepted_WithoutAnotherRestore()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(RestoreFailed());
        using ScreenshotWindow window = Open(out NewProjectDialogViewModel viewModel);
        window.Click(ButtonNamed(window, DialogWindow.AffirmativeButtonName));
        WaitFor(window, () => viewModel.IsRestoreFailed, "the restore failure");

        window.Click(ButtonWithText(window, "Continue without restore"));
        WaitFor(window, () => Dialog(window).Result != null, "the dialog to close");

        Dialog(window).Result.ShouldBe(true);
        viewModel.IsCodeProjectCreated.ShouldBeTrue();
        _service.Verify(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [AvaloniaFact]
    public void RestoreFailure_Back_ReturnsToTheForm()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(RestoreFailed());
        using ScreenshotWindow window = Open(out NewProjectDialogViewModel viewModel);
        window.Click(ButtonNamed(window, DialogWindow.AffirmativeButtonName));
        WaitFor(window, () => viewModel.IsRestoreFailed, "the restore failure");

        window.Click(ButtonWithText(window, "Back"));

        viewModel.IsRestoreFailed.ShouldBeFalse();
        Dialog(window).Result.ShouldBeNull();
        window.Find<TextBox>(box => box.Text == "MyGame").IsEffectivelyEnabled.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void Cancel_WhileBusy_StopsTheWork_AndLeavesTheDialogOpen()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .Returns<ResolvedNewProject, CancellationToken>(async (_, token) =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                }
                return new NewProjectFilesResult { Canceled = true };
            });
        using ScreenshotWindow window = Open(out NewProjectDialogViewModel viewModel);
        window.Click(ButtonNamed(window, DialogWindow.AffirmativeButtonName));
        WaitFor(window, () => viewModel.IsBusy, "the busy state");
        window.Find<TextBox>(box => box.Text == "MyGame").IsEffectivelyEnabled.ShouldBeFalse();

        window.Click(ButtonNamed(window, DialogWindow.NegativeButtonName));
        WaitFor(window, () => !viewModel.IsBusy, "the work to stop");

        Dialog(window).Result.ShouldBeNull();
        viewModel.IsCodeProjectCreated.ShouldBeFalse();
        window.Find<TextBox>(box => box.Text == "MyGame").IsEffectivelyEnabled.ShouldBeTrue();
    }
}
