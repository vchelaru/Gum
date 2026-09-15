using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class RegisterFromFileFormRuntimeDefaultsTests : BaseTestClass
{
    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Fact]
    public void RootButtonComponent_WiresFormsControlAndAppliesToolTip()
    {
        BehaviorSave buttonBehavior = new BehaviorSave { Name = StandardFormsBehaviorNames.ButtonBehaviorName };
        buttonBehavior.FormsProperties.Add(new VariableSave { Type = "string", Name = "ToolTip" });

        ComponentSave buttonComponent = new ComponentSave { Name = "ButtonComponent" };
        StateSave buttonDefaultState = new StateSave { Name = "Default", ParentContainer = buttonComponent };
        buttonDefaultState.Variables.Add(new VariableSave
        {
            Type = "string",
            Name = "ToolTip",
            Value = "Click me",
            SetsValue = true
        });
        buttonComponent.States.Add(buttonDefaultState);
        buttonComponent.Behaviors.Add(new ElementBehaviorReference { BehaviorName = buttonBehavior.Name });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(buttonComponent);
        project.Behaviors.Add(buttonBehavior);
        ObjectFinder.Self.GumProjectSave = project;

        FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        // The Forms-behavior component is itself the root passed to ToGraphicalUiElement
        // (as GumPreview does when previewing a component directly), not a child instance.
        InteractiveGue buttonVisual = buttonComponent.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false)
            .ShouldBeAssignableTo<InteractiveGue>();

        Button button = buttonVisual.FormsControlAsObject.ShouldBeOfType<Button>();
        button.ToolTip.ShouldBe("Click me");
    }

    [Fact]
    public void ScreenWithButtonInstance_WiresFormsControlAndAppliesToolTip()
    {
        BehaviorSave buttonBehavior = new BehaviorSave { Name = StandardFormsBehaviorNames.ButtonBehaviorName };
        buttonBehavior.FormsProperties.Add(new VariableSave { Type = "string", Name = "ToolTip" });

        ComponentSave buttonComponent = new ComponentSave { Name = "ButtonComponent" };
        StateSave buttonDefaultState = new StateSave { Name = "Default", ParentContainer = buttonComponent };
        buttonDefaultState.Variables.Add(new VariableSave
        {
            Type = "string",
            Name = "ToolTip",
            Value = "Click me",
            SetsValue = true
        });
        buttonComponent.States.Add(buttonDefaultState);
        buttonComponent.Behaviors.Add(new ElementBehaviorReference { BehaviorName = buttonBehavior.Name });

        ScreenSave screen = new ScreenSave { Name = "ButtonScreen" };
        InstanceSave buttonInstance = new InstanceSave
        {
            Name = "ButtonInstance",
            BaseType = buttonComponent.Name,
            ParentContainer = screen
        };
        screen.Instances.Add(buttonInstance);
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(buttonComponent);
        project.Behaviors.Add(buttonBehavior);
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;

        FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        InteractiveGue buttonVisual = screenVisual.Children.OfType<InteractiveGue>().ShouldHaveSingleItem();

        Button button = buttonVisual.FormsControlAsObject.ShouldBeOfType<Button>();
        button.ToolTip.ShouldBe("Click me");
    }
}
