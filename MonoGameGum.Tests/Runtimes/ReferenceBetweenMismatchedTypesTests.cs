using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
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
/// Without Gum.Expressions a reference between variables of different types keeps the target's old value and
/// reports an error through <see cref="ElementSaveExtensions.VariableReferenceError"/> instead of throwing at
/// creation (#6428). Variables of the same type, and the conversions the setters already make, are unchanged.
/// </summary>
public class ReferenceBetweenMismatchedTypesTests : BaseTestClass
{
    private readonly StaticRegistrationScope _registrations = new();
    private readonly List<string> _errors = new();

    public ReferenceBetweenMismatchedTypesTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ElementSaveExtensions.VariableReferenceError += _errors.Add;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.VariableReferenceError -= _errors.Add;
        _registrations.Dispose();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    // Each row: the target variable, its authored value (the old value the reference must keep), the source variable
    // and its authored value. Together they cover every way the types can differ.
    public static TheoryData<string, object, string, object> MismatchedPairs => new()
    {
        { "Width", 77f, "FontSize", 24 },            // int into float
        { "Red", 7, "Width", 150f },                  // float into int
        { "Width", 77f, "IsBold", true },             // bool into float
        { "Width", 77f, "Text", "wide" },             // string into float
        { "Text", "authored", "FontSize", 24 },       // int into string
        { "Visible", true, "FontSize", 24 },          // int into bool
        { "XOrigin", HorizontalAlignment.Center, "YOrigin", VerticalAlignment.Bottom }, // one enum into another
        { "HorizontalAlignment", HorizontalAlignment.Left, "Text", "Right" }, // string into enum
    };

    [Theory]
    [MemberData(nameof(MismatchedPairs))]
    public void Create_MismatchedTypes_DoesNotThrowAndKeepsTheOldValue(string target, object authored, string source, object sourceValue)
    {
        GraphicalUiElement targetGue = null!;

        Should.NotThrow(() => targetGue = CreatePair($"{target} = Source.{source}", out _, (target, authored), (source, sourceValue)));

        Read(targetGue, target).ShouldBe(authored);
    }

    [Theory]
    [MemberData(nameof(MismatchedPairs))]
    public void Create_MismatchedTypes_ReportsAnErrorNamingTheReference(string target, object authored, string source, object sourceValue)
    {
        TryCreate($"{target} = Source.{source}", (target, authored), (source, sourceValue));

        _errors.ShouldNotBeEmpty();
        _errors.ShouldAllBe(message => message.Contains($"{target} = Source.{source}") && message.Contains(target));
    }

    [Fact]
    public void Create_IntIntoFloat_ErrorNamesTheTypesAndPointsToGumExpressions()
    {
        TryCreate("Width = Source.FontSize", ("Width", 77f), ("FontSize", 24));

        _errors.ShouldNotBeEmpty();
        _errors[0].ShouldBe(
            "Variable reference 'Width = Source.FontSize' was not applied: Source.FontSize is an int and Target.Width is a float. " +
            "Add the Gum.Expressions package and call GumExpressionService.Initialize() to convert between them. " +
            "Target.Width keeps its current value.");
    }

    [Fact]
    public void Create_BoolIntoFloat_ErrorSaysTheTypesCannotBeConverted()
    {
        TryCreate("Width = Source.IsBold", ("Width", 77f), ("IsBold", true));

        _errors.ShouldNotBeEmpty();
        _errors[0].ShouldBe(
            "Variable reference 'Width = Source.IsBold' was not applied: Source.IsBold is a bool and Target.Width is a float, " +
            "which cannot be converted. Target.Width keeps its current value.");
    }

    [Theory]
    [InlineData("Width", 77f, "IsBold", true)]
    [InlineData("Width", 77f, "Text", "wide")]
    [InlineData("Visible", true, "FontSize", 24)]
    public void Create_TypesNoConversionCanBridge_DoNotSuggestGumExpressions(string target, object authored, string source, object sourceValue)
    {
        TryCreate($"{target} = Source.{source}", (target, authored), (source, sourceValue));

        _errors.ShouldNotBeEmpty();
        _errors.ShouldAllBe(message => !message.Contains("Gum.Expressions"));
    }

    [Theory]
    [InlineData("Width", 77f, "FontSize", 24)]
    [InlineData("Red", 7, "Width", 150f)]
    [InlineData("Text", "authored", "FontSize", 24)]
    [InlineData("HorizontalAlignment", HorizontalAlignment.Left, "Text", "Right")]
    public void Create_TypesGumExpressionsConverts_SuggestGumExpressions(string target, object authored, string source, object sourceValue)
    {
        TryCreate($"{target} = Source.{source}", (target, authored), (source, sourceValue));

        _errors.ShouldNotBeEmpty();
        _errors.ShouldAllBe(message => message.Contains("Add the Gum.Expressions package"));
    }

    [Fact]
    public void LiveUpdate_MismatchedTypes_DoesNotThrowAndKeepsTheOldValue()
    {
        GraphicalUiElement target = null!;
        GraphicalUiElement source = null!;
        Should.NotThrow(() => target = CreatePair("Width = Source.FontSize", out source, ("Width", 77f), ("FontSize", 24)));
        _errors.Clear();

        Should.NotThrow(() => ((TextRuntime)source).FontSize = 30);

        Read(target, "Width").ShouldBe(77f);
        _errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void LiveUpdate_IntIntoString_KeepsTheOldText()
    {
        GraphicalUiElement target = CreatePair("Text = Source.FontSize", out GraphicalUiElement source, ("Text", "authored"), ("FontSize", 24));

        ((TextRuntime)source).FontSize = 30;

        ((TextRuntime)target).Text.ShouldBe("authored");
    }

    [Fact]
    public void ApplyAllVariableReferences_MismatchedTypes_KeepsTheOldValueAndReports()
    {
        BuildPair("Width = Source.FontSize", useExpressions: false, ("Width", 77f), ("FontSize", 24));
        ComponentSave pair = ObjectFinder.Self.GumProjectSave!.Components[0];

        Should.NotThrow(() => ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences());

        pair.DefaultState.GetValue("Target.Width").ShouldBe(77f);
        _errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void ApplyAllVariableReferences_ThenCreate_DoesNotThrow()
    {
        ComponentSave pair = BuildPair("Width = Source.IsBold", useExpressions: false, ("Width", 77f), ("IsBold", true));
        ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences();

        GraphicalUiElement created = null!;
        Should.NotThrow(() => created = pair.ToGraphicalUiElement());

        ((TextRuntime)created.GetGraphicalUiElementByName("Target")!).Width.ShouldBe(77f);
    }

    [Fact]
    public void Create_MismatchedTypesWithGumExpressions_StillConverts()
    {
        GraphicalUiElement target = CreatePair("Width = Source.FontSize", out _, useExpressions: true, ("Width", 77f), ("FontSize", 24));

        ((TextRuntime)target).Width.ShouldBe(24f);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void Create_StringNamingAnEnumValueWithGumExpressions_StillConverts()
    {
        GraphicalUiElement target = CreatePair("HorizontalAlignment = Source.Text", out _, useExpressions: true,
            ("HorizontalAlignment", HorizontalAlignment.Left), ("Text", "Right"));

        ((TextRuntime)target).HorizontalAlignment.ShouldBe(HorizontalAlignment.Right);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void Create_BoolIntoFloatWithGumExpressions_KeepsTheOldValueWithoutThrowing()
    {
        GraphicalUiElement target = CreatePair("Width = Source.IsBold", out _, useExpressions: true, ("Width", 77f), ("IsBold", true));

        ((TextRuntime)target).Width.ShouldBe(77f);
    }

    [Fact]
    public void Create_SameType_Applies()
    {
        GraphicalUiElement target = CreatePair("Width = Source.Width", out _, ("Width", 77f), ("Width", 150f));

        ((TextRuntime)target).Width.ShouldBe(150f);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void Create_SameTypeString_Applies()
    {
        GraphicalUiElement target = CreatePair("Text = Source.Text", out _, ("Text", "authored"), ("Text", "copied"));

        ((TextRuntime)target).Text.ShouldBe("copied");
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void Create_SameEnumType_Applies()
    {
        GraphicalUiElement target = CreatePair("HorizontalAlignment = Source.HorizontalAlignment", out _,
            ("HorizontalAlignment", HorizontalAlignment.Left), ("HorizontalAlignment", HorizontalAlignment.Right));

        ((TextRuntime)target).HorizontalAlignment.ShouldBe(HorizontalAlignment.Right);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void Create_FloatIntoNullableFloat_Applies()
    {
        GraphicalUiElement target = CreatePair("MinWidth = Source.Width", out _, ("MinWidth", 5f), ("Width", 150f));

        ((TextRuntime)target).MinWidth.ShouldBe(150f);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void Create_FloatVariableIntoFontSize_StaysFractional()
    {
        GraphicalUiElement target = CreatePair("FontSize = Source.Width", out _, ("FontSize", 18), ("Width", 12.5f));

        ((TextRuntime)target).FontSize.ShouldBe(12.5f);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void Create_FontSizeIntoFontSize_Applies()
    {
        GraphicalUiElement target = CreatePair("FontSize = Source.FontSize", out _, ("FontSize", 18), ("FontSize", 30));

        ((TextRuntime)target).FontSize.ShouldBe(30f);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void Create_IntIntoEnum_Applies()
    {
        GraphicalUiElement target = CreatePair("HorizontalAlignment = Source.FontSize", out _,
            ("HorizontalAlignment", HorizontalAlignment.Left), ("FontSize", 2));

        ((TextRuntime)target).HorizontalAlignment.ShouldBe(HorizontalAlignment.Right);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void Create_NegatedBoolIntoBool_Applies()
    {
        GraphicalUiElement target = CreatePair("Visible = !Source.IsBold", out _, ("Visible", true), ("IsBold", true));

        ((TextRuntime)target).Visible.ShouldBe(false);
        _errors.ShouldBeEmpty();
    }

    private static object? Read(GraphicalUiElement element, string name)
    {
        element.TryGetProperty(name, out object? value).ShouldBeTrue();
        return value;
    }

    private GraphicalUiElement CreatePair(string row, out GraphicalUiElement source, params (string Name, object Value)[] authored)
        => CreatePair(row, out source, useExpressions: false, authored);

    private GraphicalUiElement CreatePair(string row, out GraphicalUiElement source, bool useExpressions, params (string Name, object Value)[] authored)
    {
        ComponentSave pair = BuildPair(row, useExpressions, authored);
        GraphicalUiElement root = pair.ToGraphicalUiElement();
        source = root.GetGraphicalUiElementByName("Source")!;
        return root.GetGraphicalUiElementByName("Target")!;
    }

    // Creates the pair like CreatePair but leaves a throw to the caller's assertion on _errors (the throw itself is
    // pinned by the Create_MismatchedTypes_DoesNotThrow tests).
    private void TryCreate(string row, params (string Name, object Value)[] authored)
    {
        ComponentSave pair = BuildPair(row, useExpressions: false, authored);
        try
        {
            pair.ToGraphicalUiElement();
        }
        catch (InvalidCastException)
        {
        }
    }

    // A component "Pair" holding Text instances Source and Target, with the row on Target. The first authored value is
    // Target's, the second Source's.
    private ComponentSave BuildPair(string row, bool useExpressions, params (string Name, object Value)[] authored)
    {
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }

        GumProjectSave project = new GumProjectSave();
        foreach (string name in new[] { "Container", "Text" })
        {
            StandardElementSave element = new StandardElementSave { Name = name };
            element.States.Add(StandardElementsManager.Self.GetDefaultStateFor(name)!);
            element.DefaultState!.ParentContainer = element;
            project.StandardElements.Add(element);
        }
        ElementSaveExtensions.RegisterGueInstantiation("Container", () => new ContainerRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Text", () => new TextRuntime());
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave pair = new ComponentSave { Name = "Pair", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = pair };
        pair.States.Add(state);
        pair.Instances.Add(new InstanceSave { Name = "Source", BaseType = "Text", ParentContainer = pair });
        pair.Instances.Add(new InstanceSave { Name = "Target", BaseType = "Text", ParentContainer = pair });
        AddAuthored(state, "Target", authored[0]);
        AddAuthored(state, "Source", authored[1]);
        VariableListSave<string> rows = new VariableListSave<string> { Type = "string", Name = "Target.VariableReferences" };
        rows.Value.Add(row);
        state.VariableLists.Add(rows);
        project.Components.Add(pair);
        return pair;
    }

    private static void AddAuthored(StateSave state, string instance, (string Name, object Value) variable)
    {
        string type = variable.Value switch
        {
            int => "int",
            float => "float",
            bool => "bool",
            string => "string",
            _ => variable.Value.GetType().Name
        };
        state.Variables.Add(new VariableSave { Name = $"{instance}.{variable.Name}", Type = type, Value = variable.Value, SetsValue = true });
    }
}
