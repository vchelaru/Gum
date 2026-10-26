using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Finding a loop through other elements costs time linear in the rows of the project, however long a
/// chain of elements reading each other is (#6871), and finds exactly the rows that are in a loop.
/// </summary>
public class ReferenceCycleCrossElementTests : BaseTestClass
{
    public ReferenceCycleCrossElementTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        GumExpressionService.Initialize();
    }

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static GumProjectSave CreateProject()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementSave standardElement = new StandardElementSave { Name = "Container" };
        standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container")!.Clone());
        standardElement.DefaultState!.ParentContainer = standardElement;
        project.StandardElements.Add(standardElement);
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }

    private static ComponentSave AddComponent(GumProjectSave project, string name, params string[] instances)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        foreach (string instance in instances)
        {
            component.Instances.Add(new InstanceSave { Name = instance, BaseType = "Container", ParentContainer = component });
            component.DefaultState.Variables.Add(new VariableSave { Name = instance + ".Width", Type = "float", Value = 10f, SetsValue = true });
        }
        project.Components.Add(component);
        return component;
    }

    private static void AddRow(ComponentSave component, string instance, string line)
    {
        VariableListSave<string> list = component.DefaultState.VariableLists
            .OfType<VariableListSave<string>>().FirstOrDefault(item => item.Name == instance + ".VariableReferences")
            ?? new VariableListSave<string> { Name = instance + ".VariableReferences", Type = "string" };
        if (!component.DefaultState.VariableLists.Contains(list))
        {
            component.DefaultState.VariableLists.Add(list);
        }
        list.Value.Add(line);
    }

    // ---- Time ----

    private static GumProjectSave BuildChains(int elementCount, int chainLength)
    {
        GumProjectSave project = CreateProject();
        for (int i = 0; i < elementCount; i++)
        {
            ComponentSave component = AddComponent(project, "E" + i, "A");
            if ((i + 1) % chainLength != 0)
            {
                AddRow(component, "A", $"Width = Components/E{i + 1}.A.Width + 1");
            }
        }
        return project;
    }

    private static double MedianMilliseconds(Action action)
    {
        action();
        List<double> runs = new List<double>();
        for (int i = 0; i < 5; i++)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            action();
            runs.Add(stopwatch.Elapsed.TotalMilliseconds);
        }
        runs.Sort();
        return runs[runs.Count / 2];
    }

    [Theory]
    [InlineData(400)]
    [InlineData(100)]
    public void Chains_OfElementsReadingEachOther_AreAppliedInTimeLinearInTheElements(int chainLength)
    {
        GumProjectSave project = BuildChains(400, chainLength);

        double applyAll = MedianMilliseconds(() => project.ApplyAllVariableReferences());
        double perElement = MedianMilliseconds(() =>
        {
            foreach (ComponentSave component in project.Components)
            {
                component.ApplyVariableReferences(component.DefaultState);
            }
        });
        double create = MedianMilliseconds(() =>
        {
            foreach (ComponentSave component in project.Components)
            {
                component.ToGraphicalUiElement();
            }
        });

        // Generous budgets: about 5 to 10 times what the same project took before loops were detected.
        applyAll.ShouldBeLessThan(120, "ApplyAllVariableReferences, ms");
        perElement.ShouldBeLessThan(100, "ApplyVariableReferences on every element, ms");
        create.ShouldBeLessThan(200, "ToGraphicalUiElement on every element, ms");
    }

    // ---- Which rows ----

    private sealed class Spec
    {
        // reads[element][instance] = list of (element, instance) read
        public List<(int Element, int Instance)>[][] Reads = Array.Empty<List<(int, int)>[]>();
    }

    private static string NodeName(int element, int instance) => $"E{element}|I{instance}.Width";

    private static (GumProjectSave Project, Dictionary<string, List<string>> Edges) Build(Random random, int elementCount)
    {
        GumProjectSave project = CreateProject();
        Dictionary<string, List<string>> edges = new Dictionary<string, List<string>>();
        for (int e = 0; e < elementCount; e++)
        {
            AddComponent(project, "E" + e, "I0", "I1");
        }
        for (int e = 0; e < elementCount; e++)
        {
            ComponentSave component = project.Components[e];
            for (int i = 0; i < 2; i++)
            {
                string node = NodeName(e, i);
                edges[node] = new List<string>();
                int readCount = random.Next(0, 3);
                List<string> terms = new List<string>();
                for (int r = 0; r < readCount; r++)
                {
                    int otherElement = random.Next(0, 4) == 0 ? e : random.Next(0, elementCount);
                    int otherInstance = random.Next(0, 2);
                    if (otherElement == e && otherInstance == i)
                    {
                        continue;
                    }
                    edges[node].Add(NodeName(otherElement, otherInstance));
                    terms.Add(otherElement == e ? $"I{otherInstance}.Width" : $"Components/E{otherElement}.I{otherInstance}.Width");
                }
                if (terms.Count > 0)
                {
                    AddRow(component, "I" + i, "Width = " + string.Join(" + ", terms));
                }
            }
        }
        return (project, edges);
    }

    // Nodes on a cycle of the whole project's graph, and nodes that are between cycles (reached from one,
    // reaching one): rows on a cycle must be skipped, and the rows between cycles may be.
    private static (HashSet<string> OnCycle, HashSet<string> Between) Oracle(Dictionary<string, List<string>> edges)
    {
        Dictionary<string, HashSet<string>> reach = new Dictionary<string, HashSet<string>>();
        foreach (string start in edges.Keys)
        {
            HashSet<string> seen = new HashSet<string>();
            Stack<string> stack = new Stack<string>(edges[start]);
            while (stack.Count > 0)
            {
                string node = stack.Pop();
                if (seen.Add(node))
                {
                    foreach (string next in edges[node])
                    {
                        stack.Push(next);
                    }
                }
            }
            reach[start] = seen;
        }
        HashSet<string> onCycle = new HashSet<string>(edges.Keys.Where(node => reach[node].Contains(node)));
        HashSet<string> between = new HashSet<string>(edges.Keys.Where(node =>
            onCycle.Any(cycle => reach[cycle].Contains(node)) && onCycle.Any(cycle => reach[node].Contains(cycle))));
        return (onCycle, between);
    }

    [Fact]
    public void CyclicRows_RandomProjects_ContainEveryRowOnALoopAndNothingOutsideTheLoopsAndTheirLinks()
    {
        for (int seed = 0; seed < 150; seed++)
        {
            Random random = new Random(seed);
            (GumProjectSave project, Dictionary<string, List<string>> edges) = Build(random, random.Next(2, 6));
            (HashSet<string> onCycle, HashSet<string> between) = Oracle(edges);

            HashSet<string> actual = new HashSet<string>();
            for (int e = 0; e < project.Components.Count; e++)
            {
                ComponentSave component = project.Components[e];
                foreach (ReferenceRow row in VariableReferenceGraph.GetForApply(component, component.DefaultState).CyclicRows)
                {
                    actual.Add($"E{e}|{row.WrittenName}");
                }
            }

            foreach (string node in onCycle)
            {
                actual.ShouldContain(node, $"seed {seed}: a row in a loop must be skipped");
            }
            foreach (string node in actual)
            {
                (onCycle.Contains(node) || between.Contains(node)).ShouldBeTrue($"seed {seed}: {node} is not in or between loops");
            }
        }
    }

    [Fact]
    public void CyclicRows_ADiamondLoop_ContainsBothBranches()
    {
        // A reads Other.B; B reads Panel.C and Panel.D; C and D read A.
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddComponent(project, "Panel", "A", "C", "D");
        ComponentSave other = AddComponent(project, "Other", "B");
        AddRow(panel, "A", "Width = Components/Other.B.Width + 1");
        AddRow(other, "B", "Width = Components/Panel.C.Width + Components/Panel.D.Width");
        AddRow(panel, "C", "Width = A.Width + 1");
        AddRow(panel, "D", "Width = A.Width + 2");

        VariableReferenceGraph.GetForApply(panel, panel.DefaultState).CyclicRows.Select(row => row.WrittenName)
            .OrderBy(name => name).ShouldBe(new[] { "A.Width", "C.Width", "D.Width" });
    }

    [Fact]
    public void CyclicRows_ARowAddedToTheOtherElementLater_NowFindsTheLoop()
    {
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddComponent(project, "Panel", "A");
        ComponentSave other = AddComponent(project, "Other", "B");
        AddRow(panel, "A", "Width = Components/Other.B.Width + 1");
        VariableReferenceGraph.GetForApply(panel, panel.DefaultState).CyclicRows.ShouldBeEmpty();

        AddRow(other, "B", "Width = Components/Panel.A.Width + 1");

        VariableReferenceGraph.GetForApply(panel, panel.DefaultState).CyclicRows.Select(row => row.WrittenName).ShouldBe(new[] { "A.Width" });
    }
}
