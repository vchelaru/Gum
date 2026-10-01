using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.Services.Dialogs;
using Shouldly;
using WpfDataUi.DataTypes;

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
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);

        CommitOnRow(grid, "Parent", "Inner");

        VariableGridHarness.StoredValue(panel, "Outer.Parent").ShouldBeNull();
        VariableGridHarness.StoredValue(panel, "Box.Parent").ShouldBe("Inner");
        tree.Dialogs.Messages.ShouldContain(message => message.Contains("circular reference"));
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
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);

        CommitOnRow(grid, "OuterParent", "Inner");

        VariableGridHarness.StoredValue(panel, "Outer.Parent").ShouldBeNull();
        tree.Dialogs.Messages.ShouldContain(message => message.Contains("circular reference"));
    }

    // Committing through the combo commits a rejected value a second time when the rejection
    // rebuilds the grid (#5580), so these commit through the row instead.
    private static void CommitOnRow(VariableGridHarness grid, string memberName, string value)
    {
        grid.Member(memberName).SetValue(value, SetPropertyCommitType.Full);
        grid.Settle();
    }
}
