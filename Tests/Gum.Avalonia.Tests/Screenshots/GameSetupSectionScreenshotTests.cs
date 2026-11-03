using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Code tab with a Screen selected, in the real main window, for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter GameSetupSectionScreenshotTests</c>). Uses only API that
/// main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class GameSetupSectionScreenshotTests
{
    [SkippableFact]
    public void CodeTabWithAScreen() => PrScreenshot.Run(() =>
    {
        using CodeTabHarness code = new CodeTabHarness(projectFolder => File.WriteAllText(
            Path.Combine(projectFolder, "ProjectCodeSettings.codsj"),
            """{ "SyntaxVersion": "3", "Version": 1, "AppendFolderToNamespace": true }"""));
        ScreenSave screen = code.Project.AddScreen("TitleScreen");
        code.Project.AddInstance(screen, "PlayButton", "Container");
        code.Tree.SaveAll();
        code.Select(screen);
        code.SetUpManualGeneration();

        // The real main window, so the panel, background and theme resources are the tool's own.
        code.Tree.Input.Window.Content = null;
        code.Input.Window.Content = null;
        if (code.View.Parent is global::Avalonia.Controls.Panel host)
        {
            host.Children.Remove(code.View);
        }
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.MinHeight = 0;
        window.Width = 1100;
        window.Height = 760;
        window.Show();
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        code.Tab.IsSelected = true;
        code.ViewModel.IsGameSetupExpanded = true;
        Dispatcher.UIThread.RunJobs();
        window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();
        PrScreenshot.SaveWindowAtZoomLevels(window, "code-tab-screen-dark");
    });
}
