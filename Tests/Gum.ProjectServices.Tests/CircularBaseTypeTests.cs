using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A hand-edited or externally generated project can have components whose BaseTypes form a cycle
/// (#7020). Every walk up the base chain must stop at the cycle instead of overflowing the stack.
/// </summary>
public class CircularBaseTypeTests : BaseTestClass
{
    private ComponentSave AddComponent(string name, string? baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;
        return component;
    }

    #region ObjectFinder

    [Fact]
    public void GetBaseElements_ShouldStop_WhenElementIsItsOwnBase()
    {
        ComponentSave a = AddComponent("A", "A");

        ObjectFinder.Self.GetBaseElements(a).ShouldBeEmpty();
    }

    [Fact]
    public void GetBaseElements_ShouldStop_WhenTwoElementsNameEachOther()
    {
        ComponentSave a = AddComponent("A", "B");
        ComponentSave b = AddComponent("B", "A");

        ObjectFinder.Self.GetBaseElements(a).ShouldBe(new ElementSave[0]);
        ObjectFinder.Self.GetBaseElements(b).ShouldBe(new ElementSave[0]);
    }

    [Fact]
    public void GetBaseElements_ShouldStop_WhenThreeElementsFormACycle()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "C");
        AddComponent("C", "A");

        ObjectFinder.Self.GetBaseElements(a).ShouldBeEmpty();
    }

    [Fact]
    public void GetBaseElements_ShouldEndAtTheFirstCycleMember_WhenElementOnlyDerivesFromACycle()
    {
        ComponentSave leaf = AddComponent("Leaf", "A");
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");

        ObjectFinder.Self.GetBaseElements(leaf).ShouldBe(new ElementSave[] { a });
    }

    [Fact]
    public void GetBaseElements_ShouldStillListTheWholeChain_WhenThereIsNoCycle()
    {
        ComponentSave leaf = AddComponent("Leaf", "Middle");
        ComponentSave middle = AddComponent("Middle", "Root");
        ComponentSave root = AddComponent("Root", "Container");

        ObjectFinder.Self.GetBaseElements(leaf).ShouldBe(new ElementSave[] { middle, root, Project.StandardElements.Single(item => item.Name == "Container") });
    }

    [Fact]
    public void GetBaseTypeCycle_ShouldNameTheElementsStartingAtTheArgument()
    {
        ComponentSave a = AddComponent("A", "B");
        ComponentSave b = AddComponent("B", "C");
        ComponentSave c = AddComponent("C", "A");
        ComponentSave leaf = AddComponent("Leaf", "A");

        ObjectFinder.Self.GetBaseTypeCycle(b).ShouldBe(new ElementSave[] { b, c, a });
        ObjectFinder.Self.GetBaseTypeCycle(leaf).ShouldBeEmpty();
    }

    [Fact]
    public void GetBaseTypeCycle_ShouldBeOneElement_WhenElementIsItsOwnBase()
    {
        ComponentSave a = AddComponent("A", "A");

        ObjectFinder.Self.GetBaseTypeCycle(a).ShouldBe(new ElementSave[] { a });
    }

    [Fact]
    public void GetRootStandardElementSave_ShouldReturnNull_WhenBasesFormACycle()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");

        ObjectFinder.Self.GetRootStandardElementSave(a).ShouldBeNull();
    }

    [Fact]
    public void GetRootVariable_ShouldReturn_WhenBasesFormACycle()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");

        ObjectFinder.Self.GetRootVariable("X", a).ShouldBeNull();
    }

    [Fact]
    public void GetRootVariableList_ShouldReturn_WhenBasesFormACycle()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");

        ObjectFinder.Self.GetRootVariableList("X", a).ShouldBeNull();
    }

    [Fact]
    public void GetElementsInheritingFrom_ShouldReturn_WhenBasesFormACycle()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");
        ComponentSave leaf = AddComponent("Leaf", "A");

        ObjectFinder.Self.GetElementsInheritingFrom(a).ShouldBe(new ElementSave[] { leaf });
    }

    [Fact]
    public void IsVariableHiddenRecursively_ShouldReturnFalse_WhenBasesFormACycle()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");

        ObjectFinder.Self.IsVariableHiddenRecursively(a, "X").ShouldBeFalse();
    }

    [Fact]
    public void IsVariableDrivenByToolOnlyReference_ShouldReturnFalse_WhenBasesFormACycle()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");

        ObjectFinder.Self.IsVariableDrivenByToolOnlyReference(a, "X").ShouldBeFalse();
    }

    [Fact]
    public void IsVariableHiddenRecursively_ShouldFindTheVariable_OnTheFirstElementOfACycleAThirdDerivesFrom()
    {
        ComponentSave leaf = AddComponent("Leaf", "A");
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");
        a.VariablesHiddenFromInstances = new List<string> { "X" };

        ObjectFinder.Self.IsVariableHiddenRecursively(leaf, "X").ShouldBeTrue();
    }

    #endregion

    #region Save class extension methods

    [Fact]
    public void IsOfType_ShouldReturnFalse_WhenBasesFormACycleWithoutTheType()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");

        a.IsOfType("Container").ShouldBeFalse();
    }

    [Fact]
    public void GetStateSaveRecursively_ShouldReturnNull_WhenBasesFormACycleWithoutTheState()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");

        a.GetStateSaveRecursively("Missing").ShouldBeNull();
    }

    [Fact]
    public void GetStateSaveCategoryRecursively_ShouldReturnNull_WhenBasesFormACycleWithoutTheCategory()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");

        a.GetStateSaveCategoryRecursively("Missing").ShouldBeNull();
    }

    [Fact]
    public void GetValueRecursive_ShouldReturnNull_WhenBasesFormACycleWithoutTheVariable()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");

        a.DefaultState!.GetValueRecursive("Missing").ShouldBeNull();
    }

    [Fact]
    public void GetValueRecursive_ShouldFindTheValue_WhenAnotherElementInTheCycleSetsIt()
    {
        ComponentSave a = AddComponent("A", "B");
        ComponentSave b = AddComponent("B", "A");
        b.DefaultState!.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 5f, SetsValue = true });

        a.DefaultState!.GetValueRecursive("Width").ShouldBeNull();
        b.DefaultState!.GetValueRecursive("Width").ShouldBe(5f);
    }

    [Fact]
    public void GetVariableRecursive_ShouldReturnNull_WhenBasesFormACycleWithoutTheVariable()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");

        a.DefaultState!.GetVariableRecursive("Missing").ShouldBeNull();
    }

    [Fact]
    public void GetVariableListRecursive_ShouldReturnNull_WhenBasesFormACycleWithoutTheList()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");

        a.DefaultState!.GetVariableListRecursive("Missing").ShouldBeNull();
    }

    [Fact]
    public void IsInstanceRecursivelyReferencingElement_ShouldReturnFalse_WhenBasesFormACycle()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");
        InstanceSave instance = new InstanceSave { Name = "Child", BaseType = "Unrelated" };

        ObjectFinder.Self.IsInstanceRecursivelyReferencingElement(instance, a).ShouldBeFalse();
    }

    #endregion

    #region HeadlessErrorChecker

    [Fact]
    public void GetAllErrors_ShouldReportElementThatIsItsOwnBase()
    {
        const string name = "A";
        const string cycle = "A -> A";
        AddComponent(name, name);

        IReadOnlyList<ErrorResult> errors = new HeadlessErrorChecker(new Mock<ITypeResolver>().Object).GetAllErrors(Project);

        ErrorResult error = errors.ShouldHaveSingleItem();
        error.ElementName.ShouldBe(name);
        error.Code.ShouldBe("GUM0014");
        error.Severity.ShouldBe(ErrorSeverity.Error);
        error.Message.ShouldContain(cycle);
    }

    [Fact]
    public void GetAllErrors_ShouldReportEachElementInACycle_NamingTheWholeCycle()
    {
        AddComponent("A", "B");
        AddComponent("B", "C");
        AddComponent("C", "A");
        AddComponent("Leaf", "A");

        IReadOnlyList<ErrorResult> errors = new HeadlessErrorChecker(new Mock<ITypeResolver>().Object).GetAllErrors(Project);

        errors.Select(error => error.ElementName).ShouldBe(new[] { "A", "B", "C" }, ignoreOrder: true);
        errors.ShouldAllBe(error => error.Code == "GUM0014");
        errors.Single(error => error.ElementName == "B").Message.ShouldContain("B -> C -> A -> B");
    }

    [Fact]
    public void GetErrorsFor_ShouldReportTheCycle_ForTheSelectedElement()
    {
        ComponentSave a = AddComponent("A", "B");
        AddComponent("B", "A");

        IReadOnlyList<ErrorResult> errors = new HeadlessErrorChecker(new Mock<ITypeResolver>().Object).GetErrorsFor(a, Project);

        errors.ShouldContain(error => error.Code == "GUM0014" && error.Message.Contains("A -> B -> A"));
    }

    [Fact]
    public void GetAllErrors_ShouldNotReportACycle_ForAnOrdinaryChain()
    {
        AddComponent("Leaf", "Middle");
        AddComponent("Middle", "Container");

        IReadOnlyList<ErrorResult> errors = new HeadlessErrorChecker(new Mock<ITypeResolver>().Object).GetAllErrors(Project);

        errors.ShouldNotContain(error => error.Code == "GUM0014");
    }

    #endregion
}
