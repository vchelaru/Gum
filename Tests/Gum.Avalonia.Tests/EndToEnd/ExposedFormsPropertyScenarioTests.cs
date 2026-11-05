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
    public void ExposingAnUnauthoredFormsPropertyFromItsRowMenu_ExposesIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BuildProject(tree);
        ComponentSave panel = tree.Project.AddComponent("Panel");
        InstanceSave button = tree.Project.AddInstance(panel, "Primary", "Button");
        tree.Click(tree.NodeFor(button));
        tree.Dialogs.AnswerNextUserString("PrimaryEnabled");
        tree.Grid.RowMenu("IsEnabled").ShouldContain("Expose Variable");

        tree.Grid.PickRowMenuItem("IsEnabled", "Expose Variable");

        tree.Dialogs.Messages.ShouldBeEmpty();
        Component(tree, "Panel").DefaultState!.Variables
            .Single(variable => variable.Name == "Primary.IsEnabled").ExposedAsName.ShouldBe("PrimaryEnabled");
    }

    private static VariableSave ExposedVariable_Named(ElementSave element, string exposedName) =>
        element.DefaultState!.Variables.Single(variable => variable.ExposedAsName == exposedName);

    private static object? VariableGridHarness_Stored(ElementSave element, string variableName) =>
        element.DefaultState!.Variables.FirstOrDefault(variable => variable.Name == variableName && variable.SetsValue)?.Value;
}
