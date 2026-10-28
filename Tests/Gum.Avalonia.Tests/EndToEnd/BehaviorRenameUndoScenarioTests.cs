using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Logic;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Renaming a behavior is its own undo step (#7019): undo and redo put the behavior's name back,
/// with the elements' references to it, the project's reference, the behavior's file and its tree
/// node. Driven through the real tree, F2 and Ctrl+Z.
/// </summary>
[Trait("Category", "EndToEnd")]
public class BehaviorRenameUndoScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void RenamingABehavior_IsItsOwnUndoStep_AndUndoRestoresNameReferencesFileAndNode()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BehaviorSave clickable = AddBehavior(tree, "Clickable");
        ElementBehaviorReference reference = AddComponentReferencing(tree, clickable);
        Services.GetRequiredService<IAddInstanceLogic>().AddInstanceAtDestination(tree.Project.Standard("Text"), "Label");
        // F2 renames the selected instance first; select the behavior itself.
        tree.Click(tree.NodeFor(clickable));
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(clickable, "before the rename");
        tree.SelectedState.SelectedInstance.ShouldBeNull();

        Rename(tree, "Pressable");
        AssertNamed(tree, clickable, reference, "Pressable", "after the rename");
        clickable.RequiredInstances.Count.ShouldBe(1);

        tree.Undo();
        AssertNamed(tree, clickable, reference, "Clickable", "after undoing the rename");
        clickable.RequiredInstances.Count.ShouldBe(1, "the rename was undone, not the instance add");

        tree.Undo();
        clickable.RequiredInstances.Count.ShouldBe(0, "the second undo reaches the instance add");
        clickable.Name.ShouldBe("Clickable");

        tree.Redo();
        tree.Redo();
        AssertNamed(tree, clickable, reference, "Pressable", "after redoing the rename");
        clickable.RequiredInstances.Count.ShouldBe(1);

        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void RenamingABehaviorTwice_UndoesAndRedoesEachRenameInOrder()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BehaviorSave clickable = AddBehavior(tree, "Clickable");
        ElementBehaviorReference reference = AddComponentReferencing(tree, clickable);

        Rename(tree, "Pressable");
        Rename(tree, "Tappable");
        AssertNamed(tree, clickable, reference, "Tappable", "after both renames");

        tree.Undo();
        AssertNamed(tree, clickable, reference, "Pressable", "after the first undo");
        tree.Undo();
        AssertNamed(tree, clickable, reference, "Clickable", "after the second undo");
        tree.Redo();
        AssertNamed(tree, clickable, reference, "Pressable", "after the first redo");
        tree.Redo();
        AssertNamed(tree, clickable, reference, "Tappable", "after the second redo");

        tree.AssertOracles();
    }

    private static void AssertNamed(ProjectTreeHarness tree, BehaviorSave behavior, ElementBehaviorReference reference, string name, string when)
    {
        behavior.Name.ShouldBe(name, when);
        reference.BehaviorName.ShouldBe(name, $"{when}: the component's reference");
        tree.Project.Project.BehaviorReferences.Select(item => item.Name).ShouldBe(new[] { name }, $"{when}: the project's reference");
        tree.Project.Project.Behaviors.Select(item => item.Name).ShouldBe(new[] { name }, $"{when}: the loaded behaviors");
        string behaviors = Path.Combine(tree.Project.ProjectFolder, "Behaviors");
        Directory.GetFiles(behaviors).Select(Path.GetFileName).ShouldBe(new[] { name + ".behx" }, $"{when}: the behavior files");
        tree.RootNode("Behaviors").Nodes.Select(node => node.Text).ShouldBe(new[] { name }, $"{when}: the tree");
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(behavior, $"{when}: the selection");
    }

    /// <summary>Adds a Button component (with the Label the behavior may require) that references <paramref name="behavior"/>, then selects the behavior again.</summary>
    private static ElementBehaviorReference AddComponentReferencing(ProjectTreeHarness tree, BehaviorSave behavior)
    {
        ComponentSave button = tree.Project.AddComponent("Button");
        // What the scenario's behavior requires, so the saved project is valid at every step.
        tree.Project.AddInstance(button, "Label", "Text");
        ElementBehaviorReference reference = new ElementBehaviorReference { BehaviorName = behavior.Name };
        button.Behaviors.Add(reference);
        tree.SaveAll();
        tree.Click(tree.NodeFor(behavior));
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(behavior);
        return reference;
    }

    private static void Rename(ProjectTreeHarness tree, string newName)
    {
        tree.Dialogs.AnswerNextUserString(newName);
        tree.Press(Key.F2, PhysicalKey.F2);
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
}
