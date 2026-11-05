using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
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
    public void RenamingTheInnerInstance_KeepsTheFormsPropertyExposed_AndUndoRestoresIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);
        tree.Click(tree.NodeFor(Component(tree, "Row").Instances.Single()));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNextUserString("Core");
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);

        Component(tree, "Row").Instances.Single().Name.ShouldBe("Core");
        ExposedVariable(Component(tree, "Row")).Name.ShouldBe("Core.IsEnabled");
        VariableGridHarness_Stored(Screen(tree, "Title"), "RowA." + Exposed).ShouldBe(false);

        tree.Undo();
        Component(tree, "Row").Instances.Single().Name.ShouldBe("Inner");
        ExposedVariable(Component(tree, "Row")).Name.ShouldBe("Inner.IsEnabled");
        tree.SnapshotFiles().ShouldMatch(start, "undoing the rename should restore the files");
    }

    [AvaloniaFact]
    public void RenamingTheRowComponent_KeepsTheValueSetOnItsInstances()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);
        tree.Click(tree.NodeFor(Component(tree, "Row")));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "OptionsRow"; return true; });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);

        Screen(tree, "Title").Instances.Single().BaseType.ShouldBe("OptionsRow");
        VariableGridHarness_Stored(Screen(tree, "Title"), "RowA." + Exposed).ShouldBe(false);
        ExposedVariable(Component(tree, "OptionsRow")).Name.ShouldBe("Inner.IsEnabled");

        tree.Undo();
        Screen(tree, "Title").Instances.Single().BaseType.ShouldBe("Row");
        tree.SnapshotFiles().ShouldMatch(start, "undoing the rename should restore the files");
    }

    [AvaloniaFact]
    public void DuplicatingTheRowComponent_KeepsTheFormsPropertyExposed()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);
        tree.Click(tree.NodeFor(Component(tree, "Row")));

        tree.Dialogs.AnswerNextUserString("RowCopy");
        tree.RightClick(tree.NodeFor(Component(tree, "Row")));
        tree.PickMenu("Duplicate Row");

        ExposedVariable(Component(tree, "RowCopy")).Name.ShouldBe("Inner.IsEnabled");
        ExposedVariable(Component(tree, "Row")).Name.ShouldBe("Inner.IsEnabled");
    }

    [AvaloniaFact]
    public void CopyingTheInnerInstanceWithinTheRow_DoesNotExposeTheSameNameTwice()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);
        tree.Click(tree.NodeFor(Component(tree, "Row").Instances.Single()));

        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(Component(tree, "Row")));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        Component(tree, "Row").Instances.Count.ShouldBe(2);
        Component(tree, "Row").DefaultState!.Variables.Count(variable => variable.ExposedAsName == Exposed).ShouldBe(1);
    }

    [AvaloniaFact]
    public void CopyingTheRowInstanceToAnotherScreen_KeepsTheValueSetOnTheExposedFormsProperty()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);
        tree.Project.AddScreen("Options");
        tree.Click(tree.NodeFor(Screen(tree, "Title").Instances.Single()));

        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(Screen(tree, "Options")));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        Screen(tree, "Options").Instances.Select(instance => instance.Name).ShouldBe(new[] { "RowA" });
        VariableGridHarness_Stored(Screen(tree, "Options"), "RowA." + Exposed).ShouldBe(false);
    }

    [AvaloniaFact]
    public void SavingAndReloading_KeepsTheExposureAndTheValueSetOnTheInstance()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);

        tree.Project.SaveAndReload();

        ExposedVariable(Component(tree, "Row")).Name.ShouldBe("Inner.IsEnabled");
        VariableGridHarness_Stored(Screen(tree, "Title"), "RowA." + Exposed).ShouldBe(false);
    }

    [AvaloniaFact]
    public void SelectingTheRowInstance_ShowsTheExposedFormsPropertyUnderItsExposedName()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);

        tree.Click(tree.NodeFor(Screen(tree, "Title").Instances.Single()));

        tree.Grid.ShownMemberNames().ShouldContain("RowA." + Exposed);
        tree.Grid.ShownMemberNames().ShouldNotContain("RowA.IsEnabled");
    }

    [AvaloniaFact]
    public void UnexposingTheFormsProperty_AndClearingValues_ClearsTheValueOnInstances_AndUndoRestoresBoth()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);
        tree.Click(tree.NodeFor(Component(tree, "Row").Instances.Single()));
        ProjectFileSnapshot start = tree.SnapshotFiles();
        tree.Dialogs.AnswerNext<ChoiceDialogViewModel>(dialog =>
        {
            dialog.SelectedValue = "Un-expose and clear values";
            return true;
        });

        tree.Grid.PickRowMenuItem("IsEnabled", "Un-expose Variable " + Exposed + " (Inner.IsEnabled)");

        Component(tree, "Row").DefaultState!.Variables.ShouldNotContain(variable => variable.ExposedAsName == Exposed);
        Screen(tree, "Title").DefaultState!.Variables.ShouldNotContain(variable => variable.Name == "RowA." + Exposed && variable.SetsValue);

        tree.Undo();
        ExposedVariable(Component(tree, "Row")).Name.ShouldBe("Inner.IsEnabled");
        VariableGridHarness_Stored(Screen(tree, "Title"), "RowA." + Exposed).ShouldBe(false);
        tree.SnapshotFiles().ShouldMatch(start, "one undo should restore the exposure and the cleared value");
    }

    [AvaloniaFact]
    public void RenamingTheExposedFormsProperty_MovesTheValueSetOnInstances_AndUndoMovesItBack()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);
        tree.Click(tree.NodeFor(Component(tree, "Row").Instances.Single()));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNextUserString("Active");
        tree.Grid.PickRowMenuItem("IsEnabled", "Rename Variable [" + Exposed + "]");

        ExposedVariable_Named(Component(tree, "Row"), "Active").Name.ShouldBe("Inner.IsEnabled");
        VariableGridHarness_Stored(Screen(tree, "Title"), "RowA.Active").ShouldBe(false);
        VariableGridHarness_Stored(Screen(tree, "Title"), "RowA." + Exposed).ShouldBeNull();

        tree.Undo();
        ExposedVariable(Component(tree, "Row")).Name.ShouldBe("Inner.IsEnabled");
        VariableGridHarness_Stored(Screen(tree, "Title"), "RowA." + Exposed).ShouldBe(false);
        tree.SnapshotFiles().ShouldMatch(start, "undoing the rename should restore the files");
    }

    [AvaloniaFact]
    public void DerivedRow_InstancesStillShowTheInheritedExposedFormsProperty()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);
        ComponentSave derived = tree.Project.AddComponent("WideRow", "Row");
        InstanceSave wide = tree.Project.AddInstance(Screen(tree, "Title"), "WideA", derived.Name);

        tree.Click(tree.NodeFor(wide));

        tree.Grid.ShownMemberNames().ShouldContain("WideA." + Exposed);
    }

    private static VariableSave ExposedVariable_Named(ElementSave element, string exposedName) =>
        element.DefaultState!.Variables.Single(variable => variable.ExposedAsName == exposedName);

    private static object? VariableGridHarness_Stored(ElementSave element, string variableName) =>
        element.DefaultState!.Variables.FirstOrDefault(variable => variable.Name == variableName && variable.SetsValue)?.Value;
}
