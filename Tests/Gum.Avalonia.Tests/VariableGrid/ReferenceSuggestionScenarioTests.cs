using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>#8023: the suggestion list under the caret of the VariableReferences editor.</summary>
public class ReferenceSuggestionScenarioTests
{
    private static ReferenceSuggestionPopup PopupFor(VariableGridHarness grid)
    {
        AvaloniaVariableGridHead head = (AvaloniaVariableGridHead)TestAppBuilder.Services.GetRequiredService<IVariableGridHead>();
        StringListTextBoxDisplay editor = grid.Editor<StringListTextBoxDisplay>("VariableReferences");
        return head.GetSuggestionPopup(editor) ?? throw new InvalidOperationException("The reference editor has no suggestion list.");
    }

    private static void Press(VariableGridHarness grid, Key key, PhysicalKey physicalKey, RawInputModifiers modifiers = RawInputModifiers.None) =>
        grid.Input.Press(key, physicalKey, modifiers);

    private static (VariableGridHarness grid, ComponentSave button, InstanceSave follower) Arrange()
    {
        VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Project.AddInstance(button, "Leader", "Container");
        InstanceSave follower = grid.Project.AddInstance(button, "Follower", "Container");
        grid.Select(follower);
        return (grid, button, follower);
    }

    [AvaloniaFact]
    public void TypingARightSidePrefix_ListsMatchingInstances_WithoutLeavingTheEditor()
    {
        (VariableGridHarness grid, _, _) = Arrange();
        using DiscardingScope owned = new DiscardingScope(grid);

        grid.TypeLines("VariableReferences", "X = Lea");

        ReferenceSuggestionPopup popup = PopupFor(grid);
        popup.IsOpen.ShouldBeTrue();
        popup.Items.Select(item => item.Text).ShouldContain("Leader");
        grid.Editor<StringListTextBoxDisplay>("VariableReferences").EditorTextBox.IsFocused.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void TypingTheLeftSide_OffersNothing()
    {
        (VariableGridHarness grid, _, _) = Arrange();
        using DiscardingScope owned = new DiscardingScope(grid);

        grid.TypeLines("VariableReferences", "Wid");

        PopupFor(grid).IsOpen.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void AcceptingAnInstanceWithEnter_InsertsItWithADot_AndListsItsVariables_WithoutApplying()
    {
        (VariableGridHarness grid, ComponentSave button, _) = Arrange();
        using DiscardingScope owned = new DiscardingScope(grid);
        grid.TypeLines("VariableReferences", "X = Lea");

        Press(grid, Key.Enter, PhysicalKey.Enter);

        TextBox box = grid.Editor<StringListTextBoxDisplay>("VariableReferences").EditorTextBox;
        box.Text.ShouldBe("X = Leader.");
        ReferenceSuggestionPopup popup = PopupFor(grid);
        popup.IsOpen.ShouldBeTrue();
        popup.Items.Select(item => item.Text).ShouldContain("Leader.Width");
        // Focus never left the editor, so nothing was applied.
        grid.SelectedState.SelectedElement!.DefaultState.VariableLists.Any(list => list.Name == "Follower.VariableReferences").ShouldBeFalse();
    }

    [AvaloniaFact]
    public void ArrowDownThenTab_AcceptsTheSecondEntry()
    {
        (VariableGridHarness grid, _, _) = Arrange();
        using DiscardingScope owned = new DiscardingScope(grid);
        grid.TypeLines("VariableReferences", "X = Leader.X");
        ReferenceSuggestionPopup popup = PopupFor(grid);
        string second = popup.Items[1].Text;

        Press(grid, Key.Down, PhysicalKey.ArrowDown);
        popup.Selected!.Value.Text.ShouldBe(second);
        Press(grid, Key.Tab, PhysicalKey.Tab);

        grid.Editor<StringListTextBoxDisplay>("VariableReferences").EditorTextBox.Text.ShouldBe("X = " + second);
        popup.IsOpen.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void Escape_ClosesTheList_AndKeepsTheTypedText()
    {
        (VariableGridHarness grid, _, _) = Arrange();
        using DiscardingScope owned = new DiscardingScope(grid);
        grid.TypeLines("VariableReferences", "X = Lea");

        Press(grid, Key.Escape, PhysicalKey.Escape);

        PopupFor(grid).IsOpen.ShouldBeFalse();
        grid.Editor<StringListTextBoxDisplay>("VariableReferences").EditorTextBox.Text.ShouldBe("X = Lea");
    }

    [AvaloniaFact]
    public void CtrlEnter_WithTheListOpen_StillAppliesTheLine()
    {
        (VariableGridHarness grid, _, _) = Arrange();
        using DiscardingScope owned = new DiscardingScope(grid);
        grid.TypeLines("VariableReferences", "X = Leader.X");

        Press(grid, Key.Enter, PhysicalKey.Enter, RawInputModifiers.Control);

        grid.SelectedState.SelectedElement!.DefaultState.VariableLists
            .Any(list => list.Name == "Follower.VariableReferences").ShouldBeTrue();
    }

}

/// <summary>Empties the reference editor before the harness goes, so unapplied text never reaches focus-loss apply.</summary>
internal sealed class DiscardingScope : IDisposable
{
    private readonly VariableGridHarness _grid;

    public VariableGridHarness Grid => _grid;

    public DiscardingScope(VariableGridHarness grid) => _grid = grid;

    public void Dispose()
    {
        _grid.Editor<StringListTextBoxDisplay>("VariableReferences").EditorTextBox.Text = string.Empty;
        _grid.Dispose();
    }
}
