using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// An instance holding the saved components of a vector shader parameter (<c>Icon.Outline_Tint__X</c> and
/// <c>__Y</c>) keeps every component, its marker and its type when it is renamed, when it is
/// copied into another element and when it is copied into its own element.
/// </summary>
[Trait("Category", "EndToEnd")]
public class ShaderVectorCopyRenameScenarioTests
{
    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    private static InstanceSave CardWithAVectorOnTheIcon(ProjectTreeHarness tree)
    {
        ComponentSave card = tree.Project.AddComponent("Card");
        InstanceSave icon = tree.Project.AddInstance(card, "Icon", "Container");
        StateSave state = card.GetDefaultStateOrThrow();
        state.SetValue("Icon.SourceShaderFile", "Shaders/Outline.slang", "string");
        state.SetValue("Icon.Outline_Tint__X", 0.5f, icon, "float");
        state.GetVariableSave("Icon.Outline_Tint__X")!.ShaderParameter = "Tint#0/2";
        state.SetValue("Icon.Outline_Tint__Y", 0.25f, icon, "float");
        state.GetVariableSave("Icon.Outline_Tint__Y")!.ShaderParameter = "Tint#1/2";
        return icon;
    }

    private static void ShouldHoldTheVector(ComponentSave element, string instanceName)
    {
        VariableSave x = element.DefaultState.GetVariableSave($"{instanceName}.Outline_Tint__X").ShouldNotBeNull();
        x.Value.ShouldBe(0.5f);
        x.ShaderParameter.ShouldBe("Tint#0/2");
        x.Type.ShouldBe("float");
        VariableSave y = element.DefaultState.GetVariableSave($"{instanceName}.Outline_Tint__Y").ShouldNotBeNull();
        y.Value.ShouldBe(0.25f);
        y.ShaderParameter.ShouldBe("Tint#1/2");
        y.Type.ShouldBe("float");
    }

    [AvaloniaFact]
    public void RenamingTheInstance_MovesEveryComponent()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        InstanceSave icon = CardWithAVectorOnTheIcon(tree);
        tree.Click(tree.NodeFor(icon));

        tree.Dialogs.AnswerNextUserString("Glow");
        tree.Press(Key.F2, PhysicalKey.F2);

        ComponentSave card = Component(tree, "Card");
        card.Instances.Select(i => i.Name).ShouldBe(new[] { "Glow" });
        ShouldHoldTheVector(card, "Glow");
        card.DefaultState.Variables.Any(variable => variable.Name.StartsWith("Icon.")).ShouldBeFalse();
    }

    [AvaloniaFact]
    public void CopyingTheInstance_IntoAnotherElement_KeepsEveryComponent()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        InstanceSave icon = CardWithAVectorOnTheIcon(tree);
        tree.Project.AddComponent("Panel");
        tree.Click(tree.NodeFor(icon));

        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        ShouldHoldTheVector(Component(tree, "Panel"), "Icon");
    }

    [AvaloniaFact]
    public void CopyingTheInstance_IntoItsOwnElement_KeepsEveryComponentUnderTheNewName()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        InstanceSave icon = CardWithAVectorOnTheIcon(tree);
        tree.Click(tree.NodeFor(icon));

        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(Component(tree, "Card")));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        ComponentSave card = Component(tree, "Card");
        card.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Icon", "Icon1" });
        ShouldHoldTheVector(card, "Icon");
        ShouldHoldTheVector(card, "Icon1");
    }
}
