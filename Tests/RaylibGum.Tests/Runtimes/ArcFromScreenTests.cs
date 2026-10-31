using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Renderables;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// #7296 - an Arc instance loaded from a screen must come out as an ArcRuntime whose renderable
/// carries the authored values, the same way it does on MonoGame and Skia.
/// </summary>
public class ArcFromScreenTests : BaseTestClass
{
    [Fact]
    public void ToGraphicalUiElement_ArcInstanceWithAuthoredVariables_AppliesThicknessAndAnglesToRenderable()
    {
        StandardElementSave arcStandard = new() { Name = "Arc" };
        arcStandard.States.Add(new StateSave { Name = "Default", ParentContainer = arcStandard });

        ScreenSave screen = new() { Name = "ArcScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "ArcInstance", BaseType = "Arc", ParentContainer = screen });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "ArcInstance.Thickness", Type = "float", Value = 24f, SetsValue = true });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "ArcInstance.SweepAngle", Type = "float", Value = 270f, SetsValue = true });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "ArcInstance.StartAngle", Type = "float", Value = 30f, SetsValue = true });

        typeof(SystemManagers).GetMethod("RegisterComponentRuntimeInstantiations", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(new SystemManagers(), null);

        GumProjectSave project = new();
        project.StandardElements.Add(arcStandard);
        project.Screens.Add(screen);
        GumProjectSave? previousProject = ObjectFinder.Self.GumProjectSave;
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            GraphicalUiElement visual = screen.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);

            GraphicalUiElement instance = visual.GetGraphicalUiElementByName("ArcInstance")!;
            instance.ShouldBeAssignableTo<ArcRuntime>();
            ArcRuntime arc = (ArcRuntime)instance;
            arc.Thickness.ShouldBe(24f);
            arc.PreRender();
            LineArc renderable = (LineArc)arc.RenderableComponent!;
            renderable.Thickness.ShouldBe(24f);
            renderable.SweepAngle.ShouldBe(270f);
            renderable.StartAngle.ShouldBe(30f);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = previousProject;
        }
    }

    [Fact]
    public void SetProperty_ThicknessBoxedAsInt_IsAccepted()
    {
        ArcRuntime arc = new();

        arc.SetProperty("Thickness", 24);

        arc.Thickness.ShouldBe(24f);
    }

    [Fact]
    public void SetProperty_CircleFloatVariablesBoxedAsInt_AreAccepted()
    {
        CircleRuntime circle = new();

        circle.SetProperty("Radius", 20);
        circle.SetProperty("StrokeWidth", 3);
        circle.SetProperty("DropshadowBlur", 4);

        circle.Radius.ShouldBe(20f);
        circle.StrokeWidth.ShouldBe(3f);
    }

    [Fact]
    public void SetProperty_RectangleFloatVariablesBoxedAsInt_AreAccepted()
    {
        RectangleRuntime rectangle = new();

        rectangle.SetProperty("CornerRadius", 8);
        rectangle.SetProperty("StrokeWidth", 3);
        rectangle.SetProperty("DropshadowOffsetX", 2);

        rectangle.CornerRadius.ShouldBe(8f);
        rectangle.StrokeWidth.ShouldBe(3f);
    }
}
