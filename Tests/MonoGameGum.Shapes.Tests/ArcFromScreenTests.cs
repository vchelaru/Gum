using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using Gum.Managers;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Shapes.Tests;

/// <summary>
/// #7296 - an Arc instance loaded from a screen (the path gumcli screenshot and a game take) must
/// instantiate without recursing and carry the authored values on its renderable.
/// </summary>
public class ArcFromScreenTests : RegistrationIsolatedTest
{
    [Fact]
    public void ToGraphicalUiElement_ArcInstanceWithAuthoredVariables_AppliesThicknessAndAnglesToRenderable()
    {
        AposShapeRuntime.RegisterRuntimeTypes();

        StandardElementSave arcStandard = new() { Name = "Arc" };
        arcStandard.States.Add(new StateSave { Name = "Default", ParentContainer = arcStandard });

        ScreenSave screen = new() { Name = "ArcScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "ArcInstance", BaseType = "Arc", ParentContainer = screen });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "ArcInstance.Thickness", Type = "float", Value = 24f, SetsValue = true });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "ArcInstance.SweepAngle", Type = "float", Value = 270f, SetsValue = true });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "ArcInstance.StartAngle", Type = "float", Value = 30f, SetsValue = true });

        GumProjectSave project = new();
        project.StandardElements.Add(arcStandard);
        project.Screens.Add(screen);
        GumProjectSave? previousProject = ObjectFinder.Self.GumProjectSave;
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            GraphicalUiElement visual = screen.ToGraphicalUiElement();

            GraphicalUiElement instance = visual.GetGraphicalUiElementByName("ArcInstance")!;
            instance.ShouldBeAssignableTo<ArcRuntime>();
            Arc renderable = (Arc)instance.RenderableComponent!;
            ((ArcRuntime)instance).Thickness.ShouldBe(24f);
            renderable.SweepAngle.ShouldBe(270f);
            renderable.StartAngle.ShouldBe(30f);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = previousProject;
        }
    }
}
