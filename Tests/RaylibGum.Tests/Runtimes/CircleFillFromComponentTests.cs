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
/// #7287 - the Hazard theme's SliderThumb/RadioButton circles drew plain white on raylib because a
/// loaded Circle instance was a plain GraphicalUiElement (no "Circle" registration), so the
/// component's Body.IsFilled/Body.FillRed/... variables never reached the renderable.
/// </summary>
public class CircleFillFromComponentTests : BaseTestClass
{
    [Fact]
    public void ToGraphicalUiElement_CircleInstanceWithFillVariables_FillsRenderableWithThemeColor()
    {
        StandardElementSave circleStandard = new() { Name = "Circle" };
        circleStandard.States.Add(new StateSave { Name = "Default", ParentContainer = circleStandard });

        StandardElementSave containerStandard = new() { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });

        ComponentSave component = new() { Name = "Thumb", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Instances.Add(new InstanceSave { Name = "Body", BaseType = "Circle", ParentContainer = component });
        component.DefaultState.Variables.Add(new VariableSave { Name = "Body.IsFilled", Type = "bool", Value = true, SetsValue = true });
        component.DefaultState.Variables.Add(new VariableSave { Name = "Body.FillRed", Type = "int", Value = 244, SetsValue = true });
        component.DefaultState.Variables.Add(new VariableSave { Name = "Body.FillGreen", Type = "int", Value = 200, SetsValue = true });
        component.DefaultState.Variables.Add(new VariableSave { Name = "Body.FillBlue", Type = "int", Value = 26, SetsValue = true });

        typeof(SystemManagers).GetMethod("RegisterComponentRuntimeInstantiations", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(new SystemManagers(), null);

        GumProjectSave project = new();
        project.StandardElements.Add(circleStandard);
        project.StandardElements.Add(containerStandard);
        project.Components.Add(component);
        GumProjectSave? previousProject = ObjectFinder.Self.GumProjectSave;
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            GraphicalUiElement visual = component.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);

            GraphicalUiElement body = visual.GetGraphicalUiElementByName("Body")!;
            body.ShouldBeAssignableTo<CircleRuntime>();
            LineCircle circle = (LineCircle)body.RenderableComponent!;
            circle.IsFilled.ShouldBeTrue();
            circle.FillColor.ShouldBe(new Raylib_cs.Color(244, 200, 26, 255));
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = previousProject;
        }
    }
}
