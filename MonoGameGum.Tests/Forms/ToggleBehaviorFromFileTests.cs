using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.DataTypes.Behaviors;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A component loaded from a project with ToggleBehavior gets a ToggleButton (#7222).
/// </summary>
public class ToggleBehaviorFromFileTests : BaseTestClass
{
    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static InteractiveGue Build(string behaviorName)
    {
        ComponentSave component = new ComponentSave { Name = "Thing" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(new BehaviorSave { Name = behaviorName });
        ObjectFinder.Self.GumProjectSave = project;

        FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        return component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false)
            .ShouldBeAssignableTo<InteractiveGue>();
    }

    [Fact]
    public void ToggleBehaviorComponent_CreatesToggleButton()
    {
        InteractiveGue visual = Build(StandardFormsBehaviorNames.ToggleBehaviorName);

        ToggleButton toggle = visual.FormsControlAsObject.ShouldBeOfType<ToggleButton>();
        toggle.IsChecked = true;
        toggle.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void CheckBoxBehaviorComponent_StillCreatesCheckBox()
    {
        InteractiveGue visual = Build("CheckBoxBehavior");

        visual.FormsControlAsObject.ShouldBeOfType<CheckBox>();
    }
}
