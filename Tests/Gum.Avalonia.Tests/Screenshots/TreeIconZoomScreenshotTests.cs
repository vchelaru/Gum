using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Wireframe;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Project tree's row icons at every zoom level, for a PR's before/after images
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter TreeIconZoomScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class TreeIconZoomScreenshotTests
{
    [SkippableFact]
    public void ProjectTree() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("MainMenu");
        tree.Project.AddInstance(screen, "Background", "Sprite");
        tree.Project.AddInstance(screen, "Title", "Text");
        ComponentSave button = tree.Project.AddComponent("Controls/Button");
        tree.Project.AddInstance(button, "Label", "Text");
        tree.Project.AddInstance(button, "Frame", "NineSlice");
        tree.Project.AddComponent("Panel");
        tree.TreeManager.RefreshUi();
        tree.NodeFor(screen).IsExpanded = true;
        tree.NodeFor(button).IsExpanded = true;
        tree.Click(tree.NodeFor(screen.Instances[0]));

        PrScreenshot.SaveWindowAtZoomLevels(tree.Input.Window, "project-tree");
    });

    [SkippableFact]
    public void SearchResults() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("MainMenu");
        tree.Project.AddInstance(screen, "Background", "Sprite");
        tree.Project.AddComponent("Controls/Button");
        tree.TreeManager.RefreshUi();
        tree.Search("a");

        PrScreenshot.SaveWindowAtZoomLevels(tree.Input.Window, "search-results");
    });
}
