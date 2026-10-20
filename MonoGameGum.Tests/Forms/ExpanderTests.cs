using Gum.DataTypes;
using Gum.Managers;
using Gum.Forms.Controls;
using Gum.Forms.DefaultFromFileVisuals;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
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

    [Fact]
    public void Visual_Reassigned_ShouldOnlyToggleFromNewHeader()
    {
        InteractiveGue oldVisual = CreateVisual(out InteractiveGue oldHeader);
        Expander sut = new(oldVisual);
        InteractiveGue newVisual = CreateVisual(out InteractiveGue newHeader);
        sut.Visual = newVisual;

        oldHeader.CallClick();
        sut.IsExpanded.ShouldBeFalse();

        newHeader.CallClick();
        sut.IsExpanded.ShouldBeTrue();

        static InteractiveGue CreateVisual(out InteractiveGue header)
        {
            InteractiveGue visual = new(new InvisibleRenderable());
            header = new InteractiveGue(new InvisibleRenderable()) { Name = "HeaderContainer" };
            visual.Children!.Add(header);
            return visual;
        }
    }
}
