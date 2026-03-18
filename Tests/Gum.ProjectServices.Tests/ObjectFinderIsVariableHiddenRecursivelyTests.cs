using Gum.DataTypes;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="ObjectFinder.IsVariableHiddenRecursively"/> — verifies that variable
/// hiding is detected on the element itself and through base-type inheritance.
/// </summary>
public class ObjectFinderIsVariableHiddenRecursivelyTests : BaseTestClass
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private ComponentSave BuildComponent(string name, string? baseType = null, params string[] hiddenVars)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        foreach (string v in hiddenVars)
        {
            component.VariablesHiddenFromInstances.Add(v);
        }
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;
        return component;
    }

    // -----------------------------------------------------------------------
    // Tests (alphabetical)
    // -----------------------------------------------------------------------

    [Fact]
    public void IsVariableHiddenRecursively_BaseTypeHidesVariable_ReturnsTrue()
    {
        ComponentSave baseComponent = BuildComponent("Base", hiddenVars: "Width");
        ComponentSave derived = BuildComponent("Derived", baseType: "Base");

        bool result = ObjectFinder.Self.IsVariableHiddenRecursively(derived, "Width");

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsVariableHiddenRecursively_DirectlyHidesVariable_ReturnsTrue()
    {
        ComponentSave component = BuildComponent("MyComponent", hiddenVars: "X");

        bool result = ObjectFinder.Self.IsVariableHiddenRecursively(component, "X");

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsVariableHiddenRecursively_EmptyHiddenList_ReturnsFalse()
    {
        ComponentSave component = BuildComponent("MyComponent");

        bool result = ObjectFinder.Self.IsVariableHiddenRecursively(component, "X");

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsVariableHiddenRecursively_HiddenOnGrandparent_ReturnsTrue()
    {
        ComponentSave grandparent = BuildComponent("Grandparent", hiddenVars: "Height");
        ComponentSave parent = BuildComponent("Parent", baseType: "Grandparent");
        ComponentSave child = BuildComponent("Child", baseType: "Parent");

        bool result = ObjectFinder.Self.IsVariableHiddenRecursively(child, "Height");

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsVariableHiddenRecursively_UnrelatedVariableInHiddenList_ReturnsFalse()
    {
        ComponentSave component = BuildComponent("MyComponent", hiddenVars: "Width");

        bool result = ObjectFinder.Self.IsVariableHiddenRecursively(component, "Height");

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsVariableHiddenRecursively_VariableHiddenOnlyOnDerived_ReturnsTrueForDerivedFalseForBase()
    {
        ComponentSave baseComponent = BuildComponent("Base");
        ComponentSave derived = BuildComponent("Derived", baseType: "Base", hiddenVars: "X");

        bool derivedResult = ObjectFinder.Self.IsVariableHiddenRecursively(derived, "X");
        bool baseResult = ObjectFinder.Self.IsVariableHiddenRecursively(baseComponent, "X");

        derivedResult.ShouldBeTrue();
        baseResult.ShouldBeFalse();
    }
}
