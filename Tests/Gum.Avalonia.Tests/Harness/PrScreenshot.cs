using Avalonia.Controls;
using Avalonia.Styling;
using Gum.Avalonia.Dialogs;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// Captures tool UI as PNGs for a pull request's before/after table (<c>Tools/pr-screenshots.ps1</c>).
/// A screenshot test is a <c>[SkippableFact]</c> tagged <c>[Trait("Category", PrScreenshot.Category)]</c>
/// whose body goes through <see cref="Run"/>: it skips unless <see cref="DirectoryVariable"/> names
/// an output folder, so normal and CI runs report it as skipped rather than passed.
/// </summary>
internal static class PrScreenshot
{
    public const string Category = "Screenshot";

    /// <summary>The environment variable naming the folder PNGs are written to.</summary>
    public const string DirectoryVariable = "GUM_SCREENSHOT_DIR";

    /// <summary>The output folder, or null when screenshot tests should skip.</summary>
    public static string? OutputDirectory =>
        Environment.GetEnvironmentVariable(DirectoryVariable) is { Length: > 0 } folder ? folder : null;

    /// <summary>
    /// Runs <paramref name="capture"/> on the Avalonia UI thread, or skips the test when
    /// <see cref="DirectoryVariable"/> is unset. An <c>[AvaloniaFact]</c> cannot skip, and an early
    /// return would report a pass.
    /// </summary>
    public static void Run(Action capture)
    {
        Skip.If(OutputDirectory == null, $"screenshot test; set {DirectoryVariable} to an output folder to run it (Tools/pr-screenshots.ps1 does)");
        DeviceTestThread.Run(capture);
    }

    /// <summary>
    /// Shows <paramref name="viewModel"/>'s registered view in the head's own <see cref="DialogWindow"/>,
    /// sized as the tool sizes it.
    /// </summary>
    public static ScreenshotWindow ShowDialog(DialogViewModel viewModel, ThemeVariant? theme = null)
    {
        Control view = TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        return new ScreenshotWindow(new DialogWindow(viewModel, view), theme);
    }

    /// <summary>
    /// Shows <paramref name="content"/> (a tab's view, a panel) in a plain window of the given size.
    /// A view the head builds once for the whole run (a singleton tab) belongs to a harness instead:
    /// host it through that harness and capture its window with <see cref="SaveWindow"/>.
    /// </summary>
    public static ScreenshotWindow Show(Control content, double width, double height, ThemeVariant? theme = null) =>
        new ScreenshotWindow(new Window { Content = content, Width = width, Height = height }, theme);

    /// <summary>
    /// Saves a window some other harness owns (<c>CanvasHarness.Input.Window</c>,
    /// <c>ProjectTreeHarness</c>'s driver window) as <paramref name="name"/>.png.
    /// </summary>
    public static string SaveWindow(Window window, string name)
    {
        ScreenshotWindow.Pump(window);
        return ScreenshotWindow.SaveFrame(window, name);
    }
}
