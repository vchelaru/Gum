using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Plugins.Behaviors;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the States tab (inventory area STATE): the head's own tab driven with
/// clicks, its right-click menu, its "+" buttons and its keys, next to the Project tree that picks
/// the element and the Variables tab that edits a state. Each checks the tab and the saved file,
/// undoes back to identical files, redoes, and ends with the shared oracles.
/// </summary>
[Trait("Category", "EndToEnd")]
public class StateScenarioTests
{
    [AvaloniaFact]
    [Trait("Feature", "STATE-002")]
    [Trait("Feature", "STATE-003")]
    [Trait("Feature", "STATE-004")]
    [Trait("Feature", "STATE-006")]
    [Trait("Feature", "STATE-008")]
    [Trait("Feature", "STATE-010")]
    [Trait("Feature", "STATE-012")]
    [Trait("Feature", "STATE-013")]
    [Trait("Feature", "STATE-016")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void BuildingAndArrangingStates_FromTheStatesTab_SavesThem_AndUndoRedoFollowEachStep()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        StatesTabHarness states = tree.States;
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNext<AddCategoryDialogViewModel>(dialog => { dialog.Value = "Looks"; return true; });
        states.ClickNewCategory();
        tree.Dialogs.AnswerNext<AddStateDialogViewModel>(dialog => { dialog.Value = "Pressed"; return true; });
        states.ClickAddState(states.ItemFor("Looks"));
        tree.Dialogs.AnswerNext<AddStateDialogViewModel>(dialog => { dialog.Value = "Hover"; return true; });
        states.RightClick(states.ItemFor("Looks"));
        states.PickMenu("Add State");
        tree.Dialogs.AnswerNext<AddCategoryDialogViewModel>(dialog => { dialog.Value = "Size"; return true; });
        states.RightClick(states.ItemFor("Looks", "Hover"));
        states.PickMenu("Add Category");
        states.Shown().ShouldBe(new[] { "Looks: Pressed, Hover", "Size: " });

        states.Click(states.ItemFor("Looks", "Hover"));
        tree.SelectedState.SelectedStateSave?.Name.ShouldBe("Hover");
        grid.TypeAndEnter("Width", "200");
        states.RightClick(states.ItemFor("Looks", "Hover"));
        states.PickMenu("Duplicate [Hover]");
        states.Shown().ShouldBe(new[] { "Looks: Pressed, Hover, Hover1", "Size: " });
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width", State(tree, "Looks", "Hover1")).ShouldBe(200f);

        tree.Dialogs.AnswerNextUserString("Disabled");
        states.RightClick(states.ItemFor("Looks", "Hover1"));
        states.PickMenu("Rename [Hover1]");
        states.RightClick(states.ItemFor("Looks", "Disabled"));
        states.PickMenu("^ Move Up");
        states.Shown().ShouldBe(new[] { "Looks: Pressed, Disabled, Hover", "Size: " });
        states.Press(Key.Down, PhysicalKey.ArrowDown, RawInputModifiers.Alt);
        states.Shown().ShouldBe(new[] { "Looks: Pressed, Hover, Disabled", "Size: " });

        states.RightClick(states.ItemFor("Looks"));
        states.PickMenu("Sort Alphabetically");
        states.Shown().ShouldBe(new[] { "Looks: Disabled, Hover, Pressed", "Size: " });
        tree.Dialogs.AnswerNextUserString("Appearance");
        states.Press(Key.F2, PhysicalKey.F2);
        states.Shown().ShouldBe(new[] { "Appearance: Disabled, Hover, Pressed", "Size: " });

        ComponentSave saved = grid.ReadSaved(Component(tree, "Button"));
        saved.Categories.Select(category => category.Name).ShouldBe(new[] { "Appearance", "Size" });
        saved.Categories[0].States.Select(state => state.Name).ShouldBe(new[] { "Disabled", "Hover", "Pressed" });
        VariableGridHarness.StoredValue(saved, "Width", saved.Categories[0].States[0]).ShouldBe(200f);

        // One undo per step, newest first.
        tree.Undo();
        states.Shown().ShouldBe(new[] { "Looks: Disabled, Hover, Pressed", "Size: " });
        tree.Undo();
        states.Shown().ShouldBe(new[] { "Looks: Pressed, Hover, Disabled", "Size: " });
        tree.Undo();
        states.Shown().ShouldBe(new[] { "Looks: Pressed, Disabled, Hover", "Size: " });
        tree.Undo();
        states.Shown().ShouldBe(new[] { "Looks: Pressed, Hover, Disabled", "Size: " });
        tree.Undo();
        states.Shown().ShouldBe(new[] { "Looks: Pressed, Hover, Hover1", "Size: " });
        tree.Undo();
        states.Shown().ShouldBe(new[] { "Looks: Pressed, Hover", "Size: " });
        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width", State(tree, "Looks", "Hover")).ShouldBeNull();
        tree.Undo();
        states.Shown().ShouldBe(new[] { "Looks: Pressed, Hover" });
        tree.Undo();
        tree.Undo();
        tree.Undo();
        states.Shown().ShouldBeEmpty();
        tree.SnapshotFiles().ShouldMatch(start, "undoing every step should restore the files");

        for (int i = 0; i < 11; i++)
        {
            tree.Redo();
        }
        states.Shown().ShouldBe(new[] { "Appearance: Disabled, Hover, Pressed", "Size: " });
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width", State(tree, "Appearance", "Disabled")).ShouldBe(200f);

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "STATE-005")]
    [Trait("Feature", "STATE-007")]
    [Trait("Feature", "STATE-009")]
    [Trait("Feature", "STATE-011")]
    [Trait("Feature", "STATE-014")]
    [Trait("Feature", "STATE-015")]
    [Trait("Feature", "STATE-016")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void MovingCopyingAndDeletingStates_AcrossElements_AndUndoingEachElementRestoresTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        StateSaveCategory looks = tree.Project.AddCategory(button, "Looks");
        tree.Project.AddState(button, looks, "Pressed");
        tree.Project.AddState(button, looks, "Hover");
        StateSaveCategory size = tree.Project.AddCategory(button, "Size");
        tree.Project.AddState(button, size, "Big");
        ComponentSave panel = tree.Project.AddComponent("Panel");
        tree.Project.AddState(panel, tree.Project.AddCategory(panel, "Other"), "One");
        ScreenSave title = tree.Project.AddScreen("Title");
        InstanceSave okButton = tree.Project.AddInstance(title, "OkButton", "Button");
        tree.Click(tree.NodeFor(okButton));
        VariableGridHarness grid = tree.Grid;
        grid.PickComboItem("LooksState", "Pressed");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        grid.TypeAndEnter("Width", "120");
        StatesTabHarness states = tree.States;
        states.Click(states.ItemFor("Looks", "Pressed"));
        grid.TypeAndEnter("Width", "210");
        ProjectFileSnapshot start = tree.SnapshotFiles();

        states.RightClick(states.ItemFor("Looks", "Pressed"));
        states.PickMenu("Set [Pressed] variables to default");
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width", State(tree, "Looks", "Pressed")).ShouldBe(120f);
        grid.FieldText("Width").ShouldBe("120");

        states.RightClick(states.ItemFor("Looks", "Hover"));
        states.PickMenu("Move to category", "Size");
        states.Shown().ShouldBe(new[] { "Looks: Pressed", "Size: Big, Hover" });

        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        states.Click(states.ItemFor("Size", "Big"));
        states.Press(Key.Delete, PhysicalKey.Delete);
        states.Shown().ShouldBe(new[] { "Looks: Pressed", "Size: Hover" });

        states.RightClick(states.ItemFor("Looks"));
        states.PickMenu("Copy [Looks]");
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        states.RightClick(states.ItemFor("Other"));
        states.PickMenu("Paste Category");
        states.Shown().ShouldBe(new[] { "Other: One", "Looks: Pressed" });

        tree.Click(tree.NodeFor(Component(tree, "Button")));
        states.Click(states.ItemFor("Size"));
        states.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        states.Click(states.ItemFor("Other"));
        states.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);
        states.Shown().ShouldBe(new[] { "Other: One", "Looks: Pressed", "Size: Hover" });
        grid.ReadSaved(Component(tree, "Panel")).Categories.Select(category => category.Name).ShouldBe(new[] { "Other", "Looks", "Size" });

        tree.Click(tree.NodeFor(Component(tree, "Button")));
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        states.RightClick(states.ItemFor("Looks"));
        states.PickMenu("Delete [Looks]");
        tree.Dialogs.Messages.Last().ShouldContain("OkButton");
        states.Shown().ShouldBe(new[] { "Size: Hover" });
        VariableGridHarness.StoredValue(grid.ReadSaved(Screen(tree, "Title")), "OkButton.LooksState").ShouldBeNull();

        // Undo history is per element: Button's four steps, then Panel's two pastes.
        tree.Undo();
        states.Shown().ShouldBe(new[] { "Looks: Pressed", "Size: Hover" });
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LooksState").ShouldBe("Pressed");
        tree.Undo();
        states.Shown().ShouldBe(new[] { "Looks: Pressed", "Size: Big, Hover" });
        tree.Undo();
        states.Shown().ShouldBe(new[] { "Looks: Pressed, Hover", "Size: Big" });
        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width", State(tree, "Looks", "Pressed")).ShouldBe(210f);
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        tree.Undo();
        tree.Undo();
        states.Shown().ShouldBe(new[] { "Other: One" });
        tree.SnapshotFiles().ShouldMatch(start, "undoing every step in each element should restore the files");

        tree.Redo();
        tree.Redo();
        // The pastes hold the categories as they were copied, after the move and the delete.
        states.Shown().ShouldBe(new[] { "Other: One", "Looks: Pressed", "Size: Hover" });
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        for (int i = 0; i < 4; i++)
        {
            tree.Redo();
        }
        states.Shown().ShouldBe(new[] { "Size: Hover" });
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LooksState").ShouldBeNull();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "STATE-017")]
    [Trait("Feature", "EDIT-001")]
    public void TheMarkers_ShowWhichStatesSetTheSelectionAndWhichTheSelectedBehaviorRequires()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        StatesTabHarness states = tree.States;
        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        tree.Dialogs.AnswerNext<AddCategoryDialogViewModel>(dialog => { dialog.Value = "Looks"; return true; });
        states.ClickNewCategory();
        tree.Dialogs.AnswerNext<AddStateDialogViewModel>(dialog => { dialog.Value = "Pressed"; return true; });
        states.ClickAddState(states.ItemFor("Looks"));
        tree.Project.Project.Behaviors.Single().Categories.Single().States.Select(state => state.Name).ShouldBe(new[] { "Pressed" });

        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        InstanceSave icon = tree.Project.AddInstance(button, "Icon", "Sprite");
        StateSaveCategory size = tree.Project.AddCategory(button, "Size");
        tree.Project.AddState(button, size, "Big");
        tree.Project.AddState(button, size, "Small");
        tree.Click(tree.NodeFor(label));
        states.Click(states.ItemFor("Size", "Big"));
        VariableGridHarness grid = tree.Grid;
        grid.TypeAndEnter("X", "5");

        // Every state of a category sets the same variables, so Small sets Label.X too.
        states.ShowsEditedMarker(states.ItemFor("Size", "Big")).ShouldBeTrue();
        states.ShowsEditedMarker(states.ItemFor("Size", "Small")).ShouldBeTrue();
        tree.Click(tree.NodeFor(icon));
        states.ShowsEditedMarker(states.ItemFor("Size", "Big")).ShouldBeFalse();
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        states.ShowsEditedMarker(states.ItemFor("Size", "Big")).ShouldBeFalse();
        states.Click(states.ItemFor("Size", "Small"));
        grid.TypeAndEnter("Width", "40");
        states.ShowsEditedMarker(states.ItemFor("Size", "Small")).ShouldBeTrue();
        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width", State(tree, "Size", "Small")).ShouldBeNull();
        states.ShowsEditedMarker(states.ItemFor("Size", "Small")).ShouldBeFalse();

        // The Behaviors tab: Edit, check Clickable, OK, then select it in the added list.
        BehaviorsViewModel behaviors = BehaviorsTab();
        behaviors.EditCommand.Execute(null);
        behaviors.AllBehaviors.Single(item => item.Name == "Clickable").IsChecked = true;
        behaviors.ConfirmEditCommand.Execute(null);
        tree.ThrowIfCrashed();
        states.Shown().ShouldBe(new[] { "Size: Big, Small", "Looks: Pressed" });
        states.ShowsBehaviorMarker(states.ItemFor("Looks")).ShouldBeFalse();
        behaviors.SelectedBehavior = behaviors.AddedBehaviors.Single();
        states.Input.Layout();

        states.ShowsBehaviorMarker(states.ItemFor("Looks")).ShouldBeTrue();
        states.ShowsBehaviorMarker(states.ItemFor("Looks", "Pressed")).ShouldBeTrue();
        states.ShowsBehaviorMarker(states.ItemFor("Size")).ShouldBeFalse();
        states.ShowsBehaviorMarker(states.ItemFor("Size", "Big")).ShouldBeFalse();

        tree.AssertOracles();
    }

    /// <summary>The view model behind the head's Behaviors tab.</summary>
    private static BehaviorsViewModel BehaviorsTab() =>
        ((AvaloniaTabManager)TestAppBuilder.Services.GetRequiredService<ITabManager>()).AllTabs
            .Select(tab => tab.Content is global::Avalonia.Controls.Control view ? view.DataContext : tab.Content)
            .OfType<BehaviorsViewModel>().Single();

    private static ScreenSave Screen(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Screens.Single(screen => screen.Name == name);

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    private static StateSave State(ProjectTreeHarness tree, string category, string state) =>
        Component(tree, "Button").Categories.Single(item => item.Name == category).States.Single(item => item.Name == state);
}
