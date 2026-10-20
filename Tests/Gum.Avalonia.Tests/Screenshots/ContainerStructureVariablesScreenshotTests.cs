using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab for instances of Forms controls of the Standard template whose
/// container-structure variables (ClipsChildren, IsRenderTarget, ...) the template decides to
/// expose or hide, for a PR's before/after (<c>GUM_TEMPLATES_DIR</c> points the "before" run at a
/// checkout of the base's templates).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ContainerStructureVariablesScreenshotTests
{
    [SkippableFact]
    public void ItemsControlInstance_VariablesTab() => SaveVariablesTab("ItemsControlInstance", "Controls/ItemsControl", "items-control");

    [SkippableFact]
    public void WindowInstance_VariablesTab() => SaveVariablesTab("WindowInstance", "Controls/Window", "window");

    [SkippableFact]
    public void PanelInstance_VariablesTab() => SaveVariablesTab("PanelInstance", "Controls/Panel", "panel");

    private static void SaveVariablesTab(string instanceName, string type, string fileName) => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave instance = FormsTemplateInstanceVariablesScenarioTests.AddStandardInstance(grid, instanceName, type);
        grid.Select(instance);

        grid.ViewModel.VariableFilterText = "";
        grid.Input.Window.Height = 1500;
        grid.Settle();
        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, $"{fileName}-variables");
    });
}
