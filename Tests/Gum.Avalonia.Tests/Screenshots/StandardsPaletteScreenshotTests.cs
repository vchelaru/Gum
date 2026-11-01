using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Avalonia.Styling;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Project tab with its Standards palette and the palette's hint label, for a PR's
/// before/after table (<c>Tools/pr-screenshots.ps1 -Filter StandardsPaletteScreenshotTests</c>).
/// Uses only API that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class StandardsPaletteScreenshotTests
{
    [SkippableFact]
    public void StandardsPalette() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(button));
        PrScreenshot.SaveWindow(tree.Input.Window, "standards-palette");
    });

    [SkippableFact]
    public void StandardsPaletteAtZoomLevels() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("MainMenu");
        tree.Project.AddInstance(screen, "Background", "Sprite");
        ComponentSave button = tree.Project.AddComponent("Controls/Button");
        tree.Project.AddInstance(button, "Label", "Text");
        tree.TreeManager.RefreshUi();
        tree.NodeFor(screen).IsExpanded = true;
        tree.NodeFor(button).IsExpanded = true;
        tree.Click(tree.NodeFor(button));
        tree.Input.Window.RequestedThemeVariant = ThemeVariant.Dark;
        tree.Input.Window.Height = 420;
        PrScreenshot.SaveWindowAtZoomLevels(tree.Input.Window, "standards-palette-dark");
    });
}
