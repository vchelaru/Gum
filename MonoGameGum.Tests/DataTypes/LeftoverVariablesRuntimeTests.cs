using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Tests.Shared;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

/// <summary>
/// Variables left behind by a base type change (<see cref="LeftoverVariablesProject"/>) are skipped when a
/// game builds the element: no exception, and the element behaves as its current type. A loader that
/// applied or rejected them turns these red.
/// </summary>
public class LeftoverVariablesRuntimeTests : BaseTestClass
{
    private readonly GumProjectSave _project;

    public LeftoverVariablesRuntimeTests()
    {
        _project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_project);
        LeftoverVariablesProject.AddComponents(_project, typeChanged: true);
        ObjectFinder.Self.GumProjectSave = _project;
    }

    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private GraphicalUiElement Build(string componentName)
    {
        GraphicalUiElement visual = _project.Components.Single(c => c.Name == componentName).ToGraphicalUiElement();
        visual.UpdateLayout();
        return visual;
    }

    [Fact]
    public void ToGraphicalUiElement_ComponentWithLeftoverTextVariables_IsAContainerAndAppliesTheVariablesItStillHas()
    {
        GraphicalUiElement label = Build("Label");

        (label.RenderableComponent is Text).ShouldBeFalse();
        label.Width.ShouldBe(77);
    }

    [Fact]
    public void ApplyState_CategoryStateWithLeftoverTextVariable_DoesNotThrowAndStaysAContainer()
    {
        GraphicalUiElement label = Build("Label");
        StateSave big = _project.Components.Single(c => c.Name == "Label").Categories.Single().States.Single();

        Should.NotThrow(() => label.ApplyState(big));

        (label.RenderableComponent is Text).ShouldBeFalse();
    }

    [Fact]
    public void ToGraphicalUiElement_InstanceWithLeftoverTextVariables_IsAContainerAndAppliesTheVariablesItStillHas()
    {
        GraphicalUiElement card = Build("Card");

        GraphicalUiElement caption = card.GetGraphicalUiElementByName("Caption")!;
        (caption.RenderableComponent is Text).ShouldBeFalse();
        caption.Width.ShouldBe(55);
    }

    [Fact]
    public void ToGraphicalUiElement_ConsumersOfLeftoverComponents_DoNotThrow()
    {
        Should.NotThrow(() => Build("Outer").GetGraphicalUiElementByName("Card1").ShouldNotBeNull());
        Should.NotThrow(() => Build("Reader"));
    }

    [Fact]
    public void ToGraphicalUiElement_ComponentWithLeftoverTextVariablesAndATextInstance_OnlyTheInstanceIsText()
    {
        GraphicalUiElement wrapper = Build("Wrapper");

        (wrapper.RenderableComponent is Text).ShouldBeFalse();
        Text inner = (Text)wrapper.GetGraphicalUiElementByName("Inner")!.RenderableComponent;
        inner.RawText.ShouldBe("InnerText");
    }
}
