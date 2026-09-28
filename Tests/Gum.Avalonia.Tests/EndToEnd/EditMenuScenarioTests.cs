using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the main menu's Edit menu (inventory area EDIT): Undo and Redo, the Add
/// items and the Remove items, which follow the selection. Each picks the items as a user does
/// (<see cref="ProjectTreeHarness.PickMainMenu"/>), answers the dialogs, and ends with the shared oracles.
/// </summary>
[Trait("Category", "EndToEnd")]
public class EditMenuScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    [Trait("Feature", "EDIT-003")]
    [Trait("Feature", "EDIT-004")]
    [Trait("Feature", "EDIT-005")]
    [Trait("Feature", "EDIT-006")]
    [Trait("Feature", "EDIT-007")]
    public void EditMenuAdd_AddsAScreenComponentInstanceAndState_AndUndoAndRedoFollowTheHistory()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();

        tree.Dialogs.AnswerNext<AddScreenDialogViewModel>(dialog => { dialog.Value = "Title"; return true; });
        tree.PickMainMenu("Edit", "Add", "Screen");
        ScreenSave title = tree.Project.Project.Screens.ShouldHaveSingleItem();
        title.Name.ShouldBe("Title");
        tree.SelectedState.SelectedScreen.ShouldBeSameAs(title);
        tree.ChildTexts(tree.RootNode("Screens")).ShouldBe(new[] { "Title" });

        tree.Dialogs.AnswerNext<AddComponentDialogViewModel>(dialog => { dialog.Value = "Card"; return true; });
        tree.PickMainMenu("Edit", "Add", "Component");
        ComponentSave card = tree.Project.Project.Components.ShouldHaveSingleItem();
        card.Name.ShouldBe("Card");
        tree.SelectedState.SelectedComponent.ShouldBeSameAs(card);
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Card.gucx")).ShouldBeTrue();

        // A new element has no history yet.
        ProjectFileSnapshot start = tree.SnapshotFiles();
        Item("Undo").IsEnabled.ShouldBeFalse();
        Item("Redo").IsEnabled.ShouldBeFalse();

        tree.Dialogs.AnswerNext<AddInstanceDialogViewModel>(dialog => { dialog.TypeToCreate = "Sprite"; dialog.Value = "Icon"; return true; });
        tree.PickMainMenu("Edit", "Add", "Instance");
        InstanceSave icon = card.Instances.ShouldHaveSingleItem();
        icon.Name.ShouldBe("Icon");
        icon.BaseType.ShouldBe("Sprite");
        tree.ChildTexts(tree.NodeFor(card)).ShouldBe(new[] { "Icon" });
        Item("Undo").IsEnabled.ShouldBeTrue();
        Item("Redo").IsEnabled.ShouldBeFalse();

        StateSaveCategory size = tree.Project.AddCategory(card, "Size");
        tree.Click(tree.NodeFor(card));
        tree.States.Click(tree.States.ItemFor("Size"));
        tree.Dialogs.AnswerNext<AddStateDialogViewModel>(dialog => { dialog.Value = "Big"; return true; });
        tree.PickMainMenu("Edit", "Add", "State");
        size.States.Select(state => state.Name).ShouldBe(new[] { "Big" });
        tree.States.Shown().ShouldContain("Size: Big");

        // Edit > Undo takes back the state, the category and the instance; Undo then greys out and Redo lights up.
        tree.PickMainMenu("Edit", "Undo");
        tree.PickMainMenu("Edit", "Undo");
        tree.PickMainMenu("Edit", "Undo");
        card.Instances.ShouldBeEmpty();
        card.Categories.ShouldBeEmpty();
        tree.ChildTexts(tree.NodeFor(card)).ShouldBeEmpty();
        tree.SnapshotFiles().ShouldMatch(start, "undoing every add from the Edit menu should restore the files");
        Item("Undo").IsEnabled.ShouldBeFalse();
        Item("Redo").IsEnabled.ShouldBeTrue();

        tree.PickMainMenu("Edit", "Redo");
        tree.PickMainMenu("Edit", "Redo");
        tree.PickMainMenu("Edit", "Redo");
        card.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Icon" });
        card.Categories.ShouldHaveSingleItem().States.Select(state => state.Name).ShouldBe(new[] { "Big" });
        Item("Undo").IsEnabled.ShouldBeTrue();
        Item("Redo").IsEnabled.ShouldBeFalse();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "EDIT-008")]
    [Trait("Feature", "EDIT-009")]
    [Trait("Feature", "EDIT-010")]
    [Trait("Feature", "EDIT-011")]
    public void EditMenuRemove_FollowsTheSelection_AndRemovesTheSelectedStateCategoryElementAndBehaviorVariable()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        StateSaveCategory size = tree.Project.AddCategory(card, "Size");
        tree.Project.AddState(card, size, "Big");
        tree.Project.AddComponent("Panel");
        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        tree.Click(tree.RootNode("Behaviors").Nodes.Single());
        tree.Dialogs.AnswerNext<AddVariableViewModel>(dialog => { dialog.SelectedItem = "bool"; dialog.EnteredName = "IsEnabled"; return true; });
        tree.Grid.Input.Click(tree.Grid.View.AddVariableButton);
        tree.Grid.Settle();
        tree.Project.Project.Behaviors.Single().RequiredVariables.Variables.Select(variable => variable.Name).ShouldBe(new[] { "IsEnabled" });

        // A component with no state picked: only the element can be removed.
        tree.Click(tree.NodeFor(Component(tree, "Card")));
        RemoveHeaders().ShouldBe(new[] { "Card", "<no state selected>", "<no behavior variable selected>" });
        RemoveEnabled().ShouldBe(new[] { true, false, false });

        // Picking a state or a category in the States tab offers it for removal.
        ProjectFileSnapshot withState = tree.SnapshotFiles();
        tree.States.Click(tree.States.ItemFor("Size", "Big"));
        RemoveHeaders()[1].ShouldBe("State Big");
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.PickMainMenu("Edit", "Remove", "State Big");
        size.States.ShouldBeEmpty();
        tree.States.Shown().ShouldNotContain("Size: Big");
        tree.Undo();
        // Undo puts back copies of the element's categories.
        card.Categories.Single().States.Select(state => state.Name).ShouldBe(new[] { "Big" });
        tree.States.Shown().ShouldContain("Size: Big");
        tree.SnapshotFiles().ShouldMatch(withState, "undoing the state removal should restore the files");

        tree.States.Click(tree.States.ItemFor("Size"));
        RemoveHeaders()[1].ShouldBe("Category Size");
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.PickMainMenu("Edit", "Remove", "Category Size");
        card.Categories.ShouldBeEmpty();
        tree.Undo();
        card.Categories.Select(category => category.Name).ShouldBe(new[] { "Size" });
        tree.SnapshotFiles().ShouldMatch(withState, "undoing the category removal should restore the files");

        // The selected behavior variable.
        tree.Click(tree.RootNode("Behaviors").Nodes.Single());
        RemoveHeaders()[0].ShouldBe("<no element selected>");
        tree.Grid.Settle();
        ListBoxItem row = tree.Grid.BehaviorVariables.GetVisualDescendants().OfType<ListBoxItem>()
            .Single(item => (item.DataContext as VariableSave)?.Name == "IsEnabled");
        tree.Grid.Input.Click(row);
        string variableHeader = RemoveHeaders()[2];
        variableHeader.ShouldContain("IsEnabled");
        RemoveEnabled().ShouldBe(new[] { false, false, true });
        tree.PickMainMenu("Edit", "Remove", variableHeader);
        tree.Project.Project.Behaviors.Single().RequiredVariables.Variables.ShouldBeEmpty();
        File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "Behaviors", "Clickable.behx")).ShouldNotContain("IsEnabled");
        tree.Grid.Settle();
        RemoveHeaders()[2].ShouldBe("<no behavior variable selected>", "the removed variable is no longer offered");

        // The element itself.
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        RemoveHeaders().ShouldBe(new[] { "Panel", "<no state selected>", "<no behavior variable selected>" });
        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.PickMainMenu("Edit", "Remove", "Panel");
        tree.Project.Project.Components.Select(component => component.Name).ShouldBe(new[] { "Card" });
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Card" });
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Panel.gucx")).ShouldBeFalse();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "EDIT-004")]
    [Trait("Feature", "TREE-049")]
    public void EditMenuAddScreen_AfterReopeningTheProjectWithAnInstanceInAFolderSelected_AddsTheScreen()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Controls/Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Rectangle");
        tree.Click(tree.NodeFor(label));

        // Reopening rebuilds the tree; nothing may stay selected that the new tree does not show.
        tree.Project.SaveAndReload();
        tree.Input.Layout();
        foreach (Gum.Managers.GumTreeNode node in tree.View.Selection.SelectedNodes)
        {
            Gum.Managers.GumTreeNode root = node;
            while (root.Parent != null)
            {
                root = root.Parent;
            }
            tree.View.Nodes.ShouldContain(root, $"{node.Text} is selected but no longer in the tree");
        }

        tree.Dialogs.AnswerNext<AddScreenDialogViewModel>(dialog => { dialog.Value = "Title"; return true; });
        tree.PickMainMenu("Edit", "Add", "Screen");

        tree.Project.Project.Screens.Select(screen => screen.Name).ShouldBe(new[] { "Title" });
        tree.ChildTexts(tree.RootNode("Screens")).ShouldBe(new[] { "Title" });

        tree.AssertOracles();
    }

    private static MenuItemModel Item(params string[] path)
    {
        IEnumerable<MenuItemModel> items = Services.GetRequiredService<MenuModel>().TopLevelItems;
        MenuItemModel? item = null;
        foreach (string header in new[] { "Edit" }.Concat(path))
        {
            item = items.Single(candidate => !candidate.IsSeparator && candidate.Header == header);
            items = item.Items;
        }
        return item!;
    }

    private static List<string> RemoveHeaders() => Item("Remove").Items.Select(item => item.Header ?? "").ToList();

    private static List<bool> RemoveEnabled() => Item("Remove").Items.Select(item => item.IsEnabled).ToList();

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);
}
