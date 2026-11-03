using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab's color picker flyout in the real main window, for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter ColorPickerScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ColorPickerScreenshotTests
{
    [SkippableFact]
    public void ColorPicker_Normal() => PrScreenshot.Run(() =>
        CaptureStates("picker", Color.FromRgb(61, 155, 214), null));

    [SkippableFact]
    public void ColorPicker_PurpleAndOrange() => PrScreenshot.Run(() =>
    {
        CaptureStates("purple", Color.FromRgb(163, 71, 217), null);
        CaptureStates("orange", Color.FromRgb(255, 136, 0), null);
    });

    [SkippableFact]
    public void ColorPicker_InvalidHexError() => PrScreenshot.Run(() =>
        CaptureStates("error", Color.FromRgb(61, 155, 214), picker => picker.HexTextBox.Text = "#12G4"));

    [SkippableFact]
    public void ColorPicker_AlphaIgnoredWarning() => PrScreenshot.Run(() =>
        CaptureStates("warning", Color.FromRgb(61, 155, 214), picker => picker.HexTextBox.Text = "#FF880080"));

    /// <summary>Runs <paramref name="prepare"/> after the picker opens, then saves both themes at every zoom level.</summary>
    private static void CaptureStates(string name, Color color, Action<CompactColorPicker>? prepare)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));

        // The real main window, so the panels, theme and flyout surface are the tool's own.
        tree.Input.Window.Content = null;
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.MinHeight = 0;
        window.Width = 1000;
        window.Height = 1100;
        window.Show();
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        Dispatcher.UIThread.RunJobs();
        ScreenshotWindow.Pump(window);
        ColorDisplay display = window.GetVisualDescendants().OfType<ColorDisplay>().First();

        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            window.RequestedThemeVariant = theme;
            Dispatcher.UIThread.RunJobs();
            Button swatchButton = display.GetVisualDescendants().OfType<Button>().First();
            swatchButton.BringIntoView();
            ScreenshotWindow.Pump(window);
            swatchButton.Flyout!.ShowAt(swatchButton);
            ScreenshotWindow.Pump(window);
            display.ColorPicker.OriginalColor = color;
            display.ColorPicker.Color = color;
            prepare?.Invoke(display.ColorPicker);
            PrScreenshot.SaveWindowAtZoomLevels(window, $"{name}-{theme.Key.ToString()!.ToLowerInvariant()}");
            swatchButton.Flyout!.Hide();
            ScreenshotWindow.Pump(window);
        }
    }
}
