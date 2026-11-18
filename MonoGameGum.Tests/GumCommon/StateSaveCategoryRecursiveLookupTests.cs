using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.GumCommon;

public class StateSaveCategoryRecursiveLookupTests : BaseTestClass
{
    private static (ComponentSave Derived, ComponentSave Base) CreatePair(string derivedBaseType)
    {
        GumProjectSave project = new();
        ComponentSave baseComponent = new() { Name = "Base" };
        baseComponent.Categories.Add(new StateSaveCategory { Name = "FromBase" });
        ComponentSave derived = new() { Name = "Derived", BaseType = derivedBaseType };
        project.Components.Add(baseComponent);
        project.Components.Add(derived);
        ObjectFinder.Self.GumProjectSave = project;
        return (derived, baseComponent);
    }

    [Fact]
    public void ByName_CategoryOnBase_ReturnsItAndTheBase()
    {
        (ComponentSave derived, ComponentSave baseComponent) = CreatePair("Base");

        StateSaveCategory? category = derived.GetStateSaveCategoryRecursively("FromBase", out IStateContainer? container);

        category.ShouldBe(baseComponent.Categories[0]);
        container.ShouldBe(baseComponent);
    }

    [Fact]
    public void ByCondition_CategoryOnBase_ReturnsItAndTheBase()
    {
        (ComponentSave derived, ComponentSave baseComponent) = CreatePair("Base");

        StateSaveCategory? category = derived.GetStateSaveCategoryRecursively(item => item.Name == "FromBase", out IStateContainer? container);

        category.ShouldBe(baseComponent.Categories[0]);
        container.ShouldBe(baseComponent);
    }

    [Fact]
    public void ByName_CyclicBaseTypes_ReturnsNullInsteadOfWalkingForever()
    {
        (ComponentSave derived, ComponentSave baseComponent) = CreatePair("Base");
        baseComponent.BaseType = "Derived";

        StateSaveCategory? category = derived.GetStateSaveCategoryRecursively("Missing", out IStateContainer? container);

        category.ShouldBeNull();
        container.ShouldBeNull();
    }

    [Fact]
    public void ByCondition_CyclicBaseTypes_ReturnsNullInsteadOfOverflowingTheStack()
    {
        (ComponentSave derived, ComponentSave baseComponent) = CreatePair("Base");
        baseComponent.BaseType = "Derived";

        derived.GetStateSaveCategoryRecursively(item => item.Name == "Missing", out IStateContainer? _).ShouldBeNull();
    }
}
