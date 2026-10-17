using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab's position and size rows, at every zoom level, for a PR's before/after images
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter PairedRowsScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class PairedRowsScreenshotTests
{
    [SkippableFact]
    public void PositionAndSizeRows_SomeEditedAndOneSetByAReference() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave leader = grid.Project.AddInstance(button, "Leader", "Container");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Container");
        grid.Select(leader);
        grid.TypeAndEnter("Y", "30");
        grid.Select(label);
        grid.TypeAndEnter("X", "24");
        grid.TypeAndEnter("Width", "120");
        grid.TypeLinesAndApply("VariableReferences", "Y = Leader.Y");
        grid.Input.Window.Height = 560;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "position-and-size-rows");
    });

    [SkippableFact]
    public void TextureRectangleRows() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave icon = grid.Project.AddInstance(button, "Icon", "Sprite");
        grid.Select(icon);
        grid.PickComboItem("TextureAddress", "Custom");
        grid.TypeAndEnter("TextureLeft", "16");
        grid.TypeAndEnter("TextureHeight", "32");
        grid.Input.TypeInto(grid.View.FilterTextBox, "Texture");
        grid.Input.Window.Height = 700;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "texture-rectangle-rows");
    });

    [SkippableTheory]
    [InlineData(300)]
    [InlineData(220)]
    [InlineData(160)]
    public void TextureRectangleRows_WithThePanelDraggedNarrow(int width) => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave icon = grid.Project.AddInstance(button, "Icon", "Sprite");
        grid.Select(icon);
        grid.PickComboItem("TextureAddress", "Custom");
        grid.TypeAndEnter("TextureLeft", "16");
        grid.TypeAndEnter("TextureHeight", "32");
        grid.Input.TypeInto(grid.View.FilterTextBox, "Texture");
        grid.Input.Window.Width = width;
        grid.Input.Window.Height = 520;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, $"texture-rectangle-rows-width-{width}");
    });

    [SkippableFact]
    public void PositionAndSizeRows_NothingEdited() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Container");
        grid.Select(label);
        grid.Input.Window.Height = 560;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "position-and-size-rows-default");
    });
}
