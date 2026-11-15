using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// The note under "Set by" on a reference row (#7512) follows the source variable live while the instance
/// stays selected: no reselect is needed when the source goes through 0 and back.
/// </summary>
public class ReferenceFailureNoteLiveScenarioTests
{
    private static VariableGridHarness SelectBoxWidthDrivenByHeight(string height = "10")
    {
        VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave box = grid.Project.AddInstance(button, "Box", "Rectangle");
        grid.Select(box);
        grid.TypeAndEnter("Height", height);
        grid.TypeLinesAndApply("VariableReferences", "Width = 100 / Box.Height");
        return grid;
    }

    private static bool ShowsNote(VariableGridHarness grid)
    {
        string text = grid.Member("Width").DetailText ?? "";
        text.ShouldStartWith("Set by");
        return text.Contains('\n');
    }

    private static string? NoteFromLogic(VariableGridHarness grid)
    {
        ElementSave element = grid.SelectedState.SelectedElement!;
        return new VariableReferenceStatusLogic().GetNote("Width", grid.SelectedState.SelectedInstance, element, element.DefaultState);
    }

    [AvaloniaFact]
    public void TypingTheSourceThroughZeroAndBack_ShowsThenClearsTheNote_KeepingTheValue()
    {
        using VariableGridHarness grid = SelectBoxWidthDrivenByHeight();
        ShowsNote(grid).ShouldBeFalse();
        object? valueWhileValid = VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Box.Width");

        grid.TypeAndEnter("Height", "0");

        NoteFromLogic(grid).ShouldNotBeNull();
        ShowsNote(grid).ShouldBeTrue();
        grid.Member("Width").DetailText.ShouldEndWith(NoteFromLogic(grid)!);
        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Box.Width").ShouldBe(valueWhileValid);

        grid.TypeAndEnter("Height", "20");

        ShowsNote(grid).ShouldBeFalse();
    }

    [AvaloniaFact]
    public void UndoAndRedoOfTheSourceEdit_ShowAndClearTheNote()
    {
        using VariableGridHarness grid = SelectBoxWidthDrivenByHeight();
        grid.TypeAndEnter("Height", "0");
        ShowsNote(grid).ShouldBeTrue();

        grid.Undo();
        ShowsNote(grid).ShouldBeFalse();

        grid.Redo();
        ShowsNote(grid).ShouldBeTrue();
    }
}
