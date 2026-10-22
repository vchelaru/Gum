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
/// A reference whose value is a NaN or infinity that a float or double target cannot take is reported once per row per
/// owner and the target keeps its value (#6457), in every build configuration, with and without Gum.Expressions. Before,
/// the setter's own check threw out of creation (FULL_DIAGNOSTICS) or the target silently took the value.
/// </summary>
public class ReferenceRejectedByTargetTests : BaseTestClass
{
    private readonly StaticRegistrationScope _registrations = new();
    private readonly List<string> _errors = new();

    public ReferenceRejectedByTargetTests()
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
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    public static IEnumerable<float> NonFiniteValues => new[] { float.PositiveInfinity, float.NegativeInfinity, float.NaN };

    // Every float target whose setter checks the value (X, Y, Width, Height, Rotation of any element, FontScale of a
    // Text) and Z, which no standard element defines and whose type comes from the element's own declaration, each with the value it has before the reference, and the source variable a Container hands it: StackSpacing
    // accepts any float, so the source itself never throws.
    public static TheoryData<bool, string, float, float> NonFiniteIntoCheckedTarget()
    {
        TheoryData<bool, string, float, float> data = new();
        foreach (bool useExpressions in new[] { false, true })
        {
            foreach (string target in new[] { "X", "Y", "Width", "Height", "Rotation", "FontScale", "Z" })
            {
                foreach (float value in NonFiniteValues)
                {
                    data.Add(useExpressions, target, 77f, value);
                }
            }
        }
        return data;
    }

    private GraphicalUiElement CreateTarget(bool useExpressions, string target, float authoredTarget, float sourceValue,
        out GraphicalUiElement source)
    {
        // FontScale exists only on a Text; everything else is checked on a Container.
        string targetBase = target == "FontScale" ? "Text" : "Container";
        ComponentSave pair = ReferencePairBuilder.Build($"{target} = Source.StackSpacing", useExpressions,
            new (string, object?)[] { (target, authoredTarget), ("StackSpacing", sourceValue) },
            targetBaseType: targetBase, sourceBaseType: "Container");
        GraphicalUiElement root = pair.ToGraphicalUiElement();
        source = root.GetGraphicalUiElementByName("Source")!;
        return root.GetGraphicalUiElementByName("Target")!;
    }

    private static float Read(GraphicalUiElement target, string name) => name switch
    {
        "X" => target.X,
        "Y" => target.Y,
        "Width" => target.Width,
        "Height" => target.Height,
        "Rotation" => target.Rotation,
        "Z" => target.Z,
        "FontScale" => ((TextRuntime)target).FontScale,
        _ => throw new ArgumentException(name)
    };

    [Theory]
    [MemberData(nameof(NonFiniteIntoCheckedTarget))]
    public void Create_NonFiniteValueIntoTargetThatRejectsIt_KeepsTheValueAndReportsOnce(
        bool useExpressions, string target, float authored, float sourceValue)
    {
        GraphicalUiElement element = CreateTarget(useExpressions, target, authored, sourceValue, out _);

        Read(element, target).ShouldBe(authored);
        _errors.Count.ShouldBe(1, string.Join(Environment.NewLine, _errors));
        _errors[0].ShouldContain($"Variable reference '{target} = Source.StackSpacing' was not applied in Pair (Container)");
        _errors[0].ShouldContain($"Target.{target} keeps its current value.");
    }

    [Theory]
    [MemberData(nameof(NonFiniteIntoCheckedTarget))]
    public void LiveUpdate_NonFiniteValueIntoTargetThatRejectsIt_KeepsThePreviousValueAndReportsOnce(
        bool useExpressions, string target, float authored, float nonFinite)
    {
        GraphicalUiElement element = CreateTarget(useExpressions, target, authored, 5f, out GraphicalUiElement source);
        Read(element, target).ShouldBe(5f);

        for (int frame = 0; frame < 300; frame++)
        {
            source.SetProperty("StackSpacing", frame % 2 == 0 ? nonFinite : float.PositiveInfinity);
        }

        Read(element, target).ShouldBe(5f);
        _errors.Count.ShouldBe(1, string.Join(Environment.NewLine, _errors));

        // The reference recovers as soon as the value is usable again.
        source.SetProperty("StackSpacing", 9f);
        Read(element, target).ShouldBe(9f);
        _errors.Count.ShouldBe(1);
    }

    // A nullable float takes infinity, on the live path too, where the state does not carry the declared type.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiveUpdate_InfinityIntoNullableFloatDeclaredOnTheElement_Applies(bool useExpressions)
    {
        ComponentSave pair = ReferencePairBuilder.Build("Z = Source.StackSpacing", useExpressions,
            new (string, object?)[] { ("Z", 77f), ("StackSpacing", 5f) }, targetType: "float?",
            targetBaseType: "Container", sourceBaseType: "Container");
        GraphicalUiElement root = pair.ToGraphicalUiElement();
        GraphicalUiElement source = root.GetGraphicalUiElementByName("Source")!;
        GraphicalUiElement target = root.GetGraphicalUiElementByName("Target")!;

        source.SetProperty("StackSpacing", float.PositiveInfinity);

        target.Z.ShouldBe(float.PositiveInfinity);
        _errors.ShouldBeEmpty();
    }

    // A custom float variable of a component (a property of its generated class) is typed by the component, not by a
    // standard element, and rejects a NaN or infinity live like any float.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiveUpdate_InfinityIntoCustomFloatVariable_KeepsThePreviousValueAndReportsOnce(bool useExpressions)
    {
        ComponentSave pair = ReferencePairBuilder.Build("Progress = Source.StackSpacing", useExpressions,
            new (string, object?)[] { ("Progress", 77f), ("StackSpacing", 5f) },
            targetBaseType: "Slider", sourceBaseType: "Container");
        ComponentSave slider = new ComponentSave { Name = "Slider", BaseType = "Container" };
        StateSave sliderState = new StateSave { Name = "Default", ParentContainer = slider };
        sliderState.Variables.Add(new VariableSave { Name = "Progress", Type = "float", Value = 0f, IsCustomVariable = true });
        slider.States.Add(sliderState);
        ObjectFinder.Self.GumProjectSave!.Components.Add(slider);
        ElementSaveExtensions.RegisterGueInstantiationType("Slider", typeof(SliderRuntime));
        GraphicalUiElement root = pair.ToGraphicalUiElement();
        GraphicalUiElement source = root.GetGraphicalUiElementByName("Source")!;
        SliderRuntime target = (SliderRuntime)root.GetGraphicalUiElementByName("Target")!;
        target.Progress.ShouldBe(5f);

        for (int frame = 0; frame < 300; frame++)
        {
            source.SetProperty("StackSpacing", frame % 2 == 0 ? float.PositiveInfinity : float.NaN);
        }

        target.Progress.ShouldBe(5f);
        _errors.Count.ShouldBe(1, string.Join(Environment.NewLine, _errors));
        source.SetProperty("StackSpacing", 9f);
        target.Progress.ShouldBe(9f);
    }

    private class SliderRuntime : GraphicalUiElement
    {
        public SliderRuntime() : base(new InvisibleRenderable(), null) { }

        public float Progress { get; set; }
    }

    [Fact]
    public void Create_TheSameBrokenComponentTwice_EachNewInstanceReportsOnceForItself()
    {
        CreateTarget(useExpressions: false, "Rotation", 77f, float.PositiveInfinity, out _);
        ComponentSave pair = (ComponentSave)ObjectFinder.Self.GumProjectSave!.Components[0];

        _errors.Count.ShouldBe(1);
        pair.ToGraphicalUiElement();

        _errors.Count.ShouldBe(2);
    }

    [Fact]
    public void Create_InfinityMessage_NamesTheValueAndTheTarget()
    {
        CreateTarget(useExpressions: false, "Rotation", 77f, float.PositiveInfinity, out _);

        _errors.ShouldBe(new[]
        {
            "Variable reference 'Rotation = Source.StackSpacing' was not applied in Pair (Container): " +
            "Source.StackSpacing is Infinity, which does not fit the float Target.Rotation. Target.Rotation keeps its current value."
        });
    }

    // The same pairs at the state level (ApplyAllVariableReferences writes hard values into the state before any UI
    // exists): float or double targets keep their value, a nullable float takes any value (infinity is a usable maximum),
    // and every finite value applies exactly as before.
    public static TheoryData<bool, string, string, double, bool> StateLevel()
    {
        TheoryData<bool, string, string, double, bool> data = new();
        foreach (double value in new[] { double.PositiveInfinity, double.NegativeInfinity, double.NaN })
        {
            // Without the package only a same-typed pair applies at all; with it the cast converts between the numbers.
            data.Add(false, "float", "float", value, true);
            data.Add(false, "double", "double", value, true);
            data.Add(true, "float", "float", value, true);
            data.Add(true, "float", "double", value, true);
            data.Add(true, "double", "float", value, true);
            data.Add(true, "double", "double", value, true);
            data.Add(true, "float", "float?", value, false);
            data.Add(false, "float", "float?", value, false);
        }
        foreach (double value in new[] { 0.0, -3.5, 1234.25 })
        {
            data.Add(false, "float", "float", value, false);
            data.Add(false, "double", "double", value, false);
            data.Add(true, "float", "float", value, false);
            data.Add(true, "float", "double", value, false);
            data.Add(true, "double", "float", value, false);
            data.Add(true, "double", "double", value, false);
            data.Add(true, "double", "float?", value, false);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(StateLevel))]
    public void ApplyAllVariableReferences_NonFiniteNumber_IsReportedOnlyForFloatAndDoubleTargets(
        bool useExpressions, string sourceType, string targetType, double value, bool rejected)
    {
        object sourceValue = sourceType == "float" ? (object)(float)value : value;
        object old = targetType == "double" ? (object)7.0 : 7f;
        ComponentSave pair = ReferencePairBuilder.Build("Out = Source.In", useExpressions,
            new (string, object?)[] { ("Out", old), ("In", sourceValue) }, targetType, sourceType);

        ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences();

        object? result = pair.DefaultState!.GetValue("Target.Out");
        if (rejected)
        {
            result.ShouldBe(old);
            _errors.Count.ShouldBe(1, string.Join(Environment.NewLine, _errors));
        }
        else
        {
            Equals(result, old).ShouldBeFalse($"{sourceType} {value} into {targetType} should apply");
            _errors.ShouldBeEmpty();
        }
    }

    // Unchanged behavior on every value the targets accept: a few hundred values from a fixed seed land exactly as
    // written, at creation and live, and nothing is reported.
    [Fact]
    public void CreateAndLiveUpdate_RandomFiniteValuesIntoEveryCheckedTarget_ApplyWithoutReporting()
    {
        Random random = new Random(6457);
        string[] targets = { "X", "Y", "Width", "Height", "Rotation", "FontScale", "Z" };
        for (int i = 0; i < 300; i++)
        {
            string target = targets[i % targets.Length];
            bool useExpressions = random.Next(2) == 0;
            float created = (float)Math.Round(random.NextDouble() * 20000 - 10000, 2);
            float live = (float)Math.Round(random.NextDouble() * 20000 - 10000, 2);
            // FontScale takes any finite value and keeps it only from zero up.
            if (target == "FontScale")
            {
                created = Math.Abs(created);
                live = Math.Abs(live);
            }

            GraphicalUiElement element = CreateTarget(useExpressions, target, 77f, created, out GraphicalUiElement source);
            Read(element, target).ShouldBe(created, $"{target} at creation");
            source.SetProperty("StackSpacing", live);
            Read(element, target).ShouldBe(live, $"{target} live");
        }

        _errors.ShouldBeEmpty();
    }
}
