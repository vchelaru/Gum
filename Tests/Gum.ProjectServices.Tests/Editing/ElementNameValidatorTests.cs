using Gum.DataTypes.Behaviors;
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
    public void IsVariableNameValid_RejectsAFormsPropertyNameOfTheElementsBehaviors()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "SliderBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Name = "Minimum", Type = "double" });
        Project.Behaviors.Add(behavior);
        ComponentSave slider = EditingTestProject.AddComponent(Project, "Slider");
        slider.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "SliderBehavior" });
        ComponentSave fancy = EditingTestProject.AddComponent(Project, "Fancy", baseType: "Slider");
        ComponentSave plain = EditingTestProject.AddComponent(Project, "Plain");
        ElementNameValidator validator = new ElementNameValidator(null);

        validator.IsVariableNameValid("Minimum", fancy, null, out string? whyNotValid).ShouldBeFalse();
        whyNotValid.ShouldContain("already used");
        validator.IsVariableNameValid("Minimum", plain, null, out _).ShouldBeTrue();
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

    [Theory]
    [InlineData("Name")]
    [InlineData("BaseType")]
    [InlineData("Locked")]
    [InlineData("IsSlot")]
    [InlineData("name")]
    [InlineData("locked")]
    [InlineData("DefaultChildContainer")]
    public void IsVariableNameValid_RejectsABuiltInPropertyName(string name)
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        ElementNameValidator validator = new ElementNameValidator(topLevelNameSource: null);

        validator.IsVariableNameValid(name, hud, null, out string? whyNotValid).ShouldBeFalse();
        whyNotValid.ShouldContain(name);
        validator.IsVariableNameValid(name + "Text", hud, null, out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Parent")]
    [InlineData("Width")]
    [InlineData("x")]
    [InlineData("Visible")]
    [InlineData("State")]
    public void IsVariableNameValid_RejectsAVariableTheElementsTypeDefines(string name)
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        ElementNameValidator validator = new ElementNameValidator(topLevelNameSource: null);

        validator.IsVariableNameValid(name, hud, null, out string? whyNotValid).ShouldBeFalse();
        whyNotValid.ShouldBe($"The variable name {name} is already used");
    }

    [Theory]
    [InlineData("Locked")]
    [InlineData("Speed")]
    public void IsVariableNameValid_AcceptsTheEditedVariablesOwnName(string name)
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        VariableSave edited = new VariableSave { Name = name, Type = "float", IsCustomVariable = true };
        hud.DefaultState.Variables.Add(edited);
        ElementNameValidator validator = new ElementNameValidator(topLevelNameSource: null);

        validator.IsVariableNameValid(name, hud, edited, out string? whyNotValid).ShouldBeTrue(whyNotValid);
        validator.IsVariableNameValid(name, hud, null, out _).ShouldBeFalse();
    }
}
