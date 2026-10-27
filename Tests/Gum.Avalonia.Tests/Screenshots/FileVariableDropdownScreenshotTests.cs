using AvaloniaDataUi.Controls;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// A Sprite's SourceFile row, at every zoom level, for a PR's before/after images
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter FileVariableDropdownScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class FileVariableDropdownScreenshotTests
{
    [SkippableFact]
    public void SourceFileRow() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave icon = AddSpritesReferencingThreeTextures(grid);
        grid.Select(icon);
        grid.Input.TypeInto(grid.View.FilterTextBox, "SourceFile");
        grid.Input.Window.Height = 140;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "source-file-row");
    });

    [SkippableFact]
    public void SourceFileRowWithTheListOpen() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave icon = AddSpritesReferencingThreeTextures(grid);
        grid.Select(icon);
        grid.Input.TypeInto(grid.View.FilterTextBox, "SourceFile");
        grid.Input.Window.Height = 320;
        grid.Combo("SourceFile").IsDropDownOpen = true;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "source-file-list-open");
    });

    private static InstanceSave AddSpritesReferencingThreeTextures(VariableGridHarness grid)
    {
        ComponentSave button = grid.Project.AddComponent("Button");
        string source = Path.Combine(AppContext.BaseDirectory, "Content", "ExampleSpriteFrame.png");
        InstanceSave? first = null;
        foreach (string name in new[] { "Hero", "Banner", "Frame" })
        {
            File.Copy(source, Path.Combine(grid.Project.ProjectFolder, name + ".png"));
            InstanceSave sprite = grid.Project.AddInstance(button, name, "Sprite");
            first ??= sprite;
            grid.Select(sprite);
            grid.Dialogs.AnswerNextOpenFile(Path.Combine(grid.Project.ProjectFolder, name + ".png"));
            grid.Editor<FileSelectionDisplay>("SourceFile").PickFile();
            grid.Settle();
        }
        return first!;
    }
}
