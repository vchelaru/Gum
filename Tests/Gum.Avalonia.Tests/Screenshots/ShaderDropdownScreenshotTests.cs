using Avalonia.Controls;
using Avalonia.VisualTree;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// A render-target Container's Source Shader File row with its list open, at every zoom level, for
/// a PR's images (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter ShaderDropdownScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ShaderDropdownScreenshotTests
{
    [SkippableFact]
    public void ListOpen_ProjectShaderAndBuiltIns() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave box = AddRenderTargetContainer(grid, withProjectShader: true);
        grid.Select(box);
        ShowShaderRow(grid);
        grid.Input.Window.Height = 360;
        grid.Combo("SourceShaderFile").IsDropDownOpen = true;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "shader-list-open");
    });

    [SkippableFact]
    public void ListOpen_NothingInTheProjectYet() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave box = AddRenderTargetContainer(grid, withProjectShader: false);
        grid.Select(box);
        ShowShaderRow(grid);
        grid.Input.Window.Height = 360;
        grid.Combo("SourceShaderFile").IsDropDownOpen = true;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "shader-list-open-empty-project");
    });

    private static InstanceSave AddRenderTargetContainer(VariableGridHarness grid, bool withProjectShader)
    {
        ComponentSave card = grid.Project.AddComponent("Card");
        InstanceSave box = grid.Project.AddInstance(card, "Box", "Container");
        if (withProjectShader)
        {
            Directory.CreateDirectory(Path.Combine(grid.Project.ProjectFolder, "Shaders"));
            File.WriteAllText(Path.Combine(grid.Project.ProjectFolder, "Shaders", "Glow.slang"), "");
            grid.Project.AddInstance(card, "Other", "Container");
            card.GetDefaultStateOrThrow().SetValue("Other.SourceShaderFile", "Shaders/Glow.slang", "string");
        }
        return box;
    }

    private static void ShowShaderRow(VariableGridHarness grid)
    {
        grid.Settle();
        grid.Row("IsRenderTarget").GetVisualDescendants().OfType<CheckBox>().First().IsChecked = true;
        grid.Settle();
        grid.Input.TypeInto(grid.View.FilterTextBox, "SourceShaderFile");
        grid.Settle();
    }
}
