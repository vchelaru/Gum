using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A variable-reference row is evaluated three ways: when an element loads, by the state-writing path
/// the tool and <c>ApplyAllVariableReferences</c> use, and by live re-evaluation after a variable
/// changes. They share the scanner, ordering and expression evaluator but apply results differently,
/// so each scenario here must come out the same on all three. Each runs with the expression evaluator
/// (<c>Gum.Expressions</c>) and without it, the situation Native AOT builds are in; scenarios that
/// need arithmetic or functions only run with it (ADR 0022, #5922).
/// </summary>
public class ReferenceEvaluationParityTests : BaseTestClass
{
    private sealed class Scenario
    {
        public string Name { get; init; } = "";

        /// <summary>True when a row uses arithmetic, functions or conditions, which need Gum.Expressions.</summary>
        public bool NeedsExpressions { get; init; }

        public (string Name, object Value)[] Variables { get; init; } = Array.Empty<(string, object)>();

        /// <summary>Instances of ColoredRectangle, by name.</summary>
        public string[] Instances { get; init; } = Array.Empty<string>();

        /// <summary>Rows by owner: "" for the element itself, otherwise an instance name.</summary>
        public (string Owner, string Row)[] Rows { get; init; } = Array.Empty<(string, string)>();

        /// <summary>Variables compared across the three paths, such as <c>Y</c> or <c>Ball.X</c>.</summary>
        public string[] Targets { get; init; } = Array.Empty<string>();

        public string ChangedVariable { get; init; } = "";

        public object ChangedValue { get; init; } = 0f;
    }

    private static readonly Scenario[] Scenarios =
    {
        new Scenario
        {
            Name = "ReadsACustomVariable",
            Variables = new (string, object)[] { ("Progress", 3f), ("Y", 0f) },
            Rows = new[] { ("", "Y = Progress") },
            Targets = new[] { "Y" },
            ChangedVariable = "Progress", ChangedValue = 8f
        },
        new Scenario
        {
            Name = "Arithmetic",
            NeedsExpressions = true,
            Variables = new (string, object)[] { ("Progress", 3f), ("Y", 0f) },
            Rows = new[] { ("", "Y = Progress * 10 + 1") },
            Targets = new[] { "Y" },
            ChangedVariable = "Progress", ChangedValue = 5f
        },
        new Scenario
        {
            Name = "ChainWrittenInReverse",
            NeedsExpressions = true,
            Variables = new (string, object)[] { ("Progress", 3f), ("Offset", 0f), ("Y", 0f) },
            Rows = new[] { ("", "Y = Offset + 1"), ("", "Offset = Progress * 10") },
            Targets = new[] { "Y" },
            ChangedVariable = "Progress", ChangedValue = 4f
        },
        new Scenario
        {
            Name = "ChainWithoutArithmetic",
            Variables = new (string, object)[] { ("Progress", 3f), ("Offset", 0f), ("Y", 0f), ("X", 0f) },
            Rows = new[] { ("", "X = Y"), ("", "Y = Offset"), ("", "Offset = Progress") },
            Targets = new[] { "X", "Y" },
            ChangedVariable = "Progress", ChangedValue = 9f
        },
        new Scenario
        {
            Name = "Function",
            NeedsExpressions = true,
            Variables = new (string, object)[] { ("Progress", 30f), ("Y", 0f) },
            Rows = new[] { ("", "Y = Sin(Progress) * 100") },
            Targets = new[] { "Y" },
            ChangedVariable = "Progress", ChangedValue = 90f
        },
        new Scenario
        {
            Name = "Condition",
            NeedsExpressions = true,
            Variables = new (string, object)[] { ("IsOn", true), ("Y", 0f) },
            Rows = new[] { ("", "Y = IsOn ? 10 : 20") },
            Targets = new[] { "Y" },
            ChangedVariable = "IsOn", ChangedValue = false
        },
        new Scenario
        {
            Name = "ReadsItsOwnElementThroughTheQualifiedName",
            Variables = new (string, object)[] { ("Progress", 3f), ("Y", 0f) },
            Rows = new[] { ("", "Y = Components/Wave.Progress") },
            Targets = new[] { "Y" },
            ChangedVariable = "Progress", ChangedValue = 6f
        },
        new Scenario
        {
            Name = "InstanceRowReadsTheElement",
            Variables = new (string, object)[] { ("Progress", 3f), ("Ball.X", 0f) },
            Instances = new[] { "Ball" },
            Rows = new[] { ("Ball", "X = Progress") },
            Targets = new[] { "Ball.X" },
            ChangedVariable = "Progress", ChangedValue = 7f
        },
        new Scenario
        {
            Name = "ElementRowReadsAnInstance",
            Variables = new (string, object)[] { ("Child.Width", 30f), ("Y", 0f) },
            Instances = new[] { "Child" },
            Rows = new[] { ("", "Y = Child.Width") },
            Targets = new[] { "Y" },
            ChangedVariable = "Child.Width", ChangedValue = 50f
        },
        new Scenario
        {
            Name = "InstanceRowsChainAcrossInstances",
            NeedsExpressions = true,
            Variables = new (string, object)[] { ("Progress", 10f), ("Ball.X", 0f), ("Bar.Width", 0f) },
            Instances = new[] { "Bar", "Ball" },
            Rows = new[] { ("Bar", "Width = Ball.X + 20"), ("Ball", "X = Progress * 6") },
            Targets = new[] { "Ball.X", "Bar.Width" },
            ChangedVariable = "Progress", ChangedValue = 50f
        },
        new Scenario
        {
            Name = "InstanceRowsChainAcrossInstancesWithoutArithmetic",
            Variables = new (string, object)[] { ("Progress", 10f), ("Ball.X", 0f), ("Bar.Width", 0f) },
            Instances = new[] { "Bar", "Ball" },
            Rows = new[] { ("Bar", "Width = Ball.X"), ("Ball", "X = Progress") },
            Targets = new[] { "Ball.X", "Bar.Width" },
            ChangedVariable = "Progress", ChangedValue = 50f
        },
        new Scenario
        {
            Name = "CollapsedColor",
            Variables = new (string, object)[]
            {
                ("Source.Red", 10), ("Source.Green", 20), ("Source.Blue", 30),
                ("Target.Red", 0), ("Target.Green", 0), ("Target.Blue", 0)
            },
            Instances = new[] { "Source", "Target" },
            Rows = new[] { ("Target", "Color = Source.Color") },
            Targets = new[] { "Target.Red", "Target.Green", "Target.Blue" },
            ChangedVariable = "Source.Red", ChangedValue = 200
        },
    };

    public static TheoryData<string, bool> ScenarioNamesWithAndWithoutExpressions()
    {
        TheoryData<string, bool> data = new TheoryData<string, bool>();
        foreach (Scenario scenario in Scenarios)
        {
            data.Add(scenario.Name, true);
            if (!scenario.NeedsExpressions)
            {
                data.Add(scenario.Name, false);
            }
        }
        return data;
    }

    // A name of this test's own, so registering a runtime type for it leaves "ColoredRectangle" alone.
    private const string ParityRectangleName = "ReferenceParityRectangle";

    public ReferenceEvaluationParityTests()
    {
        ElementSaveExtensions.RegisterGueInstantiation(ParityRectangleName, () => new ColoredRectangleRuntime());
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Theory]
    [MemberData(nameof(ScenarioNamesWithAndWithoutExpressions))]
    public void Load_MatchesTheStateWritingPath(string scenarioName, bool useExpressions)
    {
        Scenario scenario = Prepare(scenarioName, useExpressions);

        Dictionary<string, double> expected = EvaluateInState(scenario, authoredChange: false);
        Dictionary<string, double> actual = ReadLive(Load(scenario), scenario);

        actual.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(ScenarioNamesWithAndWithoutExpressions))]
    public void LiveReevaluation_AfterAVariableChanges_MatchesTheStateWritingPathWithThatValueAuthored(
        string scenarioName, bool useExpressions)
    {
        Scenario scenario = Prepare(scenarioName, useExpressions);
        GraphicalUiElement gue = Load(scenario);

        gue.SetProperty(scenario.ChangedVariable, scenario.ChangedValue);

        Dictionary<string, double> expected = EvaluateInState(scenario, authoredChange: true);
        ReadLive(gue, scenario).ShouldBe(expected);
    }

    private Scenario Prepare(string scenarioName, bool useExpressions)
    {
        StandardElementsManager.Self.Initialize();
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
        else
        {
            ElementSaveExtensions.CustomEvaluateExpression = null;
            ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        }
        return Scenarios.Single(item => item.Name == scenarioName);
    }

    // Applies the rows to the saved state, as the tool and ApplyAllVariableReferences do, and reads the targets back.
    private static Dictionary<string, double> EvaluateInState(Scenario scenario, bool authoredChange)
    {
        ComponentSave element = Build(scenario);
        if (authoredChange)
        {
            element.DefaultState!.SetValue(scenario.ChangedVariable, scenario.ChangedValue);
        }

        element.ApplyVariableReferences(element.DefaultState!);

        Dictionary<string, double> values = new Dictionary<string, double>();
        foreach (string target in scenario.Targets)
        {
            values[target] = Convert.ToDouble(element.DefaultState!.GetValue(target));
        }
        return values;
    }

    private static GraphicalUiElement Load(Scenario scenario) => Build(scenario).ToGraphicalUiElement();

    private static Dictionary<string, double> ReadLive(GraphicalUiElement gue, Scenario scenario)
    {
        Dictionary<string, double> values = new Dictionary<string, double>();
        foreach (string target in scenario.Targets)
        {
            GraphicalUiElement owner = gue;
            string property = target;
            int dot = target.IndexOf('.');
            if (dot >= 0)
            {
                owner = gue.GetGraphicalUiElementByName(target.Substring(0, dot))!;
                property = target.Substring(dot + 1);
            }

            owner.TryGetProperty(property, out object? value).ShouldBeTrue($"{target} should be readable");
            values[target] = Convert.ToDouble(value);
        }
        return values;
    }

    private static ComponentSave Build(Scenario scenario)
    {
        ComponentSave element = new ComponentSave { Name = "Wave", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);

        foreach (string instance in scenario.Instances)
        {
            element.Instances.Add(new InstanceSave { Name = instance, BaseType = ParityRectangleName, ParentContainer = element });
        }
        foreach ((string name, object value) in scenario.Variables)
        {
            state.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value,
                Type = value is int ? "int" : value is bool ? "bool" : "float",
                SetsValue = true
            });
        }

        foreach (IGrouping<string, (string Owner, string Row)> group in scenario.Rows.GroupBy(item => item.Owner))
        {
            VariableListSave<string> list = new VariableListSave<string>
            {
                Type = "string",
                Name = group.Key.Length == 0 ? "VariableReferences" : group.Key + ".VariableReferences"
            };
            list.Value.AddRange(group.Select(item => item.Row));
            state.VariableLists.Add(list);
        }

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(element);
        foreach ((string standardName, string defaultsFrom) in new[]
        {
            ("Container", "Container"),
            (ParityRectangleName, "ColoredRectangle")
        })
        {
            StandardElementSave standard = new StandardElementSave { Name = standardName };
            StateSave standardDefault = StandardElementsManager.Self.GetDefaultStateFor(defaultsFrom)!;
            standardDefault.ParentContainer = standard;
            standard.States.Add(standardDefault);
            project.StandardElements.Add(standard);
        }
        ObjectFinder.Self.GumProjectSave = project;
        return element;
    }
}
