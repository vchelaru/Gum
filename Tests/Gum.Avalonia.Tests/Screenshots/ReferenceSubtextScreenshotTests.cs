using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// A Variables tab row driven by a variable reference, for a PR's before/after images
/// (<c>Tools/pr-screenshots.ps1 -Filter ReferenceSubtextScreenshotTests -DryRun</c>).
/// Uses only API that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ReferenceSubtextScreenshotTests
{
    [SkippableFact]
    public void ReferenceDrivenRow() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave leader = grid.Project.AddInstance(button, "Leader", "Text");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(leader);
        grid.TypeAndEnter("Width", "120");
        grid.Select(label);
        grid.TypeLinesAndApply("VariableReferences", "Width = Leader.Width");
        grid.Input.TypeInto(grid.View.FilterTextBox, "Width");

        PrScreenshot.SaveWindow(grid.Input.Window, "reference-driven-row");
    });

    [SkippableFact]
    public void ReferenceDrivenRow_AtDifferentZoomLevels() => PrScreenshot.Run(() =>
    {
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave leader = grid.Project.AddInstance(button, "Leader", "Text");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(leader);
        grid.TypeAndEnter("Width", "120");
        grid.Select(label);
        grid.TypeLinesAndApply("VariableReferences", "Width = Leader.Width");
        grid.Input.TypeInto(grid.View.FilterTextBox, "Width");
        using IDisposable fontSizeFollower = AppWideWindowInput.FollowBaseFontSize(grid.Input.Window, scale);
        try
        {
            foreach (double size in new[] { 9d, 12d, 16d, 20d, 24d })
            {
                scale.BaseFontSize = size;
                grid.Input.Layout();
                PrScreenshot.SaveWindow(grid.Input.Window, $"zoom-{size}");
            }
        }
        finally
        {
            scale.BaseFontSize = originalSize;
        }
    });
}
