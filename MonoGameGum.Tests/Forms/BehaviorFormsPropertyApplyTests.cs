using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class BehaviorFormsPropertyApplyTests : BaseTestClass
{
    public BehaviorFormsPropertyApplyTests()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }

    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Fact]
    public void AttachingFormsControl_ToVisualWithBehaviorFormsPropertyDefault_AppliesValueToFormsControl()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "string", Name = "ToolTip" });

        ComponentSave component = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Type = "string",
            Name = "ToolTip",
            Value = "Click me",
            SetsValue = true
        });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        InteractiveGue visual = new InteractiveGue();
        visual.ElementSave = component;

        Button button = new Button(visual);

        button.ToolTip.ShouldBe("Click me");
    }
}
