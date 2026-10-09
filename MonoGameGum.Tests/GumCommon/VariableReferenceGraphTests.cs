using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumRuntime;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.GumCommon;

public class VariableReferenceGraphTests : BaseTestClass
{
    [Fact]
    public void OrderedRows_ARowReadingALaterRowsOutput_RunsAfterThatRow()
    {
        ComponentSave element = CreateElement("A = B", "B = C", "C = 5");

        VariableReferenceGraph graph = VariableReferenceGraph.GetFor(element);

        graph.OrderedRows.Select(row => row.Line).ShouldBe(new[] { "C = 5", "B = C", "A = B" });
    }

    [Fact]
    public void OrderedRows_RowsWithNoDependencyBetweenThem_KeepTheirWrittenOrder()
    {
        ComponentSave element = CreateElement("X = 1", "Y = 2", "Z = 3");

        VariableReferenceGraph graph = VariableReferenceGraph.GetFor(element);

        graph.OrderedRows.Select(row => row.Line).ShouldBe(new[] { "X = 1", "Y = 2", "Z = 3" });
    }

    [Fact]
    public void CyclicRows_RowsThatReadEachOther_AreReportedAndLeftOutOfTheOrder()
    {
        ComponentSave element = CreateElement("A = B", "B = A", "C = A", "D = 1");

        VariableReferenceGraph graph = VariableReferenceGraph.GetFor(element);

        graph.CyclicRows.Select(row => row.Line).ShouldBe(new[] { "A = B", "B = A" });
        // D has nothing to do with the cycle; C only reads from it, so it still has a place in the order.
        graph.OrderedRows.Select(row => row.Line).ShouldBe(new[] { "D = 1", "C = A" });
    }

    [Fact]
    public void CyclicRows_ARowReadingItsOwnOutput_IsACycle()
    {
        ComponentSave element = CreateElement("X = X + 1");

        VariableReferenceGraph graph = VariableReferenceGraph.GetFor(element);

        graph.CyclicRows.Select(row => row.Line).ShouldBe(new[] { "X = X + 1" });
        graph.OrderedRows.ShouldBeEmpty();
    }

    [Fact]
    public void FindAffectedRows_ChangedVariable_FindsRowsReadingItDirectlyAndThroughOtherRows()
    {
        ComponentSave element = CreateElement(
            "Z = Y * 2",
            "Y = Math.Sin(Progress)",
            "Unrelated = Other + 1");
        VariableReferenceGraph graph = VariableReferenceGraph.GetFor(element);
        List<ReferenceRow> affected = new List<ReferenceRow>();

        graph.FindAffectedRows("Progress", affected);

        affected.Select(row => row.Line).ShouldBe(new[] { "Y = Math.Sin(Progress)", "Z = Y * 2" });
    }

    [Fact]
    public void FindAffectedRows_ARowReadingAMemberOfAnotherInstance_IsNotAffectedByTheElementsOwnVariable()
    {
        ComponentSave element = CreateElement("Alpha = Other.Width");
        VariableReferenceGraph graph = VariableReferenceGraph.GetFor(element);
        List<ReferenceRow> affected = new List<ReferenceRow>();

        graph.FindAffectedRows("Width", affected);

        affected.ShouldBeEmpty();
    }

    [Fact]
    public void FindAffectedRows_ARowReadingTheElementThroughItsOwnQualifier_IsAffected()
    {
        ComponentSave element = CreateElement("Alpha = Components/Foo.Circle.Width");
        VariableReferenceGraph graph = VariableReferenceGraph.GetFor(element);
        List<ReferenceRow> affected = new List<ReferenceRow>();

        graph.FindAffectedRows("Circle.Width", affected);

        affected.Select(row => row.Line).ShouldBe(new[] { "Alpha = Components/Foo.Circle.Width" });
    }

    [Fact]
    public void FindAffectedRows_ARowReadingAnAbsoluteValue_IsAffectedByAnyChange()
    {
        ComponentSave element = CreateElement("Alpha = AbsoluteWidth");
        VariableReferenceGraph graph = VariableReferenceGraph.GetFor(element);
        List<ReferenceRow> affected = new List<ReferenceRow>();

        graph.FindAffectedRows("Rotation", affected);
        int withLiveRows = affected.Count;
        graph.FindAffectedRows("Rotation", affected, includeLiveLayoutRows: false);

        withLiveRows.ShouldBe(1);
        affected.ShouldBeEmpty();
    }

    [Fact]
    public void FindAffectedRows_ARowOnAnInstance_ReadsAndWritesNamesQualifiedByTheInstance()
    {
        ComponentSave element = CreateElement();
        element.Instances.Add(new InstanceSave { Name = "Circle", BaseType = "Circle", ParentContainer = element });
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "Circle.VariableReferences" };
        list.Value.Add("X = @Index * 2 + Offset");
        element.DefaultState.VariableLists.Add(list);
        VariableReferenceGraph graph = VariableReferenceGraph.GetFor(element);

        ReferenceRow row = graph.OrderedRows.Single();

        row.WrittenName.ShouldBe("Circle.X");
        row.Reads.ShouldBe(new[] { "Circle.Index", "Offset" });
    }

    [Fact]
    public void OrderedRows_RowAssigningTheElementsName_IsLeftOut()
    {
        ComponentSave element = CreateElement("Name = Other", "Y = Other");

        VariableReferenceGraph graph = VariableReferenceGraph.GetFor(element);

        graph.OrderedRows.Select(row => row.Line).ShouldBe(new[] { "Y = Other" });
    }

    [Theory]
    [InlineData("Math.Sin(Progress) * 40", "Progress")]
    [InlineData("\"Width\" + Name", "Name")]
    [InlineData("1.5f * Width", "Width")]
    [InlineData("IsOn ? Left.Width : Right.Width", "IsOn,Left.Width,Right.Width")]
    [InlineData("Components/Other.Width + 1", "")]
    [InlineData("true && Enabled", "Enabled")]
    public void ScanReads_ListsTheVariablePathsAnExpressionReads(string expression, string expectedCsv)
    {
        List<string> reads = new List<string>();

        VariableReferenceGraph.ScanReads(expression, reads);

        string.Join(",", reads).ShouldBe(expectedCsv);
    }

    [Fact]
    public void GetFor_AfterARowIsEdited_BuildsAFreshGraph()
    {
        ComponentSave element = CreateElement("Y = Progress");
        VariableReferenceGraph before = VariableReferenceGraph.GetFor(element);
        VariableReferenceGraph again = VariableReferenceGraph.GetFor(element);

        VariableListSave<string> list = (VariableListSave<string>)element.DefaultState.VariableLists.Single();
        list.Value[0] = "Y = Other";
        VariableReferenceGraph after = VariableReferenceGraph.GetFor(element);

        again.ShouldBeSameAs(before);
        after.ShouldNotBeSameAs(before);
        after.OrderedRows.Single().Reads.ShouldBe(new[] { "Other" });
    }

    [Fact]
    public void GetFor_AfterARowIsAdded_BuildsAFreshGraph()
    {
        ComponentSave element = CreateElement("Y = Progress");
        VariableReferenceGraph before = VariableReferenceGraph.GetFor(element);

        ((VariableListSave<string>)element.DefaultState.VariableLists.Single()).Value.Add("Z = Y");
        VariableReferenceGraph after = VariableReferenceGraph.GetFor(element);

        after.ShouldNotBeSameAs(before);
        after.OrderedRows.Count.ShouldBe(2);
    }

    [Fact]
    public void GetFor_InheritedRows_AreIncludedBeforeTheElementsOwn()
    {
        ComponentSave baseElement = CreateElement("Y = Progress");
        baseElement.Name = "Base";
        ComponentSave derived = new ComponentSave { Name = "Derived", BaseType = "Base" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = derived };
        derived.States.Add(state);
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        list.Value.Add("Z = Y");
        state.VariableLists.Add(list);
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(baseElement);
        project.Components.Add(derived);
        ObjectFinder.Self.GumProjectSave = project;

        VariableReferenceGraph graph = VariableReferenceGraph.GetFor(derived);

        graph.OrderedRows.Select(row => row.Line).ShouldBe(new[] { "Y = Progress", "Z = Y" });
    }

    private static ComponentSave CreateElement(params string[] rows)
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        if (rows.Length > 0)
        {
            VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
            list.Value.AddRange(rows);
            state.VariableLists.Add(list);
        }
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(element);
        ObjectFinder.Self.GumProjectSave = project;
        return element;
    }
}
