using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.Services.Dialogs;
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
        // The rejection message currently shows twice, so answer both.
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);

        grid.PickComboItem("Parent", "Inner");

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
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);

        grid.PickComboItem("OuterParent", "Inner");

        VariableGridHarness.StoredValue(panel, "Outer.Parent").ShouldBeNull();
        tree.Dialogs.Messages.ShouldContain(message => message.Contains("circular reference"));
    }
}
