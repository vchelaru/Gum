using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Avalonia.Diagnostics;
using Gum.Avalonia.Shell;
using Gum.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the main window's own behavior (inventory area VIEW): its placement
/// across launches and the startup failure panel. The container's <see cref="MainWindow"/> is shown
/// and closed by <see cref="HeadCompositionTests"/>, so each scenario hosts the same wiring
/// (<see cref="WindowPlacementTracker"/>, <see cref="HeadStartupRun"/>,
/// <see cref="StartupFailurePanel"/>) in a window of its own.
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

                ExitApplication(firstLaunch);
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
    [Trait("Feature", "VIEW-016")]
    public void StartupThatThrows_WritesTheReasonToStderr_AndShowsItInPlaceOfThePanels()
    {
        Window window = new Window { Content = new TextBlock { Text = "panels" } };
        StringWriter stderr = new StringWriter();
        StartupFailureReporter failureReporter = new StartupFailureReporter(exception => window.Content = new StartupFailurePanel(exception), stderr);
        window.Show();
        try
        {
            Task<UnattendedStartupOutcome> startup = new HeadStartupRun(failureReporter).RunAsync(
                () => Task.FromException<UnattendedStartupOutcome>(new InvalidOperationException("The plugins folder is unreadable.")));
            Dispatcher.UIThread.RunJobs();

            startup.IsCompletedSuccessfully.ShouldBeTrue();
            startup.Result.ShouldBe(UnattendedStartupOutcome.Failed);
            stderr.ToString().ShouldStartWith("Startup failed: System.InvalidOperationException: The plugins folder is unreadable.");
            StartupFailurePanel panel = window.Content.ShouldBeOfType<StartupFailurePanel>();
            panel.Message.Text.ShouldNotBeNull();
            panel.Message.Text.ShouldStartWith("Startup failed:");
            panel.Message.Text.ShouldContain("The plugins folder is unreadable.");
        }
        finally
        {
            window.Close();
        }
    }

    // What the head's MainWindow does with its placement: tracked from construction, restored once open.
    private static Window OpenTrackedWindow(IHost host)
    {
        Window window = new Window();
        WindowPlacementTracker placement = new WindowPlacementTracker(
            window,
            host.Services.GetRequiredService<ShellViewModel>(),
            host.Services.GetRequiredService<IWritableOptions<LayoutSettings>>(),
            isSuspended: () => false);
        window.Opened += (_, _) => placement.Restore();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    // What App does when the desktop lifetime exits.
    private static void ExitApplication(IHost host)
    {
        List<Action> teardownActions = new List<Action>();
        host.Services.GetRequiredService<IMessenger>().Send(new ApplicationTeardownMessage(teardownActions));
        foreach (Action action in teardownActions)
        {
            action();
        }
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
