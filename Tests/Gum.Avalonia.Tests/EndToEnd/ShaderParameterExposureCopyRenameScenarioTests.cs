using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// An instance holding an exposed shader parameter keeps its value, marker, type and exposed name when it is
/// renamed and when it is copied into another element. Covers a scalar (<c>Icon.Outline_Thickness</c>) and an
/// exposed vector (<c>Icon.Outline_Tint__X</c>/<c>__Y</c>).
/// </summary>
[Trait("Category", "EndToEnd")]
public class ShaderParameterExposureCopyRenameScenarioTests
{
    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    private static InstanceSave CardWithExposedParameters(ProjectTreeHarness tree)
    {
        ComponentSave card = tree.Project.AddComponent("Card");
        InstanceSave icon = tree.Project.AddInstance(card, "Icon", "Container");
        StateSave state = card.GetDefaultStateOrThrow();
        state.SetValue("Icon.SourceShaderFile", "Shaders/Outline.slang", "string");

        state.SetValue("Icon.Outline_Thickness", 3f, icon, "float");
        VariableSave thickness = state.GetVariableSave("Icon.Outline_Thickness")!;
        thickness.ShaderParameter = "Thickness";
        thickness.ExposedAsName = "OutlineThickness";

        foreach ((string letter, int index, float value) in new[] { ("X", 0, 0.5f), ("Y", 1, 0.25f) })
        {
            state.SetValue("Icon.Outline_Tint__" + letter, value, icon, "float");
            VariableSave component = state.GetVariableSave("Icon.Outline_Tint__" + letter)!;
            component.ShaderParameter = $"Tint#{index}/2";
            component.ExposedAsName = "Tint__" + letter;
        }
        return icon;
    }

    private static void ShouldHoldTheParameters(ComponentSave element, string instanceName, bool exposed)
    {
        VariableSave thickness = element.DefaultState.GetVariableSave($"{instanceName}.Outline_Thickness").ShouldNotBeNull();
        thickness.Value.ShouldBe(3f);
        thickness.ShaderParameter.ShouldBe("Thickness");
        thickness.Type.ShouldBe("float");
        thickness.ExposedAsName.ShouldBe(exposed ? "OutlineThickness" : null);

        foreach ((string letter, int index) in new[] { ("X", 0), ("Y", 1) })
        {
            VariableSave component = element.DefaultState.GetVariableSave($"{instanceName}.Outline_Tint__{letter}").ShouldNotBeNull();
            component.ShaderParameter.ShouldBe($"Tint#{index}/2");
            component.Type.ShouldBe("float");
            component.ExposedAsName.ShouldBe(exposed ? "Tint__" + letter : null);
        }
    }

    [AvaloniaFact]
    public void RenamingTheInstance_KeepsTheExposedScalarAndVector()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        InstanceSave icon = CardWithExposedParameters(tree);
        tree.Click(tree.NodeFor(icon));

        tree.Dialogs.AnswerNextUserString("Glow");
        tree.Press(Key.F2, PhysicalKey.F2);

        ComponentSave card = Component(tree, "Card");
        card.Instances.Select(i => i.Name).ShouldBe(new[] { "Glow" });
        ShouldHoldTheParameters(card, "Glow", exposed: true);
        card.DefaultState.Variables.Any(variable => variable.Name.StartsWith("Icon.")).ShouldBeFalse();
    }

    [AvaloniaFact]
    public void CopyingTheInstance_IntoAnotherElement_KeepsTheScalarAndVectorButNotTheirExposure()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        InstanceSave icon = CardWithExposedParameters(tree);
        tree.Project.AddComponent("Panel");
        tree.Click(tree.NodeFor(icon));

        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        // A copy never carries the exposure: two variables would be exposed under one name.
        ShouldHoldTheParameters(Component(tree, "Panel"), "Icon", exposed: false);
    }
}
