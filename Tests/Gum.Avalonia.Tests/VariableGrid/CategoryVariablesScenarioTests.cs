using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Services.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>The Variables tab with a state category selected: every variable any of its states sets, each with a remove button.</summary>
public class CategoryVariablesScenarioTests
{
    private static (ComponentSave Slider, StateSaveCategory Category) BuildSlider(VariableGridHarness grid)
    {
        ComponentSave slider = grid.Project.AddComponent("Slider");
        grid.Project.AddInstance(slider, "Thumb", "Text");
        StateSaveCategory category = grid.Project.AddCategory(slider, "SliderCategory");
        StateSave enabled = grid.Project.AddState(slider, category, "Enabled");
        StateSave disabled = grid.Project.AddState(slider, category, "Disabled");
        StateSave focused = grid.Project.AddState(slider, category, "DisabledFocused");

        enabled.SetValue("Thumb.Red", 10, "int");
        disabled.SetValue("Thumb.Red", 20, "int");
        disabled.SetValue("Thumb.Green", 30, "int");
        disabled.VariableLists.Add(new VariableListSave<string> { Name = "Thumb.VariableReferences", Type = "string", ValueAsIList = new List<string> { "A = B.C" } });
        focused.SetValue("Thumb.Green", 40, "int");
        focused.SetValue("Thumb.Blue", 50, "int");
        return (slider, category);
    }

    private static void ClickTrash(VariableGridHarness grid, string name)
        => ((Gum.Avalonia.Plugins.VariableGrid.VariableRemoveButton)grid.Row(name).Displayer!).Remove();

    private static StateSaveCategory Category(VariableGridHarness grid)
        => grid.SelectedState.SelectedElement!.Categories.Single(item => item.Name == "SliderCategory");

    private static void SelectCategory(VariableGridHarness grid, ComponentSave slider, StateSaveCategory category)
    {
        grid.Select(slider);
        grid.SelectedState.SelectedStateCategorySave = category;
        grid.Settle();
    }

    [AvaloniaFact]
    public void SelectingACategory_ListsEachVariableAnyStateSets_OnceEach()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave slider, StateSaveCategory category) = BuildSlider(grid);

        SelectCategory(grid, slider, category);

        grid.ShownCategoryNames().ShouldBe(new[] { "SliderCategory Variables" });
        grid.ShownMemberNames().ShouldBe(new[] { "Thumb.Red", "Thumb.Green", "Thumb.Blue", "Thumb.VariableReferences" }, ignoreOrder: true);
        grid.ShownMemberNames().Count.ShouldBe(4);
    }

    [AvaloniaFact]
    public void SelectingACategory_WhenFirstStateSetsEverything_ListsThemInFirstStateOrder()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave slider = grid.Project.AddComponent("Slider");
        grid.Project.AddInstance(slider, "Thumb", "Text");
        StateSaveCategory category = grid.Project.AddCategory(slider, "SliderCategory");
        StateSave a = grid.Project.AddState(slider, category, "A");
        StateSave b = grid.Project.AddState(slider, category, "B");
        a.SetValue("Thumb.Red", 1, "int");
        a.SetValue("Thumb.Green", 2, "int");
        b.SetValue("Thumb.Green", 3, "int");
        b.SetValue("Thumb.Red", 4, "int");

        SelectCategory(grid, slider, category);

        grid.ShownMemberNames().ShouldBe(new[] { "Thumb.Red", "Thumb.Green" });
    }

    [AvaloniaFact]
    public void TrashCan_RemovesTheVariableFromEveryStateThatSetsIt_AsOneUndoStep()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave slider, StateSaveCategory category) = BuildSlider(grid);
        SelectCategory(grid, slider, category);

        grid.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        ClickTrash(grid, "Thumb.Green");
        grid.Settle();

        grid.Dialogs.Messages.Last().ShouldContain("DisabledFocused");
        grid.Dialogs.Messages.Last().ShouldNotContain("\nEnabled");

        StateSaveCategory afterRemove = Category(grid);
        afterRemove.States.SelectMany(state => state.Variables).Select(v => v.Name).ShouldNotContain("Thumb.Green");
        afterRemove.States.SelectMany(state => state.Variables).Count(v => v.Name == "Thumb.Red").ShouldBe(2);
        afterRemove.States.Single(s => s.Name == "DisabledFocused").Variables.Select(v => v.Name).ShouldBe(new[] { "Thumb.Blue" });
        grid.ShownMemberNames().ShouldNotContain("Thumb.Green");

        grid.Undo();
        Category(grid).States.SelectMany(state => state.Variables).Count(v => v.Name == "Thumb.Green").ShouldBe(2);
        grid.Redo();
        Category(grid).States.SelectMany(state => state.Variables).Count(v => v.Name == "Thumb.Green").ShouldBe(0);

        grid.Project.SaveAndReload();
        StateSaveCategory reloaded = grid.Project.Project.Components.Single(c => c.Name == "Slider").Categories.Single();
        reloaded.States.SelectMany(state => state.Variables).Select(v => v.Name).ShouldNotContain("Thumb.Green");
        reloaded.States.Single(s => s.Name == "Disabled").VariableLists.Single().Name.ShouldBe("Thumb.VariableReferences");
    }

    [AvaloniaFact]
    public void NoVariablesNotice_IsHidden_WhenOnlyALaterStateSetsVariables_AndShownWhenNoStateDoes()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave slider = grid.Project.AddComponent("Slider");
        grid.Project.AddInstance(slider, "Thumb", "Text");
        StateSaveCategory category = grid.Project.AddCategory(slider, "SliderCategory");
        grid.Project.AddState(slider, category, "Empty");
        StateSave later = grid.Project.AddState(slider, category, "Later");

        SelectCategory(grid, slider, category);
        grid.ViewModel.HasCategoryNotification.ShouldBeTrue();

        later.SetValue("Thumb.Red", 5, "int");
        grid.Select(slider);
        SelectCategory(grid, slider, category);

        grid.ViewModel.HasCategoryNotification.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void TrashCan_OnAListVariable_RemovesItFromTheStatesThatSetIt()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave slider, StateSaveCategory category) = BuildSlider(grid);
        SelectCategory(grid, slider, category);

        grid.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        ClickTrash(grid, "Thumb.VariableReferences");
        grid.Settle();

        Category(grid).States.SelectMany(state => state.VariableLists).ShouldBeEmpty();
        grid.Undo();
        Category(grid).States.SelectMany(state => state.VariableLists).Count().ShouldBe(1);
    }
}
