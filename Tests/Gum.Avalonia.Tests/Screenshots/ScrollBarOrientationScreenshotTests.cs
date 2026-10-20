using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.Commands;
using Gum.DataTypes;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab for a ScrollBar instance of the Standard template, vertical and horizontal,
/// for a PR's before/after (<c>GUM_TEMPLATES_DIR</c> points the "before" run at a checkout of the
/// base's templates), plus the project folder so the canvas preview can be rendered with gumcli.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ScrollBarOrientationScreenshotTests
{
    [SkippableFact]
    public void ScrollBarInstance_VariablesTab() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave scrollBar = FormsTemplateInstanceVariablesScenarioTests.AddStandardInstance(grid, "ScrollBarInstance", "Controls/ScrollBar");
        grid.Select(scrollBar);

        SaveBoth(grid, "default");
        SaveProject(grid, "project-default");

        string orientationRow = grid.ShownMemberNames().Contains("ScrollBarInstance.Orientation")
            ? "Orientation"
            : "OrientationCategoryState";
        grid.ViewModel.VariableFilterText = "";
        grid.Input.Window.Height = 1500;
        grid.Settle();
        grid.PickComboItem(orientationRow, "Horizontal");
        grid.Settle();

        SaveBoth(grid, "horizontal");
        SaveProject(grid, "project-horizontal");
    });

    /// <summary>The whole tab, and the tab filtered to the orientation rows.</summary>
    private static void SaveBoth(VariableGridHarness grid, string name)
    {
        grid.ViewModel.VariableFilterText = "";
        grid.Input.Window.Height = 1500;
        grid.Settle();
        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, $"scrollbar-variables-{name}-full");

        grid.ViewModel.VariableFilterText = "Orientation";
        grid.Input.Window.Height = 200;
        grid.Settle();
        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, $"scrollbar-variables-{name}-filtered");
        grid.ViewModel.VariableFilterText = "";
        grid.Input.Window.Height = 1500;
        grid.Settle();
    }

    private static void SaveProject(VariableGridHarness grid, string name)
    {
        TestAppBuilder.Services.GetRequiredService<IFileCommands>().ForceSaveProject(forceSaveContainedElements: true);
        string target = Path.Combine(PrScreenshot.OutputDirectory!, name);
        CopyDirectory(Path.Combine(grid.Project.ProjectFolder, "Templates"), target);
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        }
        foreach (string directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }
}
