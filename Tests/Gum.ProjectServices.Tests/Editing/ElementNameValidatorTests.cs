using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class ElementNameValidatorTests : BaseTestClass
{
    private sealed class FixedTopLevelNames : ITopLevelNameSource
    {
        public void FillTopLevelNames(ElementSave element, List<TopLevelName> names) =>
            names.Add(new TopLevelName("Blink", "Animation", new object()));
    }

    [Fact]
    public void IsInstanceNameValid_RejectsDuplicatesReservedNamesAndOtherSourcesNames()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        hud.Instances.Add(new InstanceSave { Name = "Counter1", BaseType = "Text", ParentContainer = hud });
        ElementNameValidator validator = new ElementNameValidator(new FixedTopLevelNames());

        validator.IsInstanceNameValid("counter1", null, hud, out string? duplicate).ShouldBeFalse();
        duplicate.ShouldNotBeNull();
        validator.IsInstanceNameValid("Width", null, hud, out _).ShouldBeFalse();
        validator.IsInstanceNameValid("Blink", null, hud, out _).ShouldBeFalse();
        validator.IsInstanceNameValid("Counter2", null, hud, out string? none).ShouldBeTrue();
        none.ShouldBeNull();
    }

    [Fact]
    public void IsElementNameValid_RejectsAnExistingComponentInTheSameFolder()
    {
        EditingTestProject.AddComponent(Project, "Controls/Counter");
        ElementNameValidator validator = new ElementNameValidator(topLevelNameSource: null);

        validator.IsElementNameValid("Counter", "Controls", null, out _).ShouldBeFalse();
        validator.IsElementNameValid("Counter", null, null, out _).ShouldBeTrue();
    }

    [Fact]
    public void IsCategoryAndStateNameValid_RejectDuplicates()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        StateSaveCategory size = new StateSaveCategory { Name = "Size" };
        size.States.Add(new StateSave { Name = "Big" });
        hud.Categories.Add(size);
        ElementNameValidator validator = new ElementNameValidator(topLevelNameSource: null);

        validator.IsCategoryNameValid("Size", hud, out _).ShouldBeFalse();
        validator.IsStateNameValid("Big", size, null, out _).ShouldBeFalse();
        validator.IsStateNameValid("Small", size, null, out _).ShouldBeTrue();
    }
}
