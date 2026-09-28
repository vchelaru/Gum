using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
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
}
