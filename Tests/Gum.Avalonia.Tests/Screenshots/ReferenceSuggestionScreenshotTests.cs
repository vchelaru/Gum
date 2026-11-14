using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The suggestion list under the caret of the VariableReferences editor, in the real main window, in both
/// themes at every zoom level (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter ReferenceSuggestionScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ReferenceSuggestionScreenshotTests
{
    [SkippableFact]
    public void SuggestionList() => PrScreenshot.Run(() =>
    {
        using DiscardingScope scope = new DiscardingScope(new VariableGridHarness());
        VariableGridHarness grid = scope.Grid;
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Project.AddInstance(button, "Leader", "Container");
        grid.Project.AddInstance(button, "Legend", "Text");
        InstanceSave follower = grid.Project.AddInstance(button, "Follower", "Container");
        grid.Select(follower);

        // The real main window, so the panel, background and theme resources are the tool's own; the
        // harness window is emptied so the Variables tab can present its content there.
        grid.Input.Window.Content = null;
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.MinHeight = 0;
        window.Width = 1000;
        window.Height = 1300;
        window.Show();
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        Dispatcher.UIThread.RunJobs();

        foreach ((string state, string typed) in new[] { ("instances", "X = Le"), ("members", "X = Leader.Wi") })
        {
            foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
            {
                window.RequestedThemeVariant = theme;
                Dispatcher.UIThread.RunJobs();
                // The list is anchored where the caret was, so each zoom level scrolls to the editor and types again.
                IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
                double originalSize = scale.BaseFontSize;
                using IDisposable fontSizeFollower = AppWideWindowInput.FollowBaseFontSize(window, scale);
                try
                {
                    foreach (double size in PrScreenshot.ZoomLevels)
                    {
                        scale.BaseFontSize = size;
                        Dispatcher.UIThread.RunJobs();
                        StringListTextBoxDisplay editor = grid.Editor<StringListTextBoxDisplay>("VariableReferences");
                        editor.BringIntoView();
                        Dispatcher.UIThread.RunJobs();
                        editor.EditorTextBox.Text = string.Empty;
                        editor.EditorTextBox.Focus();
                        window.KeyTextInput(typed);
                        Dispatcher.UIThread.RunJobs();
                        PrScreenshot.SaveWindow(window, $"reference-suggestions-{state}-{theme.Key.ToString()!.ToLowerInvariant()}-zoom-{size}");
                    }
                }
                finally
                {
                    scale.BaseFontSize = originalSize;
                }
            }
        }
    });
}
