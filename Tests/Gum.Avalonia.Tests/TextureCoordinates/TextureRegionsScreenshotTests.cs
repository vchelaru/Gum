using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.TextureCoordinates;

/// <summary>
/// The Texture Coordinates tab inside the real main window and theme, for a PR's before/after
/// sheets (#7354). The empty case must stay pixel-identical to before the change.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class TextureRegionsScreenshotTests
{
    [SkippableFact]
    public void EmptyCase() => PrScreenshot.Run(() =>
    {
        Skip.IfNot(TextureCoordinateTabHarness.CanRun, TextureCoordinateTabHarness.SkipReason);
        using TextureCoordinateTabHarness tab = new TextureCoordinateTabHarness();
        SelectSheetSprite(tab);
        SaveInMainWindow(tab, "texture-regions-empty");
    });

    [SkippableFact]
    public void FewRegions() => PrScreenshot.Run(() =>
    {
        Skip.IfNot(TextureCoordinateTabHarness.CanRun, TextureCoordinateTabHarness.SkipReason);
        using TextureCoordinateTabHarness tab = new TextureCoordinateTabHarness();
        using IDisposable registration = TextureRegionScenarioTests.Register(
            tab, new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few)));
        SelectSheetSprite(tab);
        // The pointer over "Coin" (the overlay is told directly: the real window owns the pointer now).
        SaveInMainWindow(tab, "texture-regions-few", () =>
            tab.Plugin.DisplayController.RegionOverlay.Hovered = new TextureRegion("Coin", 128, 12, 24, 24));
    });

    [SkippableFact]
    public void SixtyRegions() => PrScreenshot.Run(() =>
    {
        Skip.IfNot(TextureCoordinateTabHarness.CanRun, TextureCoordinateTabHarness.SkipReason);
        using TextureCoordinateTabHarness tab = new TextureCoordinateTabHarness();
        using IDisposable registration = TextureRegionScenarioTests.Register(
            tab, new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Sixty())));
        SelectSheetSprite(tab, left: 64, top: 32, width: 32, height: 32);
        SaveInMainWindow(tab, "texture-regions-sixty", () =>
            tab.Plugin.DisplayController.RegionOverlay.Hovered = new TextureRegion("Tile_27", 224, 64, 32, 32));
    });

    internal static InstanceSave SelectSheetSprite(TextureCoordinateTabHarness tab, int left = 64, int top = 8, int width = 48, int height = 64)
    {
        ComponentSave button = tab.Project.AddComponent("Button");
        SampleSpriteSheet.Write(Path.Combine(tab.Project.ProjectFolder, "Sheet.png"));
        // Hero_Run is the current rectangle unless told otherwise.
        InstanceSave icon = tab.AddSprite(button, "Icon", "Sheet.png", left, top, width, height);
        tab.Select(icon);
        return icon;
    }

    /// <summary>
    /// Shows the tab in the real main window (both themes, four zoom levels) as
    /// <paramref name="name"/>-&lt;theme&gt;-zoom-&lt;size&gt;.png.
    /// </summary>
    internal static void SaveInMainWindow(TextureCoordinateTabHarness tab, string name, Action? beforeCapture = null)
    {
        tab.Input.Window.Content = null;
        tab.Editor.Input.Window.Content = null;
        tab.Tree.Input.Window.Content = null;
        tab.TabManager.BottomRightHeight = 560;
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.MinHeight = 0;
        window.Width = 1400;
        window.Height = 1000;
        window.Show();
        Dispatcher.UIThread.RunJobs();
        // Give the bottom-right dock (the Texture Coordinates tab) room to show the whole sheet.
        MainPanelView panel = window.GetVisualDescendants().OfType<MainPanelView>().First();
        Grid rightGrid = (Grid)typeof(MainPanelView).GetField("_rightGrid", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(panel)!;
        rightGrid.RowDefinitions[2].Height = new GridLength(560);
        Dispatcher.UIThread.RunJobs();
        tab.Tab.IsSelected = true;
        beforeCapture?.Invoke();
        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            window.RequestedThemeVariant = theme;
            tab.ViewModel.SelectedZoomLevel = 200;
            Dispatcher.UIThread.RunJobs();
            tab.Frame();
            tab.Camera.X = SampleSpriteSheet.Width / 2f - tab.Camera.ClientWidth / (2 * tab.Camera.Zoom);
            tab.Camera.Y = SampleSpriteSheet.Height / 2f - tab.Camera.ClientHeight / (2 * tab.Camera.Zoom);
            tab.Frame();
            PrScreenshot.SaveWindowAtZoomLevels(window, $"{name}-{theme.Key.ToString()!.ToLowerInvariant()}");
        }
    }
}
