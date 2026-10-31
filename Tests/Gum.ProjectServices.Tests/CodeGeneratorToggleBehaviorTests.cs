using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A component whose behavior is ToggleBehavior derives from ToggleButton (#7222), like
/// CheckBoxBehavior derives from CheckBox.
/// </summary>
public class CodeGeneratorToggleBehaviorTests : BaseTestClass
{
    private string Generate(string behaviorName, bool isDefaultImplementation = false)
    {
        Project.Behaviors.Add(new BehaviorSave
        {
            Name = behaviorName,
            DefaultImplementation = isDefaultImplementation ? "Controls/Thing" : null
        });

        ComponentSave component = new ComponentSave { Name = "Controls/Thing", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.GumForms,
            RootNamespace = "MyGame",
        };

        return CodeGeneratorDefaultFormsRegistrationTests.CreateCodeGenerator(syntaxVersion: 4)
            .GetGeneratedCodeForElement(component, new CodeOutputElementSettings(), settings);
    }

    [Fact]
    public void GetGeneratedCodeForElement_ToggleBehavior_DerivesFromToggleButton()
    {
        string code = Generate(StandardFormsBehaviorNames.ToggleBehaviorName);

        code.ShouldContain("partial class Thing : global::Gum.Forms.Controls.ToggleButton");
    }

    [Fact]
    public void GetGeneratedCodeForElement_CheckBoxBehavior_StillDerivesFromCheckBox()
    {
        string code = Generate(StandardFormsBehaviorNames.CheckBoxBehaviorName);

        code.ShouldContain("partial class Thing : global::Gum.Forms.Controls.CheckBox");
    }
}
