using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// A selected polygon on the editor canvas, for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter PolygonScaleHandlesScreenshotTests</c>). Uses only API that
/// main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class PolygonScaleHandlesScreenshotTests
{
    [SkippableFact]
    public void SelectedPolygon() => PrScreenshot.Run(() =>
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        using CanvasHarness canvas = new CanvasHarness();

        ComponentSave shapes = canvas.Project.AddComponent("Shapes");
        canvas.AddInstance(shapes, "Upright", "Polygon", x: 100, y: 100);
        InstanceSave tilted = canvas.AddInstance(shapes, "Tilted", "Polygon", x: 300, y: 120);
        shapes.DefaultState!.SetValue("Tilted.Rotation", 30f, "float");
        canvas.Wireframe.RefreshAll(forceLayout: true);
        canvas.Tree.Click(canvas.Tree.NodeFor(shapes.Instances[0]));
        canvas.Frame();
        PrScreenshot.SaveWindow(canvas.Input.Window, "polygon-selected");

        canvas.Tree.Click(canvas.Tree.NodeFor(tilted));
        canvas.Frame();
        PrScreenshot.SaveWindow(canvas.Input.Window, "polygon-selected-rotated");
    });
}
