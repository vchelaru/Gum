using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.DataTypes;
using Gum.Services.Dialogs;
using Gum.Undo;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// A commit whose rejection rebuilds the grid runs once: the editor torn down by the rebuild must not
/// commit the rejected value again when it loses focus (#5580).
/// </summary>
public class RejectedCommitScenarioTests
{
    [AvaloniaFact]
    public void PickingACircularParent_FromTheDropDown_IsRejectedOnce()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave panel = BuildOuterWithInnerChild(grid);
        grid.Select(panel.Instances.Single(instance => instance.Name == "Outer"));

        RunIsolated(grid, () => grid.PickComboItem("Parent", "Inner"));

        VariableGridHarness.StoredValue(panel, "Outer.Parent").ShouldBeNull();
        grid.Dialogs.Messages.Count(IsCircularMessage).ShouldBe(1);
    }

    [AvaloniaFact]
    public void TypingACircularParent_AndPressingEnter_IsRejectedOnce()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave panel = BuildOuterWithInnerChild(grid);
        grid.Select(panel.Instances.Single(instance => instance.Name == "Outer"));
        TextBox comboText = grid.Combo("Parent").GetVisualDescendants().OfType<TextBox>().First();

        RunIsolated(grid, () =>
        {
            grid.Input.TypeAndEnter(comboText, "Inner");
            grid.Settle();
        });

        VariableGridHarness.StoredValue(panel, "Outer.Parent").ShouldBeNull();
        grid.Dialogs.Messages.Count(IsCircularMessage).ShouldBe(1);
    }

    [AvaloniaFact]
    public void PickingACircularParent_OnAMultiSelection_IsRejectedOnce_AndLeavesUndoRecording()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave panel = BuildOuterWithInnerChild(grid);
        InstanceSave box = grid.Project.AddInstance(panel, "Box", "Container");
        grid.Select(panel.Instances.Single(instance => instance.Name == "Outer"), box);

        int heldLocks = RunIsolated(grid, () => grid.PickComboItem("Parent", "Inner"));

        heldLocks.ShouldBe(0);
        VariableGridHarness.StoredValue(panel, "Outer.Parent").ShouldBeNull();
        VariableGridHarness.StoredValue(panel, "Box.Parent").ShouldBe("Inner");
        grid.Dialogs.Messages.Count(IsCircularMessage).ShouldBe(1);
    }

    private static ComponentSave BuildOuterWithInnerChild(VariableGridHarness grid)
    {
        ComponentSave panel = grid.Project.AddComponent("Panel");
        grid.Project.AddInstance(panel, "Outer", "Container");
        InstanceSave inner = grid.Project.AddInstance(panel, "Inner", "Container");
        grid.Select(inner);
        grid.PickComboItem("Parent", "Outer");
        // A second message is answered too, so a regression fails on the count instead of
        // throwing mid-commit.
        grid.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        grid.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        return panel;
    }

    /// <summary>
    /// Runs <paramref name="commit"/> and returns how many undo locks it left held. The undo manager is
    /// shared by every test, so a leaked lock is released here, or later tests would record no undo.
    /// </summary>
    private static int RunIsolated(VariableGridHarness grid, Action commit)
    {
        UndoManager undoManager = (UndoManager)grid.UndoManager;
        try
        {
            commit();
            return undoManager.UndoLocks.Count;
        }
        finally
        {
            undoManager.UndoLocks.Clear();
        }
    }

    private static bool IsCircularMessage(string message) => message.Contains("circular reference");
}
