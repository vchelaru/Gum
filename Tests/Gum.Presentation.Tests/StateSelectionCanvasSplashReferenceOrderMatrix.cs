using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

/// <summary>
/// #6856: the canvas evaluates a reference row only after every row it reads has been evaluated, whatever
/// order the rows are written in, in which state they sit and which list or instance owns them.
/// </summary>
public partial class StateSelectionCanvasSplashTests
{
    #region Hand-computed chains

    public enum RowPlacement
    {
        DefaultStateDefaultSelected,
        DefaultStateUnrelatedCategorySelected,
        UncategorizedStateRows,
        CategoryStateRows,
        SplitBetweenDefaultAndCategoryState,
    }

    private static readonly int[][] OrdersOfTwo = { new[] { 0, 1 }, new[] { 1, 0 } };

    private static readonly int[][] OrdersOfThree =
    {
        new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 }, new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 },
    };

    public static IEnumerable<object[]> ChainCases()
    {
        foreach (RowPlacement placement in Enum.GetValues<RowPlacement>())
        {
            foreach (int depth in new[] { 2, 3 })
            {
                foreach (int[] order in depth == 2 ? OrdersOfTwo : OrdersOfThree)
                {
                    yield return new object[] { placement, depth, string.Join(",", order) };
                }
            }
        }
    }

    // Rows, deepest first: C.Width = R.Width + 5 (55), B.Width = C.Width * 2 (110), A.Width = B.Width + 1 (111).
    // Depth 2 has only B.Width = R.Width * 2 (100) and A.Width = B.Width + 1 (101).
    private static (string Owner, string Line)[] ChainRows(int depth) => depth == 2
        ? new[] { ("B", "Width = R.Width * 2"), ("A", "Width = B.Width + 1") }
        : new[] { ("C", "Width = R.Width + 5"), ("B", "Width = C.Width * 2"), ("A", "Width = B.Width + 1") };

    private static ComponentSave BuildRowlessChain(string name = "RowChain", string baseType = "Container")
    {
        ComponentSave chain = new ComponentSave { Name = name, BaseType = baseType };
        foreach (string instanceName in new[] { "A", "B", "C", "R" })
        {
            chain.Instances.Add(new InstanceSave { Name = instanceName, BaseType = "Rectangle", ParentContainer = chain });
        }
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = chain };
        chain.States.Add(defaultState);
        // Stored values are what the rows would have produced before their inputs changed.
        SetNumber(defaultState, "A.Width", 10f);
        SetNumber(defaultState, "B.Width", 120f);
        SetNumber(defaultState, "C.Width", 30f);
        SetNumber(defaultState, "R.Width", 50f);
        return chain;
    }

    private static StateSave PlaceRows(ComponentSave chain, RowPlacement placement, (string Owner, string Line)[] rowsInWrittenOrder)
    {
        StateSave defaultState = chain.DefaultState!;
        StateSave selected = defaultState;

        switch (placement)
        {
            case RowPlacement.DefaultStateDefaultSelected:
                break;
            case RowPlacement.DefaultStateUnrelatedCategorySelected:
                selected = AddCategoryState(chain, "Cat", "S", s => SetNumber(s, "Unrelated.Width", 1f));
                break;
            case RowPlacement.UncategorizedStateRows:
                selected = AddUncategorizedState(chain, "Alt", _ => { });
                break;
            case RowPlacement.CategoryStateRows:
            case RowPlacement.SplitBetweenDefaultAndCategoryState:
                selected = AddCategoryState(chain, "Cat", "S", _ => { });
                break;
        }

        for (int i = 0; i < rowsInWrittenOrder.Length; i++)
        {
            bool inDefault = placement is RowPlacement.DefaultStateDefaultSelected or RowPlacement.DefaultStateUnrelatedCategorySelected
                || (placement == RowPlacement.SplitBetweenDefaultAndCategoryState && i % 2 == 0);
            AddReference(inDefault ? defaultState : selected, rowsInWrittenOrder[i].Owner, rowsInWrittenOrder[i].Line);
        }

        return selected;
    }

    private static void ShouldBeWidths(GraphicalUiElement root, string tag, params (string Name, float Width)[] expected)
    {
        foreach ((string name, float width) in expected)
        {
            WidthOfChild(root, name).ShouldBe(width, tag + " " + name);
        }
    }

    [Theory]
    [MemberData(nameof(ChainCases))]
    public void RefreshAll_ChainOfRowsInAnyWrittenOrderAndPlacement_EvaluatesInDependencyOrder(
        RowPlacement placement, int depth, string order)
    {
        ComponentSave chain = BuildRowlessChain();
        BuildProject(chain);
        (string Owner, string Line)[] rows = ChainRows(depth);
        (string Owner, string Line)[] written = order.Split(',').Select(i => rows[int.Parse(i)]).ToArray();
        StateSave selected = PlaceRows(chain, placement, written);

        GraphicalUiElement root = Show(chain, selected);

        string tag = $"{placement} depth {depth} order {order}";
        if (depth == 2)
        {
            ShouldBeWidths(root, tag, ("A", 101f), ("B", 100f), ("R", 50f));
        }
        else
        {
            ShouldBeWidths(root, tag, ("A", 111f), ("B", 110f), ("C", 55f), ("R", 50f));
        }
    }

    /// <summary>The selected state sets what the head of the chain reads from: every row follows it.</summary>
    [Theory]
    [MemberData(nameof(ChainCases))]
    public void RefreshAll_SelectedStateSetsInputOfChain_EveryRowFollows(RowPlacement placement, int depth, string order)
    {
        ComponentSave chain = BuildRowlessChain();
        BuildProject(chain);
        (string Owner, string Line)[] rows = ChainRows(depth);
        (string Owner, string Line)[] written = order.Split(',').Select(i => rows[int.Parse(i)]).ToArray();
        StateSave selected = PlaceRows(chain, placement, written);
        if (selected == chain.DefaultState)
        {
            selected = AddCategoryState(chain, "Cat2", "S2", _ => { });
        }
        SetNumber(selected, "R.Width", 60f);

        GraphicalUiElement root = Show(chain, selected);

        string tag = $"{placement} depth {depth} order {order}";
        if (depth == 2)
        {
            ShouldBeWidths(root, tag, ("A", 121f), ("B", 120f), ("R", 60f));
        }
        else
        {
            ShouldBeWidths(root, tag, ("A", 131f), ("B", 130f), ("C", 65f), ("R", 60f));
        }
    }

    /// <summary>A row reading another component's variable (<c>Styles.Primary.Width</c>) feeds a row written before it.</summary>
    [Fact]
    public void RefreshAll_ChainReadsAnotherComponentsVariable_EvaluatesInDependencyOrder()
    {
        ComponentSave styles = BuildStyles();
        SetNumber(styles.DefaultState!, "Primary.Width", 70f);
        ComponentSave chain = BuildRowlessChain();
        BuildProject(styles, chain);
        AddReference(chain.DefaultState!, "A", "Width = B.Width + 1");
        AddReference(chain.DefaultState!, "B", "Width = Components/Styles.Primary.Width * 2");

        GraphicalUiElement root = Show(chain, chain.DefaultState!);

        ShouldBeWidths(root, "cross-element", ("A", 141f), ("B", 140f));
    }

    /// <summary>The same chain through a categorized state, with the cross-component row on the state.</summary>
    [Fact]
    public void RefreshAll_ChainReadsAnotherComponentsVariableInSelectedState_EvaluatesInDependencyOrder()
    {
        ComponentSave styles = BuildStyles();
        SetNumber(styles.DefaultState!, "Primary.Width", 70f);
        ComponentSave chain = BuildRowlessChain();
        BuildProject(styles, chain);
        StateSave selected = AddCategoryState(chain, "Cat", "S", s =>
        {
            AddReference(s, "A", "Width = B.Width + 1");
            AddReference(s, "B", "Width = Components/Styles.Primary.Width * 2");
        });

        GraphicalUiElement root = Show(chain, selected);

        ShouldBeWidths(root, "cross-element selected", ("A", 141f), ("B", 140f));
    }

    /// <summary>The element's own row (no instance) feeds an instance row: a row on the component itself is part of the chain.</summary>
    [Fact]
    public void RefreshAll_ElementRowFeedsInstanceRowWrittenBeforeIt_EvaluatesInDependencyOrder()
    {
        ComponentSave chain = BuildRowlessChain();
        BuildProject(chain);
        SetNumber(chain.DefaultState!, "Width", 10f);
        AddReference(chain.DefaultState!, "A", "Width = Width + 1");
        AddReference(chain.DefaultState!, null, "Width = R.Width * 2");

        GraphicalUiElement root = Show(chain, chain.DefaultState!);

        root.Width.ShouldBe(100f);
        WidthOfChild(root, "A").ShouldBe(101f);
    }

    /// <summary>A derived component's row reads the result of a row its base component owns.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefreshAll_DerivedRowReadsBaseComponentRow_EvaluatesInDependencyOrder(bool derivedRowInSelectedState)
    {
        ComponentSave baseChain = BuildRowlessChain("BaseChain");
        AddReference(baseChain.DefaultState!, "B", "Width = R.Width * 2");
        ComponentSave derived = new ComponentSave { Name = "DerivedChain", BaseType = "BaseChain" };
        foreach (InstanceSave inherited in baseChain.Instances)
        {
            derived.Instances.Add(new InstanceSave { Name = inherited.Name, BaseType = inherited.BaseType, DefinedByBase = true, ParentContainer = derived });
        }
        StateSave derivedDefault = new StateSave { Name = "Default", ParentContainer = derived };
        derived.States.Add(derivedDefault);
        BuildProject(baseChain, derived);
        StateSave selected = derivedDefault;
        if (derivedRowInSelectedState)
        {
            selected = AddCategoryState(derived, "Cat", "S", s => AddReference(s, "A", "Width = B.Width + 1"));
        }
        else
        {
            AddReference(derivedDefault, "A", "Width = B.Width + 1");
        }

        GraphicalUiElement root = Show(derived, selected);

        ShouldBeWidths(root, "inherited", ("A", 101f), ("B", 100f));
    }

    /// <summary>The selected state sets what a base component's row writes: the state's value stays, and the derived row reading it follows.</summary>
    [Fact]
    public void RefreshAll_SelectedStateSetsTargetOfBaseComponentRow_StateWinsAndDerivedRowFollows()
    {
        ComponentSave baseChain = BuildRowlessChain("BaseChain");
        AddReference(baseChain.DefaultState!, "B", "Width = R.Width * 2");
        ComponentSave derived = new ComponentSave { Name = "DerivedChain", BaseType = "BaseChain" };
        foreach (InstanceSave inherited in baseChain.Instances)
        {
            derived.Instances.Add(new InstanceSave { Name = inherited.Name, BaseType = inherited.BaseType, DefinedByBase = true, ParentContainer = derived });
        }
        StateSave derivedDefault = new StateSave { Name = "Default", ParentContainer = derived };
        derived.States.Add(derivedDefault);
        BuildProject(baseChain, derived);
        AddReference(derivedDefault, "A", "Width = B.Width + 1");
        StateSave selected = AddCategoryState(derived, "Cat", "S", s => SetNumber(s, "B.Width", 77f));

        GraphicalUiElement root = Show(derived, selected);

        ShouldBeWidths(root, "inherited overridden", ("A", 78f), ("B", 77f));
    }

    /// <summary>
    /// A cycle terminates: its rows are skipped and keep their stored values, the same result the ElementSave
    /// path gives, so applying again changes nothing (#6871).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefreshAll_RowsReadEachOther_KeepTheirStoredValues(bool inSelectedState)
    {
        ComponentSave chain = BuildRowlessChain();
        BuildProject(chain);
        StateSave selected = inSelectedState ? AddCategoryState(chain, "Cat", "S", _ => { }) : chain.DefaultState!;
        AddReference(selected, "A", "Width = B.Width + 1");
        AddReference(selected, "B", "Width = A.Width + 1");

        (float a, float b) = OracleWidths(chain, selected, "A", "B");
        GraphicalUiElement root = Show(chain, selected);

        (a, b).ShouldBe((10f, 120f), "the stored values; a cycle is not evaluated");
        (WidthOfChild(root, "A"), WidthOfChild(root, "B")).ShouldBe((a, b));
    }

    [Theory]
    [InlineData("ThreeRowCycle")]
    [InlineData("SelfRead")]
    public void RefreshAll_ThreeRowCycleOrARowReadingItself_TerminatesAndKeepsTheStoredValues(string shape)
    {
        ComponentSave chain = BuildRowlessChain();
        BuildProject(chain);
        if (shape == "ThreeRowCycle")
        {
            AddReference(chain.DefaultState!, "A", "Width = B.Width + 1");
            AddReference(chain.DefaultState!, "B", "Width = C.Width + 1");
            AddReference(chain.DefaultState!, "C", "Width = A.Width + 1");
        }
        else
        {
            AddReference(chain.DefaultState!, "A", "Width = A.Width + 1");
        }

        GraphicalUiElement? root = null;
        System.Threading.Tasks.Task shown = System.Threading.Tasks.Task.Run(() => root = Show(chain, chain.DefaultState!));
        shown.Wait(5000).ShouldBeTrue("RefreshAll froze (#6871)");
        shown.GetAwaiter().GetResult();

        (WidthOfChild(root!, "A"), WidthOfChild(root!, "B"), WidthOfChild(root!, "C")).ShouldBe((10f, 120f, 30f));
    }

    /// <summary>What the ElementSave path (dependency order, each result written where the next row reads it) gives for the state shown.</summary>
    private static (float, float) OracleWidths(ComponentSave element, StateSave selected, string first, string second)
    {
        Dictionary<string, float> values = OracleValues(element, selected, new[] { first + ".Width", second + ".Width" });
        return (values[first + ".Width"], values[second + ".Width"]);
    }

    private static Dictionary<string, float> OracleValues(ComponentSave element, StateSave selected, IEnumerable<string> names)
    {
        StateSave defaultState = element.DefaultState!;
        StateSave evaluation = selected == defaultState
            ? defaultState.Clone()
            : new SelectedStateReferenceMerger().Merge(defaultState, selected);

        ElementSaveExtensions.ApplyVariableReferences(element, evaluation, liveRoot: null, isFullCommit: true, notifyChanges: false);

        RecursiveVariableFinder finder = new RecursiveVariableFinder(evaluation);
        return names.ToDictionary(name => name, name => Convert.ToSingle(finder.GetValue(name)));
    }

    #endregion

    #region Seeded sweep

    private static float HeightOfChild(GraphicalUiElement root, string name) => FindByName(root, name).Height;

    /// <summary>
    /// Random acyclic row sets (rows on Width and Height of five instances, each reading an earlier instance),
    /// written in random order, split at random between the default state and the selected state, with the
    /// selected state setting random rowless variables. The canvas must equal the ElementSave path's result,
    /// and for the default state the instance a running game creates.
    /// </summary>
    [Fact]
    public void RefreshAll_RandomAcyclicRowSets_MatchDependencyOrderOracle()
    {
        const int nodeCount = 5;
        string[] variables = { "Width", "Height" };
        Random random = new Random(6856);
        int cases = MonoGameGum.Tests.SweepScale.Cases(300);

        for (int caseIndex = 0; caseIndex < cases; caseIndex++)
        {
            ComponentSave chain = new ComponentSave { Name = "Sweep", BaseType = "Container" };
            StateSave defaultState = new StateSave { Name = "Default", ParentContainer = chain };
            chain.States.Add(defaultState);
            for (int i = 0; i < nodeCount; i++)
            {
                chain.Instances.Add(new InstanceSave { Name = "N" + i, BaseType = "Rectangle", ParentContainer = chain });
                SetNumber(defaultState, $"N{i}.Width", 10f * (i + 1));
                SetNumber(defaultState, $"N{i}.Height", 5f * (i + 1));
            }
            BuildProject(chain);

            int kind = random.Next(3);
            StateSave selected = kind switch
            {
                0 => defaultState,
                1 => AddUncategorizedState(chain, "Alt", _ => { }),
                _ => AddCategoryState(chain, "Cat", "S", _ => { }),
            };

            List<(int Target, string Variable, string Line)> rows = new List<(int, string, string)>();
            for (int i = 1; i < nodeCount; i++)
            {
                foreach (string variable in variables)
                {
                    if (random.Next(2) == 0)
                    {
                        rows.Add((i, variable, $"{variable} = N{random.Next(i)}.{variables[random.Next(2)]} + {random.Next(10)}"));
                    }
                }
            }
            rows = rows.OrderBy(_ => random.Next()).ToList();

            HashSet<string> hasRow = rows.Select(row => $"N{row.Target}.{row.Variable}").ToHashSet();
            foreach ((int target, _, string line) in rows)
            {
                bool inSelected = selected != defaultState && random.Next(2) == 0;
                AddReference(inSelected ? selected : defaultState, "N" + target, line);
            }

            if (selected != defaultState)
            {
                for (int i = 0; i < nodeCount; i++)
                {
                    foreach (string variable in variables)
                    {
                        if (!hasRow.Contains($"N{i}.{variable}") && random.Next(10) < 3)
                        {
                            SetNumber(selected, $"N{i}.{variable}", 500f + i);
                        }
                    }
                }
            }

            string[] names = Enumerable.Range(0, nodeCount).SelectMany(i => variables.Select(v => $"N{i}.{v}")).ToArray();
            Dictionary<string, float> expected = OracleValues(chain, selected, names);
            GraphicalUiElement root = Show(chain, selected);

            string tag = $"case {caseIndex} kind {kind}: " + string.Join(" | ", rows.Select(row => $"N{row.Target}.{row.Line}"));
            foreach (string name in names)
            {
                string[] parts = name.Split('.');
                float actual = parts[1] == "Width" ? WidthOfChild(root, parts[0]) : HeightOfChild(root, parts[0]);
                actual.ShouldBe(expected[name], tag + " " + name);
            }

            if (kind == 0)
            {
                GraphicalUiElement game = chain.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);
                foreach (string name in names)
                {
                    string[] parts = name.Split('.');
                    float actual = parts[1] == "Width" ? WidthOfChild(game, parts[0]) : HeightOfChild(game, parts[0]);
                    actual.ShouldBe(expected[name], "running game, " + tag + " " + name);
                }
            }
        }
    }

    #endregion
}
