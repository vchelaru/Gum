using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.DataTypes.Behaviors;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Games;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A component loaded from a project with DialogBoxBehavior, UserControlBehavior or TooltipBehavior
/// gets the matching Forms control, not a bare element (#7252).
/// </summary>
public class DialogUserControlTooltipBehaviorFromFileTests : BaseTestClass
{
    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static InteractiveGue Build(params string[] behaviorNames)
    {
        ComponentSave component = new ComponentSave { Name = "Thing" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        foreach (string behaviorName in behaviorNames)
        {
            component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });
        }
        foreach (string behaviorName in behaviorNames.Distinct())
        {
            project.Behaviors.Add(new BehaviorSave { Name = behaviorName });
        }
        ObjectFinder.Self.GumProjectSave = project;

        FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        return component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false)
            .ShouldBeAssignableTo<InteractiveGue>();
    }

    [Fact]
    public void DialogBoxBehaviorComponent_CreatesDialogBox()
    {
        Build("DialogBoxBehavior").FormsControlAsObject.ShouldBeOfType<DialogBox>();
    }

    [Fact]
    public void UserControlBehaviorComponent_CreatesUserControl()
    {
        Build("UserControlBehavior").FormsControlAsObject.ShouldBeOfType<UserControl>();
    }

    [Fact]
    public void TooltipBehaviorComponent_CreatesTooltip()
    {
        Build("TooltipBehavior").FormsControlAsObject.ShouldBeOfType<Tooltip>();
    }

    [Fact]
    public void DialogBoxBehaviorListedTwice_CreatesDialogBox()
    {
        Build("DialogBoxBehavior", "DialogBoxBehavior").FormsControlAsObject.ShouldBeOfType<DialogBox>();
    }

    [Fact]
    public void ButtonBehaviorListedFirst_StillCreatesButton()
    {
        Build("ButtonBehavior", "UserControlBehavior").FormsControlAsObject.ShouldBeOfType<Button>();
    }
}
