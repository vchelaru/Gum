using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Services;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// A component that exposes a Forms property (IsEnabled, declared by the Button behavior) of an inner
/// instance: renaming, deleting, copying, saving and undoing the pieces involved, driven through the tool
/// the way a user would, with the real services.
/// </summary>
[Trait("Category", "EndToEnd")]
public class ExposedFormsPropertyScenarioTests
{
    private const string Exposed = "RowEnabled";

    private static IServiceProvider Services => TestAppBuilder.Services;

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    private static ScreenSave Screen(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Screens.Single(screen => screen.Name == name);

    /// <summary>
    /// Button (with the Button behavior), Row (an inner Button instance named Inner whose IsEnabled is exposed as
    /// RowEnabled), and the Title screen with a Row instance named RowA that sets RowEnabled to false.
    /// </summary>
    private static void BuildProject(ProjectTreeHarness tree)
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        tree.Project.Project.Behaviors.Add(behavior);

        ComponentSave button = tree.Project.AddComponent("Button");
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        ComponentSave row = tree.Project.AddComponent("Row");
        InstanceSave inner = tree.Project.AddInstance(row, "Inner", "Button");
        row.DefaultState!.SetValue("Inner.IsEnabled", true, inner, "bool");
        tree.Click(tree.NodeFor(row));
        Services.GetRequiredService<IExposeVariableService>().ExposeVariable(inner, "IsEnabled", Exposed)
            .Succeeded.ShouldBeTrue();

        ScreenSave title = tree.Project.AddScreen("Title");
        InstanceSave rowA = tree.Project.AddInstance(title, "RowA", "Row");
        title.DefaultState!.SetValue("RowA." + Exposed, false, rowA, "bool");

        tree.SaveAll();
    }

    private static VariableSave ExposedVariable(ElementSave element) =>
        element.DefaultState!.Variables.Single(variable => variable.ExposedAsName == Exposed);

    [AvaloniaFact]
    public void DeletingTheInnerInstance_RemovesTheExposedVariable_AndTheValuesSetOnItElsewhere()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);
        tree.Click(tree.NodeFor(Component(tree, "Row").Instances.Single()));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.Press(Key.Delete, PhysicalKey.Delete);

        Component(tree, "Row").Instances.ShouldBeEmpty();
        Component(tree, "Row").DefaultState!.Variables.ShouldNotContain(variable => variable.ExposedAsName == Exposed);
        Screen(tree, "Title").DefaultState!.Variables.ShouldNotContain(variable => variable.Name == "RowA." + Exposed);

        tree.Undo();
        ExposedVariable(Component(tree, "Row")).Name.ShouldBe("Inner.IsEnabled");
        VariableGridHarness_Stored(Screen(tree, "Title"), "RowA." + Exposed).ShouldBe(false);
        tree.SnapshotFiles().ShouldMatch(start, "undoing the delete should restore the files");
    }

    private static VariableSave ExposedVariable_Named(ElementSave element, string exposedName) =>
        element.DefaultState!.Variables.Single(variable => variable.ExposedAsName == exposedName);

    private static object? VariableGridHarness_Stored(ElementSave element, string variableName) =>
        element.DefaultState!.Variables.FirstOrDefault(variable => variable.Name == variableName && variable.SetsValue)?.Value;

    [AvaloniaFact]
    public void DeletingTheInnerInstance_RemovesTheValuesSetByDerivedComponentsAndInStates_AndUndoRestoresThem()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);
        ComponentSave row = Component(tree, "Row");
        ComponentSave fancy = tree.Project.AddComponent("FancyRow", baseType: "Row");
        fancy.DefaultState!.Variables.Add(new VariableSave { Name = "Inner.IsEnabled", ExposedAsName = Exposed, Type = "bool", Value = false, SetsValue = true });
        ScreenSave title = Screen(tree, "Title");
        StateSaveCategory category = tree.Project.AddCategory(title, "Modes");
        StateSave state = tree.Project.AddState(title, category, "Off");
        state.Variables.Add(new VariableSave { Name = "RowA." + Exposed, Type = "bool", Value = true, SetsValue = true });
        tree.Click(tree.NodeFor(row.Instances.Single()));

        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.Press(Key.Delete, PhysicalKey.Delete);

        fancy.DefaultState.Variables.ShouldNotContain(variable => variable.ExposedAsName == Exposed);
        state.Variables.ShouldNotContain(variable => variable.Name == "RowA." + Exposed);

        tree.Undo();
        state.Variables.ShouldContain(variable => variable.Name == "RowA." + Exposed);
        VariableGridHarness_Stored(title, "RowA." + Exposed).ShouldBe(false);
    }

    [AvaloniaFact]
    public void RenamingTheInnerInstance_KeepsTheValuesSetElsewhereOnTheExposedVariable()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);
        InstanceSave inner = Component(tree, "Row").Instances.Single();
        tree.Click(tree.NodeFor(Component(tree, "Row")));
        inner.Name = "Renamed";
        Services.GetRequiredService<IRenameLogic>()
            .HandleRename(Component(tree, "Row"), inner, "Inner", NameChangeAction.Rename, askAboutRename: false);

        ExposedVariable(Component(tree, "Row")).SourceObject.ShouldBe("Renamed");
        VariableGridHarness_Stored(Screen(tree, "Title"), "RowA." + Exposed).ShouldBe(false);
    }

    [AvaloniaFact]
    public void DeletingTheInnerInstance_RemovesTheValuesSetElsewhereOnAnExposedPlainVariable()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave row = tree.Project.AddComponent("PlainRow");
        InstanceSave label = tree.Project.AddInstance(row, "Label", "Text");
        tree.Click(tree.NodeFor(row));
        Services.GetRequiredService<IExposeVariableService>().ExposeVariable(label, "Text", "Caption").Succeeded.ShouldBeTrue();
        ScreenSave title = tree.Project.AddScreen("PlainTitle");
        InstanceSave rowA = tree.Project.AddInstance(title, "RowA", "PlainRow");
        title.DefaultState!.SetValue("RowA.Caption", "Hi", rowA, "string");
        tree.Click(tree.NodeFor(label));

        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.Press(Key.Delete, PhysicalKey.Delete);

        title.DefaultState.Variables.ShouldNotContain(variable => variable.Name == "RowA.Caption");
    }
}
