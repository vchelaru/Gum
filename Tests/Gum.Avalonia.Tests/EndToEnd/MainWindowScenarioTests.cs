using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Avalonia.Diagnostics;
using Gum.Avalonia.Shell;
using Gum.Managers;
using Gum.Settings;
using Gum.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the main window's own behavior (inventory area VIEW): its placement
/// across launches and the startup failure panel. The container's <see cref="MainWindow"/> is shown
/// and closed by <see cref="HeadCompositionTests"/>, so each scenario gives a window of its own the
/// objects the head wires the main window with (<see cref="WindowPlacementTracker"/>,
/// <see cref="StartupFailureReporter"/>, <see cref="HeadStartupRun"/>) and exits through
/// <see cref="ApplicationTeardown"/>, as the head does.
/// </summary>
[Trait("Category", "EndToEnd")]
public class MainWindowScenarioTests
{
    [AvaloniaFact]
    [Trait("Feature", "VIEW-009")]
    public void MovedAndResizedWindow_OpensAtTheSamePlace_OnTheNextLaunch()
    {
        string userDataFolder = Path.Combine(Path.GetTempPath(), "GumAvaloniaTests", "Placement", Guid.NewGuid().ToString("N"));
        string? originalOverride = FileManager.UserApplicationDataFolderOverride;
        FileManager.UserApplicationDataFolderOverride = userDataFolder;
        try
        {
            using (IHost firstLaunch = Program.CreateHostBuilder().Build())
            {
                Window window = OpenTrackedWindow(firstLaunch);
                window.Position = new PixelPoint(120, 90);
                window.Width = 900;
                window.Height = 650;
                Dispatcher.UIThread.RunJobs();

                ShellViewModel shell = firstLaunch.Services.GetRequiredService<ShellViewModel>();
                shell.Left.ShouldBe(120);
                shell.Top.ShouldBe(90);
                shell.Width.ShouldBe(900);
                shell.Height.ShouldBe(650);

                new ApplicationTeardown(firstLaunch.Services.GetRequiredService<IMessenger>()).Run();
                window.Close();
            }

            using (IHost secondLaunch = Program.CreateHostBuilder().Build())
            {
                secondLaunch.Services.GetRequiredService<IWritableOptions<LayoutSettings>>().CurrentValue.MainWindow
                    .ShouldBe(new WindowSettings(900, 650, Top: 90, Left: 120, IsMaximized: false));

                Window window = OpenTrackedWindow(secondLaunch);
                try
                {
                    window.Position.ShouldBe(new PixelPoint(120, 90));
                    window.Width.ShouldBe(900);
                    window.Height.ShouldBe(650);
                    window.WindowState.ShouldBe(WindowState.Normal);
                }
                finally
                {
                    window.Close();
                }
            }
        }
        finally
        {
            FileManager.UserApplicationDataFolderOverride = originalOverride;
            DeleteQuietly(userDataFolder);
        }
    }

    [AvaloniaFact]
    [Trait("Feature", "VIEW-009")]
    public void WindowShownInBackground_IgnoresTheSavedPlacement_AndRecordsNoMove()
    {
        // An unattended run parks the main window off-screen, which is not the user's placement.
        IWritableOptions<LayoutSettings> layoutSettings = TestAppBuilder.Services.GetRequiredService<IWritableOptions<LayoutSettings>>();
        ShellViewModel shell = TestAppBuilder.Services.GetRequiredService<ShellViewModel>();
        WindowSettings previousSaved = layoutSettings.CurrentValue.MainWindow;
        layoutSettings.CurrentValue.MainWindow = new WindowSettings(900, 650, Top: 90, Left: 120, IsMaximized: true);
        double leftBefore = shell.Left;
        Window window = new Window();
        _ = new WindowPlacementTracker(window, shell, layoutSettings, isSuspended: () => true);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.Position = new PixelPoint((int)leftBefore + 37, 50);
            Dispatcher.UIThread.RunJobs();

            window.WindowState.ShouldBe(WindowState.Normal);
            shell.Left.ShouldBe(leftBefore);
        }
        finally
        {
            window.Close();
            layoutSettings.CurrentValue.MainWindow = previousSaved;
        }
    }

    [AvaloniaFact]
    [Trait("Feature", "VIEW-016")]
    public void StartupThatThrows_WritesTheReasonToStderr_AndShowsItInPlaceOfThePanels()
    {
        Window window = new Window { Content = new TextBlock { Text = "panels" } };
        StringWriter stderr = new StringWriter();
        StartupFailureReporter failureReporter = new StartupFailureReporter(window, stderr);
        // The real startup sequence, over services whose first step (loading settings) throws.
        Mock<IProjectManager> projectManager = new Mock<IProjectManager>();
        projectManager.Setup(manager => manager.LoadSettings()).Throws(new InvalidOperationException("The settings file is unreadable."));
        using ServiceProvider services = new ServiceCollection().AddSingleton(projectManager.Object).BuildServiceProvider();
        GumStartupSequence sequence = new GumStartupSequence(services, Mock.Of<IHeadStartup>());
        window.Show();
        try
        {
            Task<UnattendedStartupOutcome> startup = new HeadStartupRun(failureReporter).RunAsync(async () =>
            {
                await sequence.RunAsync();
                return UnattendedStartupOutcome.Ready;
            });
            Dispatcher.UIThread.RunJobs();

            startup.IsCompletedSuccessfully.ShouldBeTrue();
            startup.Result.ShouldBe(UnattendedStartupOutcome.Failed);
            stderr.ToString().ShouldStartWith("Startup failed: System.InvalidOperationException: The settings file is unreadable.");
            StartupFailurePanel panel = window.Content.ShouldBeOfType<StartupFailurePanel>();
            panel.Message.Text.ShouldNotBeNull();
            panel.Message.Text.ShouldStartWith("Startup failed:");
            panel.Message.Text.ShouldContain("The settings file is unreadable.");
        }
        finally
        {
            window.Close();
        }
    }

    // A window tracked the way the head tracks its main window.
    private static Window OpenTrackedWindow(IHost host)
    {
        Window window = new Window();
        _ = new WindowPlacementTracker(
            window,
            host.Services.GetRequiredService<ShellViewModel>(),
            host.Services.GetRequiredService<IWritableOptions<LayoutSettings>>(),
            isSuspended: () => false);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void DeleteQuietly(string folder)
    {
        try
        {
            Directory.Delete(folder, recursive: true);
        }
        catch (IOException)
        {
            // The config file watcher can still hold the folder for a moment; it is a temp folder.
        }
    }
}
