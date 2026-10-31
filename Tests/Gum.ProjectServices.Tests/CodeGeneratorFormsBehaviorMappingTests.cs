using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A component with DialogBoxBehavior, UserControlBehavior or TooltipBehavior derives from the
/// matching Forms class instead of a plain FrameworkElement (#7252).
/// </summary>
public class CodeGeneratorFormsBehaviorMappingTests : BaseTestClass
{
    private string Generate(string[] behaviorNames, bool isDefaultImplementation = false)
    {
        foreach (string behaviorName in behaviorNames.Distinct())
        {
            Project.Behaviors.Add(new BehaviorSave
            {
                Name = behaviorName,
                DefaultImplementation = isDefaultImplementation ? "Controls/Thing" : null
            });
        }

        ComponentSave component = new ComponentSave { Name = "Controls/Thing", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        foreach (string behaviorName in behaviorNames)
        {
            component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });
        }
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

    [Theory]
    [InlineData("DialogBoxBehavior", "global::Gum.Forms.Controls.Games.DialogBox")]
    [InlineData("UserControlBehavior", "global::Gum.Forms.Controls.UserControl")]
    [InlineData("TooltipBehavior", "global::Gum.Forms.Controls.Tooltip")]
    public void GetGeneratedCodeForElement_Behavior_DerivesFromMatchingFormsClass(string behaviorName, string formsType)
    {
        string code = Generate(new[] { behaviorName });

        code.ShouldContain("partial class Thing : " + formsType);
    }

    [Theory]
    [InlineData("DialogBoxBehavior", "global::Gum.Forms.Controls.Games.DialogBox")]
    [InlineData("UserControlBehavior", "global::Gum.Forms.Controls.UserControl")]
    [InlineData("TooltipBehavior", "global::Gum.Forms.Controls.Tooltip")]
    public void GetGeneratedCodeForElement_BehaviorListedTwice_DerivesFromMatchingFormsClass(string behaviorName, string formsType)
    {
        string code = Generate(new[] { behaviorName, behaviorName });

        code.ShouldContain("partial class Thing : " + formsType);
    }

    [Theory]
    [InlineData("DialogBoxBehavior")]
    [InlineData("UserControlBehavior")]
    [InlineData("TooltipBehavior")]
    public void GetGeneratedCodeForElement_ButtonBehaviorListedFirst_StillDerivesFromButton(string otherBehavior)
    {
        string code = Generate(new[] { StandardFormsBehaviorNames.ButtonBehaviorName, otherBehavior });

        code.ShouldContain("partial class Thing : global::Gum.Forms.Controls.Button");
    }

    [Fact]
    public void GetGeneratedCodeForElement_DefaultImplementationDialogBox_RegistersDefaultTemplate()
    {
        string code = Generate(new[] { "DialogBoxBehavior" }, isDefaultImplementation: true);

        code.ShouldContain("DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.Games.DialogBox)]");
    }

    [Fact]
    public void GetGeneratedCodeForElement_DefaultImplementationTooltip_DoesNotReplaceTooltipTemplate()
    {
        // A plain new Tooltip() keeps drawing what it does today; the from-file loader owns the
        // project Tooltip template (FromFileTooltipTemplate).
        string code = Generate(new[] { "TooltipBehavior" }, isDefaultImplementation: true);

        code.ShouldContain("partial class Thing : global::Gum.Forms.Controls.Tooltip");
        code.ShouldNotContain("DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.Tooltip)]");
    }
}
