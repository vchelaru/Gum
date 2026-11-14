using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// A reference row whose right side cannot be evaluated right now (#7512), one contact sheet (all four zoom
/// levels) per theme (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter ReferenceFailureNoteScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ReferenceFailureNoteScreenshotTests
{
    [SkippableFact]
    public void DividesByZeroRow() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave leader = grid.Project.AddInstance(button, "Leader", "Text");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(leader);
        grid.TypeAndEnter("Width", "10");
        grid.Select(label);
        grid.TypeLinesAndApply("VariableReferences", "Width = 100 / Leader.Width");
        grid.Select(leader);
        grid.TypeAndEnter("Width", "0");
        grid.Select(label);
        grid.Input.TypeInto(grid.View.FilterTextBox, "Width");

        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            grid.Input.Window.RequestedThemeVariant = theme;
            PrScreenshot.SaveContactSheet(grid.Input.Window, $"reference-failure-note-{theme.ToString().ToLowerInvariant()}");
        }
    });

    [SkippableFact]
    public void ValidReferenceRowHasNoNote() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave leader = grid.Project.AddInstance(button, "Leader", "Text");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(leader);
        grid.TypeAndEnter("Width", "10");
        grid.Select(label);
        grid.TypeLinesAndApply("VariableReferences", "Width = 100 / Leader.Width");
        grid.Input.TypeInto(grid.View.FilterTextBox, "Width");

        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            grid.Input.Window.RequestedThemeVariant = theme;
            PrScreenshot.SaveContactSheet(grid.Input.Window, $"reference-valid-row-{theme.ToString().ToLowerInvariant()}");
        }
    });

    [SkippableFact]
    public void MissingInstanceRow() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave leader = grid.Project.AddInstance(button, "Leader", "Text");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(leader);
        grid.TypeAndEnter("Width", "10");
        grid.Select(label);
        grid.TypeLinesAndApply("VariableReferences", "Width = 100 / Leader.Width");
        button.Instances.Remove(leader);
        button.DefaultState!.Variables.RemoveAll(variable => variable.Name.StartsWith("Leader."));
        grid.Select(button);
        grid.Select(label);
        grid.Input.TypeInto(grid.View.FilterTextBox, "Width");

        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            grid.Input.Window.RequestedThemeVariant = theme;
            PrScreenshot.SaveContactSheet(grid.Input.Window, $"reference-missing-instance-note-{theme.ToString().ToLowerInvariant()}");
        }
    });
}