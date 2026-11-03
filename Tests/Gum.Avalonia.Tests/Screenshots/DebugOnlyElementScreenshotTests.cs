using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Services;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Code tab's per-element rows with the Debug Only checkbox (#7200) in the real main window, both themes, at every
/// zoom level. The "before" run has no such row and renders the unchecked rows only, so the same file renders both sides.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class DebugOnlyElementScreenshotTests
{
    [SkippableFact]
    public void DebugOnlyRow() => PrScreenshot.Run(() =>
    {
        using CodeTabHarness code = new CodeTabHarness(projectFolder => File.WriteAllText(
            Path.Combine(projectFolder, "ProjectCodeSettings.codsj"),
            """{ "SyntaxVersion": "3", "Version": 1, "AppendFolderToNamespace": true }"""));
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        code.Select(card);
        code.SetUpManualGeneration();

        // The real main window hosts the Code tab; the harness window gives it up.
        code.Input.Window.Content = null;
        code.Tree.Input.Window.Content = null;
        Dispatcher.UIThread.RunJobs();
        (code.View.Parent as Panel)?.Children.Remove(code.View);
        Dispatcher.UIThread.RunJobs();
        // A taller bottom-right dock (the splitter the user drags) so the settings rows fit at every zoom.
        double dockHeight = code.TabManager.BottomRightHeight;
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.MinHeight = 0;
        window.Width = 1600;
        window.Height = 1500;
        window.Show();
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        foreach (Grid grid in window.GetVisualDescendants().OfType<Grid>())
        {
            if (grid.RowDefinitions.Count == 3 && grid.RowDefinitions[2].Height.IsAbsolute && grid.RowDefinitions[2].Height.Value == dockHeight)
            {
                grid.RowDefinitions[2].Height = new GridLength(700);
            }
        }
        code.Tab.IsSelected = true;
        Dispatcher.UIThread.RunJobs();

        Control debugRow = code.Row("Debug Only");
        Save(window, debugRow, "unchecked");

        if (code.ShownMemberNames().Contains("Debug Only"))
        {
            code.Row("Debug Only").GetVisualDescendants().OfType<CheckBox>().First().IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Save(window, debugRow, "checked");
        }
    });

    private static void Save(Window window, Control row, string state)
    {
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(window, scale);
        try
        {
            foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
            {
                window.RequestedThemeVariant = theme;
                foreach (double size in PrScreenshot.ZoomLevels)
                {
                    scale.BaseFontSize = size;
                    PrScreenshot.SaveWindow(window, $"debug-only-{state}-{theme.Key.ToString()!.ToLowerInvariant()}-zoom-{size}");
                    // The Code tab's settings scroll; show the Debug Only row, then save again over the first frame.
                    row.BringIntoView();
                    row.GetVisualAncestors().OfType<ScrollViewer>().FirstOrDefault()?.ScrollToEnd();
                    PrScreenshot.SaveWindow(window, $"debug-only-{state}-{theme.Key.ToString()!.ToLowerInvariant()}-zoom-{size}");
                }
            }
        }
        finally
        {
            scale.BaseFontSize = originalSize;
        }
    }
}
