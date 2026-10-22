using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
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
/// A live read of FontSize for a reference is an int only when the float is a whole number inside the int range;
/// anything else (infinity, NaN, a fraction, a whole number the int cannot hold) stays a float (#6452).
/// </summary>
public class FontSizeOutOfIntRangeReferenceTests : BaseTestClass
{
    // The largest float below 2^31, the biggest whole float an int holds. int.MaxValue itself rounds up to 2^31.
    private const float LargestWholeFloatInIntRange = 2147483520f;

    public FontSizeOutOfIntRangeReferenceTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(1f, 1)]
    [InlineData(24f, 24)]
    [InlineData(-5f, -5)]
    [InlineData(LargestWholeFloatInIntRange, 2147483520)]
    [InlineData(-2147483648f, int.MinValue)]
    public void TryGetPropertyExact_WholeFontSizeInIntRange_IsAnInt(float fontSize, int expected)
    {
        object? value = ReadExact(fontSize);

        value.ShouldBeOfType<int>().ShouldBe(expected);
    }

    [Theory]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.NaN)]
    [InlineData(1e10f)]
    [InlineData(-1e10f)]
    [InlineData(2147483648f)] // (float)int.MaxValue, which is 2^31 and one past the int range
    [InlineData(-2147483904f)] // the next float below int.MinValue
    [InlineData(7.5f)]
    [InlineData(-0.25f)]
    public void TryGetPropertyExact_FontSizeAnIntCannotHold_StaysAFloat(float fontSize)
    {
        object? value = ReadExact(fontSize);

        value.ShouldBeOfType<float>().ShouldBe(fontSize);
    }

    [Theory]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(1e10f)]
    public void SourceFontSizeChanged_OutOfIntRangeIntoFontSize_KeepsTheFloatWithoutExpressions(float fontSize)
    {
        (TextRuntime source, TextRuntime target) = Create("FontSize = Source.FontSize");

        source.FontSize = fontSize;

        target.FontSize.ShouldBe(fontSize);
    }

    [Fact]
    public void SourceFontSizeChanged_BeyondIntRangeIntoRotation_KeepsTheFloatWithExpressions()
    {
        GumExpressionService.Initialize();
        (TextRuntime source, TextRuntime target) = Create("Rotation = Source.FontSize");

        source.FontSize = 1e10f;

        target.Rotation.ShouldBe(1e10f);
    }

    [Theory]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void SourceFontSizeChanged_InfinityIntoRotation_ReportsTheReferenceAndKeepsTheRotationWithExpressions(float fontSize)
    {
        GumExpressionService.Initialize();
        List<string> errors = new List<string>();
        void Record(string message) { if (message.StartsWith("Variable reference")) errors.Add(message); }
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Record;
        try
        {
            (TextRuntime source, TextRuntime target) = Create("Rotation = Source.FontSize");

            source.FontSize = fontSize;

            target.Rotation.ShouldBe(0f);
            errors.Count.ShouldBe(1, string.Join(Environment.NewLine, errors));
            errors[0].ShouldContain("does not fit the float Target.Rotation");
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Record;
        }
    }

    private static object? ReadExact(float fontSize)
    {
        TextRuntime text = new TextRuntime();
        text.FontSize = fontSize;

        text.TryGetPropertyExact("FontSize", out object? value).ShouldBeTrue();
        return value;
    }

    private static (TextRuntime Source, TextRuntime Target) Create(string targetRow)
    {
        ComponentSave card = new ComponentSave { Name = "Card", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = card };
        card.States.Add(state);
        foreach (string name in new[] { "Source", "Target" })
        {
            card.Instances.Add(new InstanceSave { Name = name, BaseType = "Text", ParentContainer = card });
        }
        state.Variables.Add(new VariableSave { Name = "Source.FontSize", Type = "int", Value = 2, SetsValue = true });

        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "Target.VariableReferences" };
        list.Value.Add(targetRow);
        state.VariableLists.Add(list);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(card);
        foreach (string standard in new[] { "Container", "Text" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!);
            standardElement.DefaultState!.ParentContainer = standardElement;
            project.StandardElements.Add(standardElement);
        }
        ObjectFinder.Self.GumProjectSave = project;
        ElementSaveExtensions.RegisterGueInstantiation("Text", () => new TextRuntime());
        GraphicalUiElement root = card.ToGraphicalUiElement();
        return ((TextRuntime)root.GetGraphicalUiElementByName("Source")!, (TextRuntime)root.GetGraphicalUiElementByName("Target")!);
    }
}
