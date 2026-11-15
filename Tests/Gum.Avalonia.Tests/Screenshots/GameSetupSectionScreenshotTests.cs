using Avalonia.Styling;
using CodeOutputPlugin.ViewModels;
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
/// (<c>Tools/pr-screenshots.ps1 -Filter GameSetupSectionScreenshotTests</c>). The default test
/// uses only API that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class GameSetupSectionScreenshotTests
{
    private const string DefaultSettings = """{ "SyntaxVersion": "3", "Version": 1, "AppendFolderToNamespace": true }""";

    private const string RaylibSettings =
        """{ "SyntaxVersion": "3", "Version": 1, "AppendFolderToNamespace": true, "CodeProjectRoot": "Code/", "OutputLibrary": 6, "ObjectInstantiationType": 1 }""";

    private static MainWindow ShowMainWindow(CodeTabHarness code, int height)
    {
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
        window.Height = height;
        window.Show();
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        code.Tab.IsSelected = true;
        window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static CodeTabHarness ScreenSelected(string settings, bool manualSetup)
    {
        CodeTabHarness code = new CodeTabHarness(projectFolder => File.WriteAllText(
            Path.Combine(projectFolder, "ProjectCodeSettings.codsj"), settings));
        ScreenSave screen = code.Project.AddScreen("TitleScreen");
        code.Project.AddInstance(screen, "PlayButton", "Container");
        code.Tree.SaveAll();
        code.Select(screen);
        if (manualSetup)
        {
            code.SetUpManualGeneration();
        }
        return code;
    }

    /// <summary>The Object tab, which is also what the "before" side shows.</summary>
    [SkippableFact]
    public void CodeTabWithAScreen() => PrScreenshot.Run(() =>
    {
        using CodeTabHarness code = ScreenSelected(DefaultSettings, manualSetup: true);
        MainWindow window = ShowMainWindow(code, 760);
        PrScreenshot.SaveWindowAtZoomLevels(window, "code-tab-screen-dark");
    });

    /// <summary>The State tab, unchanged by the Game tab.</summary>
    [SkippableFact]
    public void CodeTabStateTab() => PrScreenshot.Run(() =>
    {
        using CodeTabHarness code = ScreenSelected(DefaultSettings, manualSetup: true);
        MainWindow window = ShowMainWindow(code, 760);
        code.ViewModel.WhatToView = WhatToView.SelectedState;
        Dispatcher.UIThread.RunJobs();
        PrScreenshot.SaveWindowAtZoomLevels(window, "code-tab-state-dark");
    });

    /// <summary>The Game tab, in both themes.</summary>
    [SkippableFact]
    public void CodeTabGameTab() => PrScreenshot.Run(() =>
    {
        using CodeTabHarness code = ScreenSelected(DefaultSettings, manualSetup: true);
        MainWindow window = ShowMainWindow(code, 760);
        code.ViewModel.WhatToView = WhatToView.GameSetup;
        Dispatcher.UIThread.RunJobs();
        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            window.RequestedThemeVariant = theme;
            Dispatcher.UIThread.RunJobs();
            PrScreenshot.SaveWindowAtZoomLevels(window, $"code-tab-game-{theme.Key.ToString()!.ToLowerInvariant()}");
        }
    });

    /// <summary>A Raylib project: the Game tab is a note with no code.</summary>
    [SkippableFact]
    public void CodeTabForARaylibProject() => PrScreenshot.Run(() =>
    {
        using CodeTabHarness code = ScreenSelected(RaylibSettings, manualSetup: false);
        MainWindow window = ShowMainWindow(code, 760);
        code.ViewModel.WhatToView = WhatToView.GameSetup;
        Dispatcher.UIThread.RunJobs();
        PrScreenshot.SaveWindowAtZoomLevels(window, "code-tab-raylib-game-dark");
    });
}
