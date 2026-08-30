using Gum.DataTypes;
using Gum.Managers;
using Gum.Forms.DefaultFromFileVisuals;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class ExpanderTests : BaseTestClass
{
    [Fact]
    public void ToGraphicalUiElement_ShouldCreateFromFileExpander_IfElementExists()
    {
        GumProjectSave gumProject = new GumProjectSave();
        var expanderComponent = new ComponentSave();
        expanderComponent.States.Add(new Gum.DataTypes.Variables.StateSave() { Name = "Default" });
        gumProject.Components.Add(expanderComponent);
        expanderComponent.Name = "TestExpanderComponent";
        expanderComponent.Behaviors.Add(new Gum.DataTypes.Behaviors.ElementBehaviorReference
        { BehaviorName = "ExpanderBehavior" });

        ObjectFinder.Self.GumProjectSave = gumProject;

        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        var gue = expanderComponent.ToGraphicalUiElement();

        (gue is DefaultFromFileExpanderRuntime).ShouldBeTrue();
    }
}
