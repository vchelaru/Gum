using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class ElementDescriptionServiceTests : BaseTestClass
{
    private readonly ElementDescriptionService _service =
        new ElementDescriptionService(new VariableDefinedByTypeLogic(), new DefaultTypeResolver());

    [Fact]
    public void Describe_Element_ListsSelectorValuesStatesAndCategories()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        EditingTestProject.AddCategory(hud, "Size", "Big", "Normal");

        ElementDescription description = _service.Describe(hud, instance: null);

        description.Element.ShouldBe("Hud");
        description.Instance.ShouldBeNull();
        description.Type.ShouldBe("Hud");
        description.Variables.Single(item => item.Name == "SizeState").Values.ShouldBe(new[] { "Big", "Normal" });
        description.Variables.Single(item => item.Name == "State").Values.ShouldBe(new[] { "Default" });
        description.Variables.Single(item => item.Name == "Width").Values.ShouldBeNull();
        description.States.ShouldBe(new[] { "Default" });
        description.Categories.Single().Name.ShouldBe("Size");
        description.Categories.Single().States.ShouldBe(new[] { "Big", "Normal" });
    }

    [Fact]
    public void Describe_Instance_PrefixesNamesWithTheInstanceAndUsesItsType()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        EditingTestProject.AddCategory(hud, "Size", "Big");
        InstanceSave label = EditingTestProject.AddInstance(hud, "Label", "Text");

        ElementDescription description = _service.Describe(hud, label);

        description.Instance.ShouldBe("Label");
        description.Type.ShouldBe("Text");
        description.Variables.ShouldAllBe(item => item.Name.StartsWith("Label."));
        description.Variables.Single(item => item.Name == "Label.Text").Type.ShouldBe("string");
        description.Variables.Single(item => item.Name == "Label.HorizontalAlignment").Values.ShouldBe(new[] { "Left", "Center", "Right" });
        description.Variables.ShouldNotContain(item => item.Name == "Label.SizeState");
        description.Categories.Single().Name.ShouldBe("Size");
    }
}
