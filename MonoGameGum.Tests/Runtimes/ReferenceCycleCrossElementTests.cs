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

    // ---- A row that only shares a written name with a loop row is not in the loop ----

    private static StateSaveCategory AddCategoryState(ComponentSave component, string stateName)
    {
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        category.States.Add(new StateSave { Name = stateName, ParentContainer = component });
        component.Categories.Add(category);
        return category;
    }

    private static void AddRowToState(StateSave state, string instance, string line)
    {
        VariableListSave<string> list = new VariableListSave<string> { Name = instance + ".VariableReferences", Type = "string" };
        list.Value.Add(line);
        state.VariableLists.Add(list);
    }

    private static GumProjectSave LoopWithZ(out ComponentSave panel, out ComponentSave z)
    {
        GumProjectSave project = CreateProject();
        panel = AddComponent(project, "Panel", "A");
        ComponentSave other = AddComponent(project, "Other", "B");
        z = AddComponent(project, "Z", "I0");
        AddRow(panel, "A", "Width = Components/Other.B.Width + 1");
        AddRow(other, "B", "Width = Components/Panel.A.Width + 1");
        return project;
    }

    [Fact]
    public void ApplyVariableReferences_CategorizedStateRowWithTheSameNameAsALoopRow_IsNotSkipped()
    {
        LoopWithZ(out ComponentSave panel, out _);
        StateSaveCategory category = AddCategoryState(panel, "S1");
        StateSave s1 = category.States[0];
        AddRowToState(s1, "A", "Width = Components/Z.I0.Width + 1");
        s1.Variables.Add(new VariableSave { Name = "A.Width", Type = "float", Value = 0f, SetsValue = true });

        panel.ApplyVariableReferences(s1);

        s1.GetValue("A.Width").ShouldBe(11f);
    }

    [Fact]
    public void ApplyVariableReferences_CategorizedStateRowIdenticalToALoopRow_IsSkipped()
    {
        LoopWithZ(out ComponentSave panel, out _);
        StateSave s1 = AddCategoryState(panel, "S1").States[0];
        AddRowToState(s1, "A", "Width = Components/Other.B.Width + 1");
        s1.Variables.Add(new VariableSave { Name = "A.Width", Type = "float", Value = 3f, SetsValue = true });

        panel.ApplyVariableReferences(s1);

        s1.GetValue("A.Width").ShouldBe(3f);
    }

    [Fact]
    public void CyclicRows_DerivedElementOverridingABaseLoopRow_DoesNotSkipTheOverride()
    {
        GumProjectSave project = LoopWithZ(out ComponentSave panel, out _);
        ComponentSave derived = AddComponent(project, "Derived", "A");
        derived.BaseType = "Panel";
        AddRow(derived, "A", "Width = Components/Z.I0.Width + 1");

        VariableReferenceGraph graph = VariableReferenceGraph.GetForApply(derived, derived.DefaultState);

        graph.CyclicRows.Select(row => row.Line).ShouldNotContain("Width = Components/Z.I0.Width + 1");
        graph.OrderedRows.Select(row => row.Line).ShouldContain("Width = Components/Z.I0.Width + 1");
    }

    [Fact]
    public void ApplyVariableReferences_DefaultRowReadingAnotherRowsLoopMember_IsStillEvaluated()
    {
        GumProjectSave project = LoopWithZ(out ComponentSave panel, out _);
        ComponentSave reader = AddComponent(project, "Reader", "R");
        AddRow(reader, "R", "Width = Components/Panel.A.Width + 1");

        reader.ApplyVariableReferences(reader.DefaultState);

        reader.DefaultState.GetValue("R.Width").ShouldBe(11f);
    }

    [Fact]
    public void CyclicRows_RandomAcyclicProjectsReusingNamesAcrossStatesAndBaseTypes_AreUnchangedByLoopDetection()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            Random random = new Random(seed);
            GumProjectSave project = CreateProject();
            int elementCount = random.Next(2, 6);
            for (int e = 0; e < elementCount; e++)
            {
                ComponentSave component = AddComponent(project, "E" + e, "I0", "I1");
                if (e > 0 && random.Next(0, 3) == 0)
                {
                    component.BaseType = "E" + random.Next(0, e);
                }
                AddRandomRows(random, component.DefaultState, e);
                if (random.Next(0, 2) == 0)
                {
                    StateSave categorized = AddCategoryState(component, "S").States[0];
                    AddRandomRows(random, categorized, e);
                }
            }

            foreach (ComponentSave component in project.Components)
            {
                List<StateSave> states = new List<StateSave> { component.DefaultState };
                states.AddRange(component.Categories.SelectMany(category => category.States));
                foreach (StateSave state in states)
                {
                    VariableReferenceGraph withDetection = VariableReferenceGraph.GetForState(component, state);
                    VariableReferenceGraph without = VariableReferenceGraph.Create(component, new[] { state }, false);
                    withDetection.CyclicRows.Select(row => row.Line).ShouldBe(without.CyclicRows.Select(row => row.Line), $"seed {seed} {component.Name}/{state.Name}");
                    withDetection.OrderedRows.Select(row => row.Line).ShouldBe(without.OrderedRows.Select(row => row.Line), $"seed {seed} {component.Name}/{state.Name}");
                }
                VariableReferenceGraph.GetForApply(component, component.DefaultState).CyclicRows.Select(row => row.Line)
                    .ShouldBe(VariableReferenceGraph.GetFor(component).CyclicRows.Select(row => row.Line), $"seed {seed} {component.Name} default");
            }
        }
    }

    // Rows read only elements with a lower number (and I1 reads I0), so the project has no loop, while
    // the same written names recur in many states and base types.
    private static void AddRandomRows(Random random, StateSave state, int elementNumber, bool useExpressions = true)
    {
        for (int i = 0; i < 2; i++)
        {
            if (random.Next(0, 3) == 0)
            {
                continue;
            }
            List<string> terms = new List<string>();
            if (elementNumber > 0 && random.Next(0, 2) == 0)
            {
                terms.Add($"Components/E{random.Next(0, elementNumber)}.I{random.Next(0, 2)}.Width");
            }
            if (i == 1 && random.Next(0, 2) == 0)
            {
                terms.Add("I0.Width");
            }
            if (terms.Count > 0)
            {
                // Without Gum.Expressions only a single path can be read.
                AddRowToState(state, "I" + i, "Width = " + string.Join(" + ", useExpressions ? terms : terms.Take(1)));
            }
        }
    }

    private static GumProjectSave BuildRing(int count)
    {
        GumProjectSave ring = CreateProject();
        for (int i = 0; i < count; i++)
        {
            ComponentSave component = AddComponent(ring, "E" + i, "A");
            AddRow(component, "A", $"Width = Components/E{(i + 1) % count}.A.Width + 1");
        }
        return ring;
    }

    [Fact]
    public void ApplyAll_AChainFourTimesAsLong_TakesAboutFourTimesAsLong()
    {
        GumProjectSave shorter = BuildChains(800, 800);
        GumProjectSave longer = BuildChains(3200, 3200);

        double shortMilliseconds = MedianMilliseconds(() => shorter.ApplyAllVariableReferences());
        double longMilliseconds = MedianMilliseconds(() => longer.ApplyAllVariableReferences());

        // Linear is about 4 (measured 4.2); quadratic is 16.
        (longMilliseconds / Math.Max(shortMilliseconds, 1)).ShouldBeLessThan(8);
    }

    [Fact]
    public void ApplyAll_AChainAndALoopOf10000Elements_FinishWithoutOverflowingTheStack()
    {
        GumProjectSave chain = BuildChains(10000, 10000);
        System.Threading.Tasks.Task chainApplied = System.Threading.Tasks.Task.Run(() => chain.ApplyAllVariableReferences());
        chainApplied.Wait(15000).ShouldBeTrue("a 10000-element chain took more than 15 s (measured about 0.5 s)");
        chainApplied.GetAwaiter().GetResult();
        chain.Components[0].DefaultState.GetValue("A.Width").ShouldBe(10009f);

        GumProjectSave ring = BuildRing(10000);
        System.Threading.Tasks.Task ringApplied = System.Threading.Tasks.Task.Run(() => ring.ApplyAllVariableReferences());
        ringApplied.Wait(15000).ShouldBeTrue("a 10000-element loop took more than 15 s (measured about 0.2 s)");
        ringApplied.GetAwaiter().GetResult();
        ring.Components[0].DefaultState.GetValue("A.Width").ShouldBe(10f);
    }

    // ---- ApplyAll resolves elements exactly like a per-element apply (no shared lookup cache) ----

    private static ScreenSave AddScreenWithInstance(GumProjectSave project, string name, string instance, float width)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = instance, BaseType = "Container", ParentContainer = screen });
        screen.DefaultState.Variables.Add(new VariableSave { Name = instance + ".Width", Type = "float", Value = width, SetsValue = true });
        project.Screens.Add(screen);
        return screen;
    }

    private static void SetWidth(ComponentSave component, string instance, float width) =>
        component.DefaultState.Variables.First(variable => variable.Name == instance + ".Width").Value = width;

    [Fact]
    public void ApplyAll_AScreenAndAComponentWithTheSameName_ReadTheKindTheRowNames()
    {
        GumProjectSave project = CreateProject();
        AddScreenWithInstance(project, "Menu", "M", 40f);
        ComponentSave menu = AddComponent(project, "Menu", "M");
        SetWidth(menu, "M", 20f);
        ComponentSave reader = AddComponent(project, "Reader", "R");
        AddRow(reader, "R", "Width = Screens/Menu.M.Width + 1");

        project.ApplyAllVariableReferences();

        reader.DefaultState.GetValue("R.Width").ShouldBe(41f);
    }

    [Fact]
    public void ApplyAll_AComponentNamedLikeAStandardElement_IsReadByItsReaders()
    {
        GumProjectSave project = CreateProject();
        ComponentSave text = AddComponent(project, "Text", "A");
        SetWidth(text, "A", 31f);
        ComponentSave reader = AddComponent(project, "Reader", "R");
        AddRow(reader, "R", "Width = Components/Text.A.Width + 1");

        project.ApplyAllVariableReferences();

        reader.DefaultState.GetValue("R.Width").ShouldBe(32f);
    }

    [Fact]
    public void ApplyAll_ComponentsWhoseNamesDifferOnlyInCase_ResolveToTheFirstLikeAPerElementApply()
    {
        GumProjectSave project = CreateProject();
        ComponentSave upper = AddComponent(project, "Panel", "A");
        ComponentSave lower = AddComponent(project, "panel", "A");
        SetWidth(upper, "A", 5f);
        SetWidth(lower, "A", 50f);
        ComponentSave reader = AddComponent(project, "Reader", "R");
        AddRow(reader, "R", "Width = Components/panel.A.Width + 1");

        project.ApplyAllVariableReferences();

        // The lookup ignores case and takes the first, as a per-element apply does.
        reader.ApplyVariableReferences(reader.DefaultState);
        float perElement = (float)reader.DefaultState.GetValue("R.Width")!;
        reader.DefaultState.Variables.First(variable => variable.Name == "R.Width").Value = 10f;
        project.ApplyAllVariableReferences();
        reader.DefaultState.GetValue("R.Width").ShouldBe(perElement);
        perElement.ShouldBe(6f);
    }

    [Fact]
    public void CyclicRows_TwoComponentsWithTheSameName_TheLoopIsThroughTheFirstOne()
    {
        GumProjectSave project = CreateProject();
        ComponentSave first = AddComponent(project, "Panel", "A");
        ComponentSave second = AddComponent(project, "Panel", "A");
        ComponentSave other = AddComponent(project, "Other", "B");
        AddRow(first, "A", "Width = Components/Other.B.Width + 1");
        AddRow(second, "A", "Width = Components/Other.B.Width + 1");
        AddRow(other, "B", "Width = Components/Panel.A.Width + 1");

        VariableReferenceGraph.GetForApply(first, first.DefaultState).CyclicRows.Count.ShouldBe(1);
        VariableReferenceGraph.GetForApply(second, second.DefaultState).CyclicRows.ShouldBeEmpty();
    }

    // ---- Values over random projects ----

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplyAll_RandomAcyclicProjects_GiveTheSameValuesWithAndWithoutLoopDetection(bool useExpressions)
    {
        for (int seed = 0; seed < 300; seed++)
        {
            string with = AppliedSnapshot(seed, useExpressions, detect: true);
            string without = AppliedSnapshot(seed, useExpressions, detect: false);
            with.ShouldBe(without, $"seed {seed}");
        }
    }

    private string AppliedSnapshot(int seed, bool useExpressions, bool detect)
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
        VariableReferenceGraph.DetectCrossElementLoops = detect;
        try
        {
            Random random = new Random(seed);
            GumProjectSave project = CreateProject();
            int elementCount = random.Next(2, 6);
            for (int e = 0; e < elementCount; e++)
            {
                ComponentSave component = AddComponent(project, "E" + e, "I0", "I1");
                if (e > 0 && random.Next(0, 3) == 0)
                {
                    component.BaseType = "E" + random.Next(0, e);
                }
                AddRandomRows(random, component.DefaultState, e, useExpressions);
                if (random.Next(0, 2) == 0)
                {
                    AddRandomRows(random, AddCategoryState(component, "S").States[0], e, useExpressions);
                }
            }
            project.ApplyAllVariableReferences();
            return string.Join(";", project.Components.SelectMany(component =>
                new[] { component.DefaultState }.Concat(component.Categories.SelectMany(category => category.States))
                    .Select(state => component.Name + "/" + state.Name + ":" + string.Join(",", state.Variables.Select(variable => variable.Name + "=" + variable.Value)))));
        }
        finally
        {
            VariableReferenceGraph.DetectCrossElementLoops = true;
            ElementSaveExtensions.CustomEvaluateExpression = null;
            ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void ApplyAll_RandomProjectsWithLoops_KeepLoopRowsAtTheirStoredValues()
    {
        for (int seed = 0; seed < 150; seed++)
        {
            Random random = new Random(1000 + seed);
            (GumProjectSave project, Dictionary<string, List<string>> edges) = Build(random, random.Next(2, 6));
            (HashSet<string> onCycle, _) = Oracle(edges);

            project.ApplyAllVariableReferences();
            project.ApplyAllVariableReferences();
            project.ApplyAllVariableReferences();

            foreach (string node in onCycle)
            {
                string[] parts = node.Split('|');
                ComponentSave component = project.Components.First(item => item.Name == parts[0]);
                component.DefaultState.GetValue(parts[1]).ShouldBe(10f, $"seed {seed}: {node} is in a loop");
            }
        }
    }
}
