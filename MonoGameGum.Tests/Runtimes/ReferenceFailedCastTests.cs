using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum.TestsCommon;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// With Gum.Expressions a reference whose value cannot be cast to the target's type is reported once per row per
/// owner, like the same reference without the package, and the variable keeps its value (#6463). References the cast
/// can apply never report.
/// </summary>
public class ReferenceFailedCastTests : BaseTestClass
{
    private readonly StaticRegistrationScope _registrations = new();
    private readonly List<string> _errors = new();

    public ReferenceFailedCastTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Record;
    }

    private void Record(string message)
    {
        if (message.StartsWith("Variable reference"))
        {
            _errors.Add(message);
        }
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= Record;
        _registrations.Dispose();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    public enum Outcome
    {
        /// <summary>The reference is applied and nothing is reported.</summary>
        Applies,
        /// <summary>The target keeps its value and the row is reported once.</summary>
        Reports,
        /// <summary>The source has no value: nothing to apply and nothing to report, with or without the package.</summary>
        Silent
    }

    private static readonly object? Missing = null;

    // Every source type (bool, string, int, float, double, enum, missing) into every target type (int, float, double,
    // string, bool, enum, nullable int, another enum). The target's old value is 7 / 7f / 7.0 / "old" / false / Left /
    // Top, so an applied value always differs from it.
    public static TheoryData<string, object?, string, object, Outcome, object?> Matrix()
    {
        TheoryData<string, object?, string, object, Outcome, object?> data = new();

        void Add(string source, object? sourceValue, string target, object old, Outcome outcome, object? expected = null)
            => data.Add(source, sourceValue, target, old, outcome, expected);

        // bool
        Add("bool", true, "int", 7, Outcome.Reports);
        Add("bool", true, "float", 7f, Outcome.Reports);
        Add("bool", true, "double", 7.0, Outcome.Reports);
        Add("bool", true, "string", "old", Outcome.Applies, "True");
        Add("bool", true, "bool", false, Outcome.Applies, true);
        Add("bool", true, "HorizontalAlignment", HorizontalAlignment.Left, Outcome.Reports);
        Add("bool", true, "int?", 7, Outcome.Reports);
        Add("bool", true, "VerticalAlignment", VerticalAlignment.Top, Outcome.Reports);

        // string
        Add("string", "Right", "int", 7, Outcome.Reports);
        Add("string", "Right", "float", 7f, Outcome.Reports);
        Add("string", "Right", "double", 7.0, Outcome.Reports);
        Add("string", "Right", "string", "old", Outcome.Applies, "Right");
        Add("string", "Right", "bool", false, Outcome.Reports);
        Add("string", "Right", "HorizontalAlignment", HorizontalAlignment.Left, Outcome.Applies, HorizontalAlignment.Right);
        Add("string", "Nonsense", "HorizontalAlignment", HorizontalAlignment.Left, Outcome.Reports);
        Add("string", "Right", "int?", 7, Outcome.Reports);
        Add("string", "Right", "VerticalAlignment", VerticalAlignment.Top, Outcome.Reports);

        // int
        Add("int", 2, "int", 7, Outcome.Applies, 2);
        Add("int", 2, "float", 7f, Outcome.Applies, 2f);
        Add("int", 2, "double", 7.0, Outcome.Applies, 2.0);
        Add("int", 2, "string", "old", Outcome.Applies, "2");
        Add("int", 2, "bool", false, Outcome.Reports);
        Add("int", 2, "HorizontalAlignment", HorizontalAlignment.Left, Outcome.Applies, HorizontalAlignment.Right);
        Add("int", 2, "int?", 7, Outcome.Applies, 2);
        Add("int", 2, "VerticalAlignment", VerticalAlignment.Top, Outcome.Applies, VerticalAlignment.Bottom);

        // float
        Add("float", 2.5f, "int", 7, Outcome.Applies, 2);
        Add("float", 2.5f, "float", 7f, Outcome.Applies, 2.5f);
        Add("float", 2.5f, "double", 7.0, Outcome.Applies, 2.5);
        Add("float", 2.5f, "string", "old", Outcome.Applies, 2.5f.ToString());
        Add("float", 2.5f, "bool", false, Outcome.Reports);
        Add("float", 2.5f, "HorizontalAlignment", HorizontalAlignment.Left, Outcome.Reports);
        Add("float", 2.5f, "int?", 7, Outcome.Applies, 2);
        Add("float", 2.5f, "VerticalAlignment", VerticalAlignment.Top, Outcome.Reports);

        // double
        Add("double", 2.5, "int", 7, Outcome.Applies, 2);
        Add("double", 2.5, "float", 7f, Outcome.Applies, 2.5f);
        Add("double", 2.5, "double", 7.0, Outcome.Applies, 2.5);
        Add("double", 2.5, "string", "old", Outcome.Applies, 2.5.ToString());
        Add("double", 2.5, "bool", false, Outcome.Reports);
        Add("double", 2.5, "HorizontalAlignment", HorizontalAlignment.Left, Outcome.Reports);
        Add("double", 2.5, "int?", 7, Outcome.Applies, 2);
        Add("double", 2.5, "VerticalAlignment", VerticalAlignment.Top, Outcome.Reports);

        // enum
        Add("HorizontalAlignment", HorizontalAlignment.Right, "int", 7, Outcome.Reports);
        Add("HorizontalAlignment", HorizontalAlignment.Right, "float", 7f, Outcome.Reports);
        Add("HorizontalAlignment", HorizontalAlignment.Right, "double", 7.0, Outcome.Reports);
        Add("HorizontalAlignment", HorizontalAlignment.Right, "string", "old", Outcome.Applies, "Right");
        Add("HorizontalAlignment", HorizontalAlignment.Right, "bool", false, Outcome.Reports);
        Add("HorizontalAlignment", HorizontalAlignment.Right, "HorizontalAlignment", HorizontalAlignment.Left, Outcome.Applies, HorizontalAlignment.Right);
        Add("HorizontalAlignment", HorizontalAlignment.Right, "int?", 7, Outcome.Reports);
        Add("HorizontalAlignment", HorizontalAlignment.Right, "VerticalAlignment", VerticalAlignment.Top, Outcome.Reports);

        // a state category (a type the cast does not know): the value reaches the setter as it does without the package,
        // which applies a text naming a state and ignores anything else
        foreach ((string source, object value) in new (string, object)[]
        {
            ("bool", true), ("string", "Pressed"), ("string", "Nonsense"), ("int", 2), ("float", 2.5f), ("double", 2.5),
            ("HorizontalAlignment", HorizontalAlignment.Right)
        })
        {
            Add(source, value, "ButtonCategory", "Enabled", Outcome.Applies, value);
        }

        // a variable with no value
        foreach ((string target, object old) in new (string, object)[]
        {
            ("int", 7), ("float", 7f), ("double", 7.0), ("string", "old"), ("bool", false),
            ("HorizontalAlignment", HorizontalAlignment.Left), ("int?", 7), ("ButtonCategory", "Enabled")
        })
        {
            Add("missing", Missing, target, old, Outcome.Silent);
        }

        return data;
    }

    // The matrix's source and target columns, for tests that decide the outcome themselves.
    public static TheoryData<string, object?, string, object> MatrixPairs()
    {
        TheoryData<string, object?, string, object> data = new();
        foreach (object[] row in Matrix())
        {
            data.Add((string)row[0], row[1], (string)row[2], row[3]);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public void ApplyAllVariableReferences_EverySourceIntoEveryTargetWithGumExpressions_AppliesOrKeepsTheValueAndReportsOnce(
        string source, object? sourceValue, string target, object old, Outcome outcome, object? expected)
    {
        ComponentSave pair = BuildMatrixPair(source, sourceValue, target, old, useExpressions: true);

        ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences();

        pair.DefaultState!.GetValue("Target.Out").ShouldBe(outcome == Outcome.Applies ? expected : old);
        _errors.Count.ShouldBe(outcome == Outcome.Reports ? 1 : 0, string.Join(Environment.NewLine, _errors));
    }

    [Fact]
    public void Create_BoolIntoNumberWithGumExpressions_KeepsTheValueAndReportsTheTypes()
    {
        GraphicalUiElement target = CreateTarget("Width = Source.IsBold", ("Width", 77f), ("IsBold", true));

        ((TextRuntime)target).Width.ShouldBe(77f);
        _errors.ShouldBe(new[]
        {
            "Variable reference 'Width = Source.IsBold' was not applied in Pair (Container): Source.IsBold is a bool and Target.Width is a float, " +
            "which cannot be converted. Target.Width keeps its current value."
        });
    }

    [Fact]
    public void Create_TextIntoNumberWithGumExpressions_KeepsTheValueAndReportsTheTypes()
    {
        GraphicalUiElement target = CreateTarget("FontSize = Source.Text", ("FontSize", 18), ("Text", "wide"));

        ((TextRuntime)target).FontSize.ShouldBe(18f);
        _errors.ShouldBe(new[]
        {
            "Variable reference 'FontSize = Source.Text' was not applied in Pair (Container): Source.Text is a string and Target.FontSize is an int, " +
            "which cannot be converted. Target.FontSize keeps its current value."
        });
    }

    [Fact]
    public void Create_EnumIntoNumberWithGumExpressions_KeepsTheValueAndReportsTheTypes()
    {
        GraphicalUiElement target = CreateTarget("Width = Source.HorizontalAlignment", ("Width", 77f), ("HorizontalAlignment", HorizontalAlignment.Right));

        ((TextRuntime)target).Width.ShouldBe(77f);
        _errors.ShouldBe(new[]
        {
            "Variable reference 'Width = Source.HorizontalAlignment' was not applied in Pair (Container): Source.HorizontalAlignment is a HorizontalAlignment " +
            "and Target.Width is a float, which cannot be converted. Target.Width keeps its current value."
        });
    }

    [Fact]
    public void Create_NameThatIsNotAnEnumValueWithGumExpressions_KeepsTheValueAndReportsTheName()
    {
        GraphicalUiElement target = CreateTarget("HorizontalAlignment = Source.Text", ("HorizontalAlignment", HorizontalAlignment.Left), ("Text", "Nonsense"));

        ((TextRuntime)target).HorizontalAlignment.ShouldBe(HorizontalAlignment.Left);
        _errors.ShouldBe(new[]
        {
            "Variable reference 'HorizontalAlignment = Source.Text' was not applied in Pair (Container): Source.Text is \"Nonsense\", " +
            "which is not a value of the HorizontalAlignment Target.HorizontalAlignment. Target.HorizontalAlignment keeps its current value."
        });
    }

    // The right side is an expression rather than a plain variable; its value is still named by its type.
    [Theory]
    [InlineData("Width = Source.IsBold && Source.IsBold", "Source.IsBold && Source.IsBold is a bool and Target.Width is a float")]
    [InlineData("Width = \"wide\"", "\"wide\" is a string and Target.Width is a float")]
    [InlineData("Width = Source.IsBold ? \"a\" : \"b\"", "Source.IsBold ? \"a\" : \"b\" is a string and Target.Width is a float")]
    [InlineData("Width = !Source.IsBold", "!Source.IsBold is a bool and Target.Width is a float")]
    public void Create_ExpressionWhoseValueCannotBeCastWithGumExpressions_KeepsTheValueAndReports(string row, string expectedFragment)
    {
        GraphicalUiElement target = CreateTarget(row, ("Width", 77f), ("IsBold", true));

        ((TextRuntime)target).Width.ShouldBe(77f);
        _errors.Count.ShouldBe(1);
        _errors[0].ShouldContain(expectedFragment);
    }

    [Fact]
    public void Create_ExpressionWhoseValueCanBeCastWithGumExpressions_Applies()
    {
        GraphicalUiElement target = CreateTarget("Width = Source.FontSize * 2 + 1", ("Width", 77f), ("FontSize", 24));

        ((TextRuntime)target).Width.ShouldBe(49f);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void LiveUpdate_BoolIntoNumberWithGumExpressions_ReportsOnceAndKeepsTheValue()
    {
        GraphicalUiElement target = CreateTarget("Width = Source.IsBold", ("Width", 77f), ("IsBold", true), out GraphicalUiElement source);

        for (int frame = 0; frame < 300; frame++)
        {
            source.SetProperty("IsBold", frame % 2 == 0);
        }

        ((TextRuntime)target).Width.ShouldBe(77f);
        _errors.Count.ShouldBe(1);
    }

    [Fact]
    public void Create_TheSameBrokenComponentTwice_EachNewInstanceReportsOnceForItself()
    {
        ComponentSave pair = ReferencePairBuilder.Build("Width = Source.IsBold", useExpressions: true, new (string, object?)[] { ("Width", 77f), ("IsBold", true) });

        pair.ToGraphicalUiElement();
        _errors.Count.ShouldBe(1);
        pair.ToGraphicalUiElement();

        _errors.Count.ShouldBe(2);
    }

    [Fact]
    public void ApplyAllVariableReferences_BrokenRow_ReportsOncePerCall()
    {
        ReferencePairBuilder.Build("Width = Source.IsBold", useExpressions: true, new (string, object?)[] { ("Width", 77f), ("IsBold", true) });
        GumProjectSave project = ObjectFinder.Self.GumProjectSave!;

        project.ApplyAllVariableReferences();
        _errors.Count.ShouldBe(1);
        project.ApplyAllVariableReferences();

        _errors.Count.ShouldBe(2);
    }

    // A reference to a variable that has no value or does not exist reports nothing, as it does without the package.
    [Fact]
    public void Create_ReferenceToAVariableThatDoesNotExistWithGumExpressions_DoesNotReport()
    {
        GraphicalUiElement target = CreateTarget("Width = Source.NoSuchVariable", ("Width", 77f), ("IsBold", true));

        ((TextRuntime)target).Width.ShouldBe(77f);
        _errors.ShouldBeEmpty();
    }

    // Unchanged behavior on every pair the cast can apply: a few hundred random values from a fixed seed land converted
    // exactly as the cast defines it, and nothing is reported.
    [Fact]
    public void ApplyAllVariableReferences_RandomValuesTheCastCanApplyWithGumExpressions_ApplyWithoutReporting()
    {
        Random random = new Random(6463);
        for (int i = 0; i < 300; i++)
        {
            int integer = random.Next(-100000, 100000);
            float single = (float)Math.Round(random.NextDouble() * 20000 - 10000, 2);
            double precise = Math.Round(random.NextDouble() * 20000 - 10000, 3);

            bool flag = random.Next(2) == 0;
            (string source, object sourceValue, string target, object old, object expected)[] cases =
            {
                ("int", integer, "float", 7f, (float)integer),
                ("int", integer, "double", 7.0, (double)integer),
                ("int", integer, "string", "old", integer.ToString()),
                ("int", integer, "int?", 7, integer),
                ("float", single, "int", 7, (int)single),
                ("float", single, "double", 7.0, (double)single),
                ("float", single, "string", "old", single.ToString()),
                ("double", precise, "float", 7f, (float)precise),
                ("double", precise, "int", 7, (int)precise),
                ("double", precise, "string", "old", precise.ToString()),
                ("bool", flag, "bool", !flag, flag),
            };

            (string source, object sourceValue, string target, object old, object expected) pick = cases[random.Next(cases.Length)];

            ComponentSave pair = BuildMatrixPair(pick.source, pick.sourceValue, pick.target, pick.old, useExpressions: true);
            ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences();

            pair.DefaultState!.GetValue("Target.Out").ShouldBe(pick.expected, $"{pick.source} {pick.sourceValue} into {pick.target}");
        }

        _errors.ShouldBeEmpty();
    }

    // The package never does worse than none: every pair that applies without it applies with it, with no report.
    [Theory]
    [MemberData(nameof(MatrixPairs))]
    public void ApplyAllVariableReferences_PairThatAppliesWithoutGumExpressions_AppliesWithGumExpressionsToo(
        string source, object? sourceValue, string target, object old)
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ComponentSave withoutPackage = BuildMatrixPair(source, sourceValue, target, old, useExpressions: false);
        ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences();
        bool appliedWithout = _errors.Count == 0 && !Equals(withoutPackage.DefaultState!.GetValue("Target.Out"), old);
        object? valueWithout = withoutPackage.DefaultState!.GetValue("Target.Out");
        _errors.Clear();

        ElementSaveExtensions.CustomEvaluateExpression = null;
        ComponentSave withPackage = BuildMatrixPair(source, sourceValue, target, old, useExpressions: true);
        ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences();

        if (appliedWithout)
        {
            _errors.ShouldBeEmpty();
            Equals(withPackage.DefaultState!.GetValue("Target.Out"), old).ShouldBeFalse($"{source} into {target} applied without the package ({valueWithout})");
        }
    }

    // A text that names no state of the category reaches the setter, which ignores it, with or without the package.
    [Fact]
    public void ApplyAllVariableReferences_TextNamingNoStateOfTheCategoryWithGumExpressions_BehavesAsWithoutThePackage()
    {
        ComponentSave pair = BuildMatrixPair("string", "Nonsense", "ButtonCategory", "Enabled", useExpressions: true);

        ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences();

        pair.DefaultState!.GetValue("Target.Out").ShouldBe("Nonsense");
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyAllVariableReferences_TargetWhoseTypeIsNotKnownWithGumExpressions_BehavesAsWithoutThePackage()
    {
        ReferencePairBuilder.Build("Mystery = Source.In", useExpressions: true, new (string, object?)[] { ("Out", 7), ("In", 2) });
        ComponentSave pair = ObjectFinder.Self.GumProjectSave!.Components[0];

        ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences();

        pair.DefaultState!.GetValue("Target.Mystery").ShouldBe(2);
        _errors.ShouldBeEmpty();
    }

    private GraphicalUiElement CreateTarget(string row, params (string Name, object? Value)[] authored)
        => CreateTarget(row, authored[0], authored[1], out _);

    private GraphicalUiElement CreateTarget(string row, (string Name, object? Value) target, (string Name, object? Value) source, out GraphicalUiElement sourceElement)
    {
        ComponentSave pair = ReferencePairBuilder.Build(row, useExpressions: true, new[] { target, source });
        GraphicalUiElement root = pair.ToGraphicalUiElement();
        sourceElement = root.GetGraphicalUiElementByName("Source")!;
        return root.GetGraphicalUiElementByName("Target")!;
    }

    // Target.Out and Source.In are variables the instances do not define, so the state alone decides their types.
    private static ComponentSave BuildMatrixPair(string sourceType, object? sourceValue, string targetType, object targetOld, bool useExpressions)
        => ReferencePairBuilder.Build("Out = Source.In", useExpressions,
            new (string, object?)[] { ("Out", targetOld), ("In", sourceValue) }, targetType, sourceType);
}
