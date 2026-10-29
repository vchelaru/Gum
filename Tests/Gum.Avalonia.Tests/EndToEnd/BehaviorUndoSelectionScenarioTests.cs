using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Logic;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Undo and redo of a change to a behavior leave that behavior selected, and a required instance
/// that was selected stays selected when it still exists, so the next Ctrl+Z still has a behavior
/// to undo (#7013). Driven through the real tree, hotkeys and add logic.
/// </summary>
[Trait("Category", "EndToEnd")]
public class BehaviorUndoSelectionScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void UndoingRequiredInstanceAdds_KeepsTheBehaviorSelected_ForEachUndoAndRedo()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BehaviorSave clickable = AddBehavior(tree, "Clickable");

        IAddInstanceLogic addInstanceLogic = Services.GetRequiredService<IAddInstanceLogic>();
        addInstanceLogic.AddInstanceAtDestination(tree.Project.Standard("Sprite"));
        addInstanceLogic.AddInstanceAtDestination(tree.Project.Standard("Text"));
        clickable.RequiredInstances.Count.ShouldBe(2);
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(clickable, "after the adds");

        tree.Undo();
        clickable.RequiredInstances.Count.ShouldBe(1, "first undo");
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(clickable, "after the first undo");

        tree.Undo();
        clickable.RequiredInstances.Count.ShouldBe(0, "second undo");
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(clickable, "after the second undo");

        tree.Redo();
        clickable.RequiredInstances.Count.ShouldBe(1, "first redo");
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(clickable, "after the first redo");

        tree.Redo();
        clickable.RequiredInstances.Count.ShouldBe(2, "second redo");
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(clickable, "after the second redo");
    }

    [AvaloniaFact]
    public void UndoingAnotherInstancesAdd_KeepsTheSelectedRequiredInstanceSelected_ByName()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BehaviorSave clickable = AddBehavior(tree, "Clickable");
        IAddInstanceLogic addInstanceLogic = Services.GetRequiredService<IAddInstanceLogic>();
        addInstanceLogic.AddInstanceAtDestination(tree.Project.Standard("Sprite"), "First");
        addInstanceLogic.AddInstanceAtDestination(tree.Project.Standard("Text"), "Second");
        tree.Click(NodeForRequired(tree, clickable, "First"));
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(clickable.RequiredInstances[0]);

        tree.Undo();

        clickable.RequiredInstances.Select(instance => instance.Name).ShouldBe(new[] { "First" });
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(clickable, "after the undo");
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(clickable.RequiredInstances[0], "the restored copy of First");

        tree.Redo();

        clickable.RequiredInstances.Select(instance => instance.Name).ShouldBe(new[] { "First", "Second" });
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(clickable, "after the redo");
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(clickable.RequiredInstances[0], "the restored copy of First");
    }

    [AvaloniaFact]
    public void UndoingARequiredInstanceRename_KeepsTheBehaviorSelected()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BehaviorSave clickable = AddBehavior(tree, "Clickable");
        IAddInstanceLogic addInstanceLogic = Services.GetRequiredService<IAddInstanceLogic>();
        addInstanceLogic.AddInstanceAtDestination(tree.Project.Standard("Text"), "Label");
        tree.Click(NodeForRequired(tree, clickable, "Label"));

        tree.Dialogs.AnswerNextUserString("Caption");
        tree.Press(Key.F2, PhysicalKey.F2);
        clickable.RequiredInstances.Select(instance => instance.Name).ShouldBe(new[] { "Caption" });

        tree.Undo();
        clickable.RequiredInstances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(clickable, "after the undo");

        tree.Redo();
        clickable.RequiredInstances.Select(instance => instance.Name).ShouldBe(new[] { "Caption" });
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(clickable, "after the redo");
    }

    [AvaloniaFact]
    public void UndoingARequiredInstanceDelete_KeepsTheBehaviorSelected()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BehaviorSave clickable = AddBehavior(tree, "Clickable");
        IAddInstanceLogic addInstanceLogic = Services.GetRequiredService<IAddInstanceLogic>();
        addInstanceLogic.AddInstanceAtDestination(tree.Project.Standard("Sprite"), "First");
        addInstanceLogic.AddInstanceAtDestination(tree.Project.Standard("Text"), "Second");
        tree.Click(NodeForRequired(tree, clickable, "Second"));

        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.Press(Key.Delete, PhysicalKey.Delete);
        clickable.RequiredInstances.Select(instance => instance.Name).ShouldBe(new[] { "First" });

        tree.Undo();
        clickable.RequiredInstances.Select(instance => instance.Name).ShouldBe(new[] { "First", "Second" });
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(clickable, "after the undo");

        tree.Redo();
        clickable.RequiredInstances.Select(instance => instance.Name).ShouldBe(new[] { "First" });
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(clickable, "after the redo");

        tree.Undo();
        tree.Undo();
        clickable.RequiredInstances.Select(instance => instance.Name).ShouldBe(new[] { "First" });
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(clickable, "after undoing past the delete");
    }

    private static BehaviorSave AddBehavior(ProjectTreeHarness tree, string name)
    {
        tree.Dialogs.AnswerNextUserString(name);
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        BehaviorSave behavior = tree.Project.Project.Behaviors.Single();
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(behavior);
        return behavior;
    }

    private static Gum.Managers.GumTreeNode NodeForRequired(ProjectTreeHarness tree, BehaviorSave behavior, string instanceName) =>
        tree.NodeFor(behavior).Nodes.Single(node => node.Text == instanceName);
}
