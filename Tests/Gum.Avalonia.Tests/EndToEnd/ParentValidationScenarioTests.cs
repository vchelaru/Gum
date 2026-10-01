using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.Services.Dialogs;
using Gum.Undo;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>Setting Parent rejects a circular parent, whichever row the edit came through.</summary>
[Trait("Category", "EndToEnd")]
public class ParentValidationScenarioTests
{
    [AvaloniaFact]
    public void PickingACircularParent_OnMultiSelectedInstances_IsRejectedOnlyWhereCircular()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave panel = tree.Project.AddComponent("Panel");
        InstanceSave outer = tree.Project.AddInstance(panel, "Outer", "Container");
        InstanceSave inner = tree.Project.AddInstance(panel, "Inner", "Container");
        InstanceSave box = tree.Project.AddInstance(panel, "Box", "Container");
        tree.Click(tree.NodeFor(inner));
        VariableGridHarness grid = tree.Grid;
        grid.PickComboItem("Parent", "Outer");
        tree.Click(tree.NodeFor(outer));
        tree.Click(tree.NodeFor(box), RawInputModifiers.Control);
        // A second message is answered too, so a double commit fails on the count instead of
        // throwing mid-commit.
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);

        int heldLocks = PickIsolated(tree, grid, "Parent", "Inner");

        heldLocks.ShouldBe(0);
        VariableGridHarness.StoredValue(panel, "Outer.Parent").ShouldBeNull();
        VariableGridHarness.StoredValue(panel, "Box.Parent").ShouldBe("Inner");
        tree.Dialogs.Messages.Count(message => message.Contains("circular reference")).ShouldBe(1);
    }

    [AvaloniaFact]
    public void SettingACircularParent_ThroughAnExposedParentOnTheComponent_IsRejected()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave panel = tree.Project.AddComponent("Panel");
        InstanceSave outer = tree.Project.AddInstance(panel, "Outer", "Container");
        InstanceSave inner = tree.Project.AddInstance(panel, "Inner", "Container");
        tree.Click(tree.NodeFor(inner));
        VariableGridHarness grid = tree.Grid;
        grid.PickComboItem("Parent", "Outer");
        tree.Click(tree.NodeFor(outer));
        tree.Dialogs.AnswerNextUserString("OuterParent");
        grid.PickRowMenuItem("Parent", "Expose Variable");
        tree.Click(tree.NodeFor(panel));
        // A second message is answered too, so a double commit fails on the count instead of
        // throwing mid-commit.
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);

        int heldLocks = PickIsolated(tree, grid, "OuterParent", "Inner");

        heldLocks.ShouldBe(0);
        VariableGridHarness.StoredValue(panel, "Outer.Parent").ShouldBeNull();
        tree.Dialogs.Messages.Count(message => message.Contains("circular reference")).ShouldBe(1);
    }

    /// <summary>
    /// Picks <paramref name="item"/> and returns how many undo locks the commit left held. The undo
    /// manager is shared by every test, so a leaked lock is released here, or later tests would
    /// record no undo.
    /// </summary>
    private static int PickIsolated(ProjectTreeHarness tree, VariableGridHarness grid, string memberName, string item)
    {
        UndoManager undoManager = (UndoManager)tree.UndoManager;
        try
        {
            grid.PickComboItem(memberName, item);
            return undoManager.UndoLocks.Count;
        }
        finally
        {
            undoManager.UndoLocks.Clear();
        }
    }
}
