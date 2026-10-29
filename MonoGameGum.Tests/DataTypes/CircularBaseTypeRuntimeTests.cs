using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

/// <summary>
/// #7020: components whose BaseTypes form a cycle must not overflow the stack when a game builds them.
/// </summary>
public class CircularBaseTypeRuntimeTests : BaseTestClass
{
    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static ComponentSave AddComponent(GumProjectSave project, string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        project.Components.Add(component);
        return component;
    }

    [Fact]
    public void ToGraphicalUiElement_ComponentsNamingEachOtherAsBase_ReturnsAnElement()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);
        ComponentSave first = AddComponent(project, "First", "Second");
        AddComponent(project, "Second", "First");
        ObjectFinder.Self.GumProjectSave = project;

        GraphicalUiElement element = first.ToGraphicalUiElement();

        element.ShouldNotBeNull();
    }

    [Fact]
    public void ToGraphicalUiElement_ComponentThatIsItsOwnBase_ReturnsAnElement()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);
        ComponentSave only = AddComponent(project, "Only", "Only");
        ObjectFinder.Self.GumProjectSave = project;

        GraphicalUiElement element = only.ToGraphicalUiElement();

        element.ShouldNotBeNull();
    }
}
