using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class ComponentBehaviorLogicTests : BaseTestClass
{
    private readonly ComponentBehaviorLogic _logic = new ComponentBehaviorLogic(new StateCreationLogic());

    private BehaviorSave AddBehavior(string name, string category, params string[] states)
    {
        BehaviorSave behavior = new BehaviorSave { Name = name };
        StateSaveCategory behaviorCategory = new StateSaveCategory { Name = category };
        foreach (string state in states)
        {
            behaviorCategory.States.Add(new StateSave { Name = state });
        }
        behavior.Categories.Add(behaviorCategory);
        Project.Behaviors.Add(behavior);
        return behavior;
    }

    [Fact]
    public void AddBehavior_AddsTheReferenceAndCopiesTheCategoryWithItsStates()
    {
        BehaviorSave behavior = AddBehavior("ButtonBehavior", "ButtonCategory", "Enabled", "Disabled");
        ComponentSave component = EditingTestProject.AddComponent(Project, "MyButton");

        IReadOnlyList<StateSaveCategory> created = _logic.AddBehavior(component, behavior);

        component.Behaviors.Select(item => item.BehaviorName).ShouldBe(new[] { "ButtonBehavior" });
        created.ShouldHaveSingleItem().ShouldBeSameAs(component.Categories.Single());
        component.Categories.Single().Name.ShouldBe("ButtonCategory");
        component.Categories.Single().States.Select(item => item.Name).ShouldBe(new[] { "Enabled", "Disabled" });
        component.Categories.Single().States.ShouldAllBe(item => item.ParentContainer == component);
        component.GetDefaultStateOrThrow().Variables.ShouldContain(item => item.Name == "ButtonCategoryState");
    }

    [Fact]
    public void AddBehavior_ComponentAlreadyHasTheCategory_AddsOnlyTheMissingStates()
    {
        BehaviorSave behavior = AddBehavior("ButtonBehavior", "ButtonCategory", "Enabled", "Disabled");
        ComponentSave component = EditingTestProject.AddComponent(Project, "MyButton");
        StateSaveCategory existing = EditingTestProject.AddCategory(component, "ButtonCategory", "Enabled");

        IReadOnlyList<StateSaveCategory> created = _logic.AddBehavior(component, behavior);

        created.ShouldBeEmpty();
        component.Categories.ShouldHaveSingleItem().ShouldBeSameAs(existing);
        existing.States.Select(item => item.Name).ShouldBe(new[] { "Enabled", "Disabled" });
    }

    [Fact]
    public void GetWhyCannotAdd_UnknownBehavior_NamesTheProjectsBehaviors()
    {
        AddBehavior("ButtonBehavior", "ButtonCategory", "Enabled");
        ComponentSave component = EditingTestProject.AddComponent(Project, "MyButton");

        string? why = _logic.GetWhyCannotAdd(Project, component, "Nope");

        why.ShouldNotBeNull();
        why.ShouldContain("Nope");
        why.ShouldContain("ButtonBehavior");
    }

    [Fact]
    public void GetWhyCannotAdd_ComponentAlreadyHasIt_Refuses()
    {
        BehaviorSave behavior = AddBehavior("ButtonBehavior", "ButtonCategory", "Enabled");
        ComponentSave component = EditingTestProject.AddComponent(Project, "MyButton");
        _logic.AddBehavior(component, behavior);

        _logic.GetWhyCannotAdd(Project, component, "ButtonBehavior")!.ShouldContain("already");
    }

    [Fact]
    public void GetWhyCannotAdd_ValidBehavior_ReturnsNull()
    {
        AddBehavior("ButtonBehavior", "ButtonCategory", "Enabled");
        ComponentSave component = EditingTestProject.AddComponent(Project, "MyButton");

        _logic.GetWhyCannotAdd(Project, component, "ButtonBehavior").ShouldBeNull();
    }

    [Fact]
    public void RemoveBehavior_DropsTheReferenceAndKeepsTheCategory()
    {
        BehaviorSave behavior = AddBehavior("ButtonBehavior", "ButtonCategory", "Enabled");
        ComponentSave component = EditingTestProject.AddComponent(Project, "MyButton");
        _logic.AddBehavior(component, behavior);

        _logic.GetWhyCannotRemove(component, "ButtonBehavior").ShouldBeNull();
        _logic.RemoveBehavior(component, "ButtonBehavior");

        component.Behaviors.ShouldBeEmpty();
        component.Categories.ShouldHaveSingleItem();
        _logic.GetWhyCannotRemove(component, "ButtonBehavior")!.ShouldContain("does not have");
    }
}
