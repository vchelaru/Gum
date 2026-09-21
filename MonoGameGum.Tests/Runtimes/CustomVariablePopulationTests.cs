using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #4891: a Component's custom ("new") variable is generated as a bare auto-property with
// no wiring back to the Gum variable system (CodeGenerator.FillWithNewVariables). Under FindByName
// instantiation, nothing ever assigned it - GraphicalUiElement.SetProperty only knew a hardcoded
// switch of built-in properties (TrySetValueOnThis) and the contained renderable
// (SetPropertyThroughReflection targets the drawn primitive, not the GUE). This pins the fix:
// SetProperty falls back to reflecting the value directly onto the generated class when nothing
// else claims the variable name.
public class CustomVariablePopulationTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        base.Dispose();
    }

    // Mirrors what CodeGenerator.FillWithNewVariables emits for a custom variable under
    // FindByName: a plain get/set property with no body wiring it to anything.
    private class SliderRuntime : GraphicalUiElement
    {
        public SliderRuntime() : base(new InvisibleRenderable(), null) { }

        public int NumberOfMarks { get; set; }
    }

    [Fact]
    public void ToGraphicalUiElement_ShouldPopulateCustomVariable_FromDefaultState()
    {
        ElementSaveExtensions.RegisterGueInstantiationType("Slider", typeof(SliderRuntime));

        var element = new ComponentSave { Name = "Slider" };
        var defaultState = new StateSave { Name = "Default", ParentContainer = element };
        defaultState.Variables.Add(new VariableSave
        {
            Name = "NumberOfMarks",
            Value = 5,
            Type = "int",
            IsCustomVariable = true
        });
        element.States.Add(defaultState);

        var gumProject = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = gumProject;
        gumProject.Components.Add(element);

        var created = Gum.ElementSaveExtensionMethods.ToGraphicalUiElement(element);

        var slider = created.ShouldBeOfType<SliderRuntime>();
        slider.NumberOfMarks.ShouldBe(5);
    }
}
