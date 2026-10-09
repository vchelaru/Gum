using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumRuntime;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.GumCommon;

public class ReferenceDependenciesTests : BaseTestClass
{
    [Theory]
    [InlineData("Red = Components/Styles/Colors.Red", true)]
    [InlineData("Red = 1 + Components/Styles/Colors.Red", true)]
    [InlineData("Red = global::Components/Styles/Colors.Red", true)]
    [InlineData("Red = Components/Styles/ColorsExtra.Red", false)]
    [InlineData("Red = Components/Styles.Red", false)]
    [InlineData("// Red = Components/Styles/Colors.Red", false)]
    [InlineData("Red = 5", false)]
    [InlineData("no equals sign Components/Styles/Colors.Red", false)]
    public void RowReadsElement_MatchesWholeElementNamesAnywhereInTheRightSide(string row, bool expected)
    {
        ReferenceDependencies.RowReadsElement(row, "Components/Styles/Colors").ShouldBe(expected);
    }

    [Fact]
    public void OrderByDependency_ReaderListedBeforeItsSource_ComesAfterIt()
    {
        List<ComponentSave> elements = CreateChain("C:B", "B:A", "A:");

        List<ElementSave> ordered = ReferenceDependencies.OrderByDependency(elements);

        ordered.Select(item => item.Name).ShouldBe(new[] { "A", "B", "C" });
    }

    [Fact]
    public void OrderByDependency_ElementsWithNoDependencyBetweenThem_KeepTheirOrder()
    {
        List<ComponentSave> elements = CreateChain("X:", "Y:", "Z:");

        List<ElementSave> ordered = ReferenceDependencies.OrderByDependency(elements);

        ordered.Select(item => item.Name).ShouldBe(new[] { "X", "Y", "Z" });
    }

    [Fact]
    public void OrderByDependency_ElementsReadingEachOther_KeepTheirOrderAfterTheRest()
    {
        List<ComponentSave> elements = CreateChain("A:B", "B:A", "C:");

        List<ElementSave> ordered = ReferenceDependencies.OrderByDependency(elements);

        ordered.Select(item => item.Name).ShouldBe(new[] { "C", "A", "B" });
    }

    [Fact]
    public void OrderByDependency_ElementReadingItself_IsNotACycle()
    {
        List<ComponentSave> elements = CreateChain("A:A", "B:");

        List<ElementSave> ordered = ReferenceDependencies.OrderByDependency(elements);

        ordered.Select(item => item.Name).ShouldBe(new[] { "A", "B" });
    }

    [Fact]
    public void GetDependentsInOrder_ReadersOfReaders_AreIncludedAfterWhatTheyRead()
    {
        // D reads A and B, B reads A, E reads nothing relevant.
        List<ComponentSave> elements = CreateChain("D:A,B", "B:A", "A:", "E:");

        List<ElementSave> dependents = ReferenceDependencies.GetDependentsInOrder(elements, elements.Single(item => item.Name == "A"));

        dependents.Select(item => item.Name).ShouldBe(new[] { "B", "D" });
    }

    [Fact]
    public void GetDependentsInOrder_ElementsReadingEachOther_AreEachIncludedOnce()
    {
        List<ComponentSave> elements = CreateChain("A:B", "B:A");

        List<ElementSave> dependents = ReferenceDependencies.GetDependentsInOrder(elements, elements[0]);

        dependents.Select(item => item.Name).ShouldBe(new[] { "B" });
    }

    // Each spec is "Name:Read1,Read2": the element and the elements one of its rows reads.
    private List<ComponentSave> CreateChain(params string[] specs)
    {
        GumProjectSave project = new GumProjectSave();
        List<ComponentSave> elements = new List<ComponentSave>();
        foreach (string spec in specs)
        {
            string[] parts = spec.Split(':');
            ComponentSave element = new ComponentSave { Name = parts[0] };
            StateSave state = new StateSave { Name = "Default", ParentContainer = element };
            element.States.Add(state);
            if (parts[1].Length > 0)
            {
                VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
                list.Value.Add("V = " + string.Join(" + ", parts[1].Split(',').Select(name => $"Components/{name}.V")));
                state.VariableLists.Add(list);
            }
            project.Components.Add(element);
            elements.Add(element);
        }
        ObjectFinder.Self.GumProjectSave = project;
        return elements;
    }
}
