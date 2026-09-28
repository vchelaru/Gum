using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The PNG File > Export > Export as Image writes, for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter ExportAsImageScreenshotTests</c>). The screenshot is the
/// exported file itself, not the window. Uses only API that main already has, so the same file
/// renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ExportAsImageScreenshotTests
{
    [SkippableFact]
    public void ExportedImage() => PrScreenshot.Run(() =>
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        using CanvasHarness canvas = new CanvasHarness();
        ComponentSave badge = canvas.Project.AddComponent("Badge");
        canvas.AddInstance(badge, "Fill", "Rectangle", x: 20, y: 20, width: 160, height: 80);
        badge.DefaultState!.SetValue("Fill.IsFilled", true, "bool");
        badge.DefaultState.SetValue("Fill.FillGreen", 60, "int");
        badge.DefaultState.SetValue("Fill.FillBlue", 60, "int");
        canvas.AddInstance(badge, "Frame", "Rectangle", x: 40, y: 40, width: 120, height: 40);
        canvas.Tree.Click(canvas.Tree.NodeFor(badge));
        canvas.Frame();

        canvas.Project.Dialogs.AnswerNextSaveFile(Path.Combine(PrScreenshot.OutputDirectory!, "exported-badge.png"));
        canvas.Tree.PickMainMenu("File", "Export", "Export as Image");
        canvas.Frame();
    });
}
