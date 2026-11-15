using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A ToggleBehavior component derives from ToggleButton, which already declares Text, so an
/// exposed Text must not generate a second property (CS0108, #7515).
/// </summary>
public class CodeGeneratorToggleButtonExposedTextTests : BaseTestClass
{
    private string GenerateWithExposedText(string behaviorName)
    {
        Project.Behaviors.Add(new BehaviorSave { Name = behaviorName });
        Project.StandardElements.Single(item => item.Name == "Text").DefaultState.Variables.Add(
            new VariableSave { Name = "Text", Type = "string", Value = string.Empty });

        ComponentSave component = new ComponentSave { Name = "Controls/Thing", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        component.Instances.Add(new InstanceSave
        {
            Name = "TextInstance",
            BaseType = "Text",
            ParentContainer = component,
        });
        defaultState.Variables.Add(new VariableSave
        {
            Name = "TextInstance.Text",
            Type = "string",
            Value = "Label",
            SetsValue = true,
            ExposedAsName = "Text",
        });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        return CodeGeneratorDefaultFormsRegistrationTests.CreateCodeGenerator(syntaxVersion: 4)
            .GetGeneratedCodeForElement(
                component,
                new CodeOutputElementSettings(),
                new CodeOutputProjectSettings
                {
                    OutputLibrary = OutputLibrary.GumForms,
                    RootNamespace = "MyGame",
                });
    }

    [Theory]
    [InlineData("ToggleBehavior", "global::Gum.Forms.Controls.ToggleButton")]
    [InlineData("CheckBoxBehavior", "global::Gum.Forms.Controls.CheckBox")]
    [InlineData("RadioButtonBehavior", "global::Gum.Forms.Controls.RadioButton")]
    public void GetGeneratedCodeForElement_ExposedTextOnToggleFamily_DoesNotDeclareText(string behaviorName, string formsType)
    {
        string code = GenerateWithExposedText(behaviorName);

        code.ShouldContain("partial class Thing : " + formsType);
        code.ShouldNotContain("public string Text");
    }

    [Fact]
    public void GetGeneratedCodeForElement_ExposedTextOnPanel_DeclaresText()
    {
        string code = GenerateWithExposedText("PanelBehavior");

        code.ShouldContain("public string Text");
    }
}
