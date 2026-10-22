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
/// reports an error through <see cref="CustomSetPropertyOnRenderable.PropertyAssignmentError"/> instead of throwing at
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
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Record;
    }

    // PropertyAssignmentError also carries font and file failures; only the reference messages are under test.
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
            "Variable reference 'Width = Source.FontSize' was not applied in Pair (Container): Source.FontSize is an int and Target.Width is a float. " +
            "Add the Gum.Expressions package and call GumExpressionService.Initialize() to convert between them. " +
            "Target.Width keeps its current value.");
    }

    [Fact]
    public void Create_BoolIntoFloat_ErrorSaysTheTypesCannotBeConverted()
    {
        TryCreate("Width = Source.IsBold", ("Width", 77f), ("IsBold", true));

        _errors.ShouldNotBeEmpty();
        _errors[0].ShouldBe(
            "Variable reference 'Width = Source.IsBold' was not applied in Pair (Container): Source.IsBold is a bool and Target.Width is a float, " +
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

        pair.DefaultState!.GetValue("Target.Width").ShouldBe(77f);
        _errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void LiveUpdate_BrokenRowChangingEveryFrame_ReportsOnce()
    {
        GraphicalUiElement target = CreatePair("Width = Source.FontSize", out GraphicalUiElement source, ("Width", 77f), ("FontSize", 24));

        for (int frame = 0; frame < 300; frame++)
        {
            ((TextRuntime)source).FontSize = 25 + frame;
        }

        _errors.Count.ShouldBe(1);
        Read(target, "Width").ShouldBe(77f);
    }

    [Fact]
    public void LiveUpdate_BrokenRowChangedThroughSetProperty_ReportsOnce()
    {
        CreatePair("Width = Source.FontSize", out GraphicalUiElement source, ("Width", 77f), ("FontSize", 24));

        for (int frame = 0; frame < 300; frame++)
        {
            source.SetProperty("FontSize", 25 + frame);
        }

        _errors.Count.ShouldBe(1);
    }

    [Fact]
    public void Create_TheSameBrokenComponentTwice_EachNewInstanceReportsOnceForItself()
    {
        ComponentSave pair = BuildPair("Width = Source.FontSize", useExpressions: false, ("Width", 77f), ("FontSize", 24));

        GraphicalUiElement first = pair.ToGraphicalUiElement();
        _errors.Count.ShouldBe(1);
        GraphicalUiElement second = pair.ToGraphicalUiElement();
        _errors.Count.ShouldBe(2);

        ((TextRuntime)first.GetGraphicalUiElementByName("Source")!).FontSize = 30;
        ((TextRuntime)second.GetGraphicalUiElementByName("Source")!).FontSize = 31;
        _errors.Count.ShouldBe(2);
    }

    [Fact]
    public void ApplyAllVariableReferences_BrokenRow_ReportsOncePerCall()
    {
        BuildPair("Width = Source.FontSize", useExpressions: false, ("Width", 77f), ("FontSize", 24));
        GumProjectSave project = ObjectFinder.Self.GumProjectSave!;

        project.ApplyAllVariableReferences();
        _errors.Count.ShouldBe(1);
        project.ApplyAllVariableReferences();

        _errors.Count.ShouldBe(2);
    }

    [Fact]
    public void Create_ASubscriberThrows_CreationStillCompletesAndTheFailureIsWrittenToConsoleError()
    {
        Action<string> throwing = _ => throw new InvalidOperationException("the subscriber failed");
        System.IO.TextWriter originalError = Console.Error;
        System.IO.StringWriter captured = new System.IO.StringWriter();
        Console.SetError(captured);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += throwing;
        GraphicalUiElement target = null!;
        try
        {
            Should.NotThrow(() => target = CreatePair("Width = Source.FontSize", out _, ("Width", 77f), ("FontSize", 24)));
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= throwing;
            Console.SetError(originalError);
        }

        Read(target, "Width").ShouldBe(77f);
        captured.ToString().ShouldContain("the subscriber failed");
    }

    [Fact]
    public void Create_MismatchedTypes_WritesTheMessageToConsoleError()
    {
        System.IO.TextWriter originalError = Console.Error;
        System.IO.StringWriter captured = new System.IO.StringWriter();
        Console.SetError(captured);
        try
        {
            TryCreate("Width = Source.FontSize", ("Width", 77f), ("FontSize", 24));
        }
        finally
        {
            Console.SetError(originalError);
        }

        captured.ToString().ShouldContain("[Gum] Variable reference 'Width = Source.FontSize' was not applied in Pair");
    }

    [Theory]
    [InlineData("MinWidth", 50f, "FontSize", 24)]
    [InlineData("MaxWidth", 50f, "FontSize", 24)]
    [InlineData("MinHeight", 50f, "Red", 9)]
    [InlineData("MaxHeight", 50f, "Alpha", 9)]
    [InlineData("MaxNumberOfLines", 3, "Width", 150f)]
    public void LiveUpdate_MismatchedTypesIntoANullableVariable_KeepsTheOldValue(string target, object authored, string source, object sourceValue)
    {
        GraphicalUiElement targetGue = null!;
        GraphicalUiElement sourceGue = null!;
        Should.NotThrow(() => targetGue = CreatePair($"{target} = Source.{source}", out sourceGue, (target, authored), (source, sourceValue)));

        sourceGue.SetProperty(source, sourceValue is int sourceInt ? sourceInt + 1 : (object)((float)sourceValue + 1));

        Read(targetGue, target).ShouldBe(authored);
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

    // With Gum.Expressions an enum value read into a different enum type is refused like it is without the package
    // (#6441): Enum.ToObject on its number would turn VerticalAlignment.Bottom into HorizontalAlignment.Right.
    public static TheoryData<string, object, string, object> OtherEnumTypePairs => new()
    {
        { "XOrigin", HorizontalAlignment.Center, "YOrigin", VerticalAlignment.Bottom },
        { "YOrigin", VerticalAlignment.Center, "XOrigin", HorizontalAlignment.Right },
        { "HorizontalAlignment", HorizontalAlignment.Left, "VerticalAlignment", VerticalAlignment.Bottom },
        { "VerticalAlignment", VerticalAlignment.Top, "HorizontalAlignment", HorizontalAlignment.Right },
    };

    [Theory]
    [MemberData(nameof(OtherEnumTypePairs))]
    public void Create_EnumIntoOtherEnumTypeWithGumExpressions_KeepsTheOldValue(string target, object authored, string source, object sourceValue)
    {
        GraphicalUiElement targetGue = CreatePair($"{target} = Source.{source}", out _, useExpressions: true, (target, authored), (source, sourceValue));

        Read(targetGue, target).ShouldBe(authored);
    }

    [Fact]
    public void Create_EnumIntoOtherEnumTypeWithGumExpressions_ReportsThatItCannotBeConverted()
    {
        TryCreate("XOrigin = Source.YOrigin", useExpressions: true,
            ("XOrigin", HorizontalAlignment.Center), ("YOrigin", VerticalAlignment.Bottom));

        _errors.ShouldBe(new[]
        {
            "Variable reference 'XOrigin = Source.YOrigin' was not applied in Pair (Container): Source.YOrigin is a VerticalAlignment " +
            "and Target.XOrigin is a HorizontalAlignment, which cannot be converted. Target.XOrigin keeps its current value."
        });
    }

    [Fact]
    public void Create_EnumIntoOtherEnumTypeWithoutGumExpressions_ReportsTheSameMessage()
    {
        TryCreate("XOrigin = Source.YOrigin", useExpressions: false,
            ("XOrigin", HorizontalAlignment.Center), ("YOrigin", VerticalAlignment.Bottom));

        _errors.ShouldBe(new[]
        {
            "Variable reference 'XOrigin = Source.YOrigin' was not applied in Pair (Container): Source.YOrigin is a VerticalAlignment " +
            "and Target.XOrigin is a HorizontalAlignment, which cannot be converted. Target.XOrigin keeps its current value."
        });
    }

    [Fact]
    public void LiveUpdate_EnumIntoOtherEnumTypeWithGumExpressions_KeepsTheOldValueAndReportsOnce()
    {
        GraphicalUiElement target = CreatePair("XOrigin = Source.YOrigin", out GraphicalUiElement source, useExpressions: true,
            ("XOrigin", HorizontalAlignment.Center), ("YOrigin", VerticalAlignment.Bottom));

        ((TextRuntime)source).YOrigin = VerticalAlignment.Top;
        ((TextRuntime)source).YOrigin = VerticalAlignment.Center;
        source.SetProperty("YOrigin", VerticalAlignment.Bottom);

        Read(target, "XOrigin").ShouldBe(HorizontalAlignment.Center);
        _errors.Count.ShouldBe(1);
    }

    [Fact]
    public void ApplyAllVariableReferences_EnumIntoOtherEnumTypeWithGumExpressions_KeepsTheOldValueAndReports()
    {
        BuildPair("XOrigin = Source.YOrigin", useExpressions: true,
            ("XOrigin", HorizontalAlignment.Center), ("YOrigin", VerticalAlignment.Bottom));
        ComponentSave pair = ObjectFinder.Self.GumProjectSave!.Components[0];

        ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences();

        pair.DefaultState!.GetValue("Target.XOrigin").ShouldBe(HorizontalAlignment.Center);
        _errors.Count.ShouldBe(1);
    }

    [Fact]
    public void Create_SameEnumTypeWithGumExpressions_Applies()
    {
        GraphicalUiElement target = CreatePair("XOrigin = Source.HorizontalAlignment", out _, useExpressions: true,
            ("XOrigin", HorizontalAlignment.Left), ("HorizontalAlignment", HorizontalAlignment.Right));

        ((TextRuntime)target).XOrigin.ShouldBe(HorizontalAlignment.Right);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void LiveUpdate_SameEnumTypeWithGumExpressions_Applies()
    {
        GraphicalUiElement target = CreatePair("XOrigin = Source.XOrigin", out GraphicalUiElement source, useExpressions: true,
            ("XOrigin", HorizontalAlignment.Left), ("XOrigin", HorizontalAlignment.Center));

        ((TextRuntime)source).XOrigin = HorizontalAlignment.Right;

        ((TextRuntime)target).XOrigin.ShouldBe(HorizontalAlignment.Right);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyAllVariableReferences_SameEnumTypeWithGumExpressions_Applies()
    {
        BuildPair("XOrigin = Source.XOrigin", useExpressions: true,
            ("XOrigin", HorizontalAlignment.Left), ("XOrigin", HorizontalAlignment.Right));
        ComponentSave pair = ObjectFinder.Self.GumProjectSave!.Components[0];

        ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences();

        pair.DefaultState!.GetValue("Target.XOrigin").ShouldBe(HorizontalAlignment.Right);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void Create_WholeNumberIntoEnumWithGumExpressions_SelectsTheEnumValue()
    {
        GraphicalUiElement target = CreatePair("HorizontalAlignment = Source.FontSize", out _, useExpressions: true,
            ("HorizontalAlignment", HorizontalAlignment.Left), ("FontSize", 2));

        ((TextRuntime)target).HorizontalAlignment.ShouldBe(HorizontalAlignment.Right);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void LiveUpdate_WholeNumberIntoEnumWithGumExpressions_SelectsTheEnumValue()
    {
        GraphicalUiElement target = CreatePair("HorizontalAlignment = Source.Red", out GraphicalUiElement source, useExpressions: true,
            ("HorizontalAlignment", HorizontalAlignment.Left), ("Red", 2));

        ((TextRuntime)source).Red = 1;

        ((TextRuntime)target).HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void Create_TextNamingAnEnumValueWithGumExpressions_IgnoresCase()
    {
        GraphicalUiElement target = CreatePair("HorizontalAlignment = Source.Text", out _, useExpressions: true,
            ("HorizontalAlignment", HorizontalAlignment.Left), ("Text", "rIGHT"));

        ((TextRuntime)target).HorizontalAlignment.ShouldBe(HorizontalAlignment.Right);
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void Create_EnumIntoStringWithGumExpressions_WritesTheValueName()
    {
        GraphicalUiElement target = CreatePair("Text = Source.HorizontalAlignment", out _, useExpressions: true,
            ("Text", "authored"), ("HorizontalAlignment", HorizontalAlignment.Right));

        ((TextRuntime)target).Text.ShouldBe("Right");
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void Create_EnumIntoIntWithGumExpressions_KeepsTheOldValue()
    {
        GraphicalUiElement target = CreatePair("FontSize = Source.HorizontalAlignment", out _, useExpressions: true,
            ("FontSize", 18), ("HorizontalAlignment", HorizontalAlignment.Right));

        ((TextRuntime)target).FontSize.ShouldBe(18f);
    }

    [Fact]
    public void Create_EnumIntoFloatWithGumExpressions_KeepsTheOldValue()
    {
        GraphicalUiElement target = CreatePair("Width = Source.HorizontalAlignment", out _, useExpressions: true,
            ("Width", 77f), ("HorizontalAlignment", HorizontalAlignment.Right));

        ((TextRuntime)target).Width.ShouldBe(77f);
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
        => TryCreate(row, useExpressions: false, authored);

    private void TryCreate(string row, bool useExpressions, params (string Name, object Value)[] authored)
    {
        ComponentSave pair = BuildPair(row, useExpressions, authored);
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
        if (variable.Name == "MinWidth" || variable.Name == "MaxWidth" || variable.Name == "MinHeight" || variable.Name == "MaxHeight" || variable.Name == "MaxNumberOfLines")
        {
            type += "?";
        }
        state.Variables.Add(new VariableSave { Name = $"{instance}.{variable.Name}", Type = type, Value = variable.Value, SetsValue = true });
    }
}
