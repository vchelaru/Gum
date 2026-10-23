using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// MinWidth, MinHeight, MaxWidth, MaxHeight and custom nullable floats take any value, a NaN or infinity included, from
/// a reference at creation, live and in ApplyAllVariableReferences, with and without Gum.Expressions, and none of them
/// throws (#6473). A bound that is not a finite number cannot limit the size, so it never makes the size infinite either;
/// before, a minimum of +infinity threw from MinHeight (FULL_DIAGNOSTICS) and made the width infinite for MinWidth.
/// </summary>
public class ReferenceNonFiniteMinMaxTests : BaseTestClass
{
    private readonly List<string> _errors = new();

    public ReferenceNonFiniteMinMaxTests()
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
        base.Dispose();
    }

    private static readonly string[] SizeBounds = { "MinWidth", "MinHeight", "MaxWidth", "MaxHeight" };
    private static readonly float[] NonFinite = { float.PositiveInfinity, float.NegativeInfinity, float.NaN };

    public static TheoryData<bool, string, float> SizeBoundCases()
    {
        TheoryData<bool, string, float> data = new();
        foreach (bool useExpressions in new[] { false, true })
        {
            foreach (string bound in SizeBounds)
            {
                foreach (float value in NonFinite)
                {
                    data.Add(useExpressions, bound, value);
                }
            }
        }
        return data;
    }

    public static TheoryData<string, float> AssignCases()
    {
        TheoryData<string, float> data = new();
        foreach (string bound in SizeBounds)
        {
            foreach (float value in NonFinite)
            {
                data.Add(bound, value);
            }
        }
        return data;
    }

    public static TheoryData<bool, float> CustomCases()
    {
        TheoryData<bool, float> data = new();
        foreach (bool useExpressions in new[] { false, true })
        {
            foreach (float value in NonFinite)
            {
                data.Add(useExpressions, value);
            }
        }
        return data;
    }

    private class SliderRuntime : GraphicalUiElement
    {
        public SliderRuntime() : base(new InvisibleRenderable(), null) { }

        public float? Progress { get; set; }
    }

    private static float? ReadBound(GraphicalUiElement element, string name) => name switch
    {
        "MinWidth" => element.MinWidth,
        "MinHeight" => element.MinHeight,
        "MaxWidth" => element.MaxWidth,
        "MaxHeight" => element.MaxHeight,
        _ => throw new ArgumentException(name)
    };

    private static void ShouldHold(float? actual, float expected, string context)
    {
        actual.ShouldNotBeNull(context);
        if (float.IsNaN(expected))
        {
            float.IsNaN(actual.Value).ShouldBeTrue($"{context}: {actual}");
        }
        else
        {
            actual.Value.ShouldBe(expected, context);
        }
    }

    // A bound that is not a number cannot limit anything, so the element is as large as with no bound.
    private static void ShouldBeUnlimited(GraphicalUiElement element, string context)
    {
        GraphicalUiElement unlimited = new ContainerRuntime();
        unlimited.UpdateLayout();
        element.AbsoluteWidth.ShouldBe(unlimited.AbsoluteWidth, context);
        element.AbsoluteHeight.ShouldBe(unlimited.AbsoluteHeight, context);
    }

    private GraphicalUiElement CreateTarget(bool useExpressions, string bound, float authored, float sourceValue,
        out GraphicalUiElement source)
    {
        ComponentSave pair = ReferencePairBuilder.Build($"{bound} = Source.StackSpacing", useExpressions,
            new (string, object?)[] { (bound, authored), ("StackSpacing", sourceValue) },
            targetBaseType: "Container", sourceBaseType: "Container");
        GraphicalUiElement root = pair.ToGraphicalUiElement();
        source = root.GetGraphicalUiElementByName("Source")!;
        return root.GetGraphicalUiElementByName("Target")!;
    }

    [Theory]
    [MemberData(nameof(SizeBoundCases))]
    public void Create_NonFiniteValueIntoSizeBound_AppliesWithoutThrowingOrReporting(bool useExpressions, string bound, float value)
    {
        GraphicalUiElement target = CreateTarget(useExpressions, bound, 77f, value, out _);

        ShouldHold(ReadBound(target, bound), value, $"{bound} = {value}");
        ShouldBeUnlimited(target, $"{bound} = {value}");
        _errors.ShouldBeEmpty(string.Join(Environment.NewLine, _errors));
    }

    [Theory]
    [MemberData(nameof(SizeBoundCases))]
    public void LiveUpdate_NonFiniteValueIntoSizeBound_AppliesWithoutThrowingOrReporting(bool useExpressions, string bound, float value)
    {
        GraphicalUiElement target = CreateTarget(useExpressions, bound, 77f, 5f, out GraphicalUiElement source);
        ReadBound(target, bound).ShouldBe(5f);

        for (int frame = 0; frame < 20; frame++)
        {
            source.SetProperty("StackSpacing", frame % 2 == 0 ? value : 5f);
        }
        source.SetProperty("StackSpacing", value);

        ShouldHold(ReadBound(target, bound), value, $"{bound} = {value}");
        ShouldBeUnlimited(target, $"{bound} = {value}");
        _errors.ShouldBeEmpty(string.Join(Environment.NewLine, _errors));

        source.SetProperty("StackSpacing", 9f);
        ReadBound(target, bound).ShouldBe(9f);
    }

    // The same values assigned from code, which is where a reference ends up.
    [Theory]
    [MemberData(nameof(AssignCases))]
    public void Assign_NonFiniteValueToSizeBound_DoesNotThrowAndKeepsTheSizeFinite(string bound, float value)
    {
        GraphicalUiElement element = new ContainerRuntime();

        element.SetProperty(bound, value);

        ShouldHold(ReadBound(element, bound), value, $"{bound} = {value}");
        ShouldBeUnlimited(element, $"{bound} = {value}");
    }

    [Theory]
    [MemberData(nameof(CustomCases))]
    public void CreateAndLiveUpdate_NonFiniteValueIntoCustomNullableFloat_AppliesWithoutReporting(bool useExpressions, float value)
    {
        ComponentSave pair = ReferencePairBuilder.Build("Progress = Source.StackSpacing", useExpressions,
            new (string, object?)[] { ("Progress", 77f), ("StackSpacing", value) }, targetType: "float?",
            targetBaseType: "Slider", sourceBaseType: "Container");
        ComponentSave slider = new ComponentSave { Name = "Slider", BaseType = "Container" };
        slider.States.Add(new StateSave { Name = "Default", ParentContainer = slider });
        ObjectFinder.Self.GumProjectSave!.Components.Add(slider);
        ElementSaveExtensions.RegisterGueInstantiationType("Slider", typeof(SliderRuntime));
        GraphicalUiElement root = pair.ToGraphicalUiElement();
        GraphicalUiElement source = root.GetGraphicalUiElementByName("Source")!;
        SliderRuntime target = (SliderRuntime)root.GetGraphicalUiElementByName("Target")!;

        ShouldHold(target.Progress, value, "creation");

        source.SetProperty("StackSpacing", 5f);
        target.Progress.ShouldBe(5f);
        source.SetProperty("StackSpacing", value);
        ShouldHold(target.Progress, value, "live");
        _errors.ShouldBeEmpty(string.Join(Environment.NewLine, _errors));
    }

    [Theory]
    [MemberData(nameof(SizeBoundCases))]
    public void ApplyAllVariableReferences_NonFiniteValueIntoSizeBound_WritesTheValueWithoutReporting(bool useExpressions, string bound, float value)
    {
        ComponentSave pair = ReferencePairBuilder.Build("Out = Source.In", useExpressions,
            new (string, object?)[] { ("Out", 7f), ("In", value) }, targetType: "float?", sourceType: "float");

        ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences();

        object? result = pair.DefaultState!.GetValue("Target.Out");
        result.ShouldBeOfType<float>();
        ShouldHold((float)result!, value, $"{bound} = {value}");
        _errors.ShouldBeEmpty(string.Join(Environment.NewLine, _errors));
    }

    // Finite bounds clamp exactly as before: a few hundred values from a fixed seed land where the old rule (above the
    // maximum goes to the maximum, then below the minimum goes to the minimum) puts them, at creation and live.
    [Fact]
    public void CreateAndLiveUpdate_RandomFiniteBounds_ClampExactlyAsBefore()
    {
        Random random = new Random(6473);
        for (int i = 0; i < 300; i++)
        {
            string bound = SizeBounds[i % SizeBounds.Length];
            bool useExpressions = random.Next(2) == 0;
            float created = (float)Math.Round(random.NextDouble() * 400 - 100, 1);
            float live = (float)Math.Round(random.NextDouble() * 400 - 100, 1);

            GraphicalUiElement target = CreateTarget(useExpressions, bound, 77f, created, out GraphicalUiElement source);
            SizeOn(target, bound).ShouldBe(ExpectedSize(bound, created), $"{bound} = {created} at creation");
            source.SetProperty("StackSpacing", live);
            SizeOn(target, bound).ShouldBe(ExpectedSize(bound, live), $"{bound} = {live} live");
        }

        _errors.ShouldBeEmpty();
    }

    private static float SizeOn(GraphicalUiElement target, string bound) =>
        bound.EndsWith("Width") ? target.AbsoluteWidth : target.AbsoluteHeight;

    // The default container is 150 in each direction.
    private static float ExpectedSize(string bound, float value)
    {
        float size = 150;
        if (bound.StartsWith("Max") && size > value)
        {
            size = value;
        }
        if (bound.StartsWith("Min") && size < value)
        {
            size = value;
        }
        return size;
    }
}
