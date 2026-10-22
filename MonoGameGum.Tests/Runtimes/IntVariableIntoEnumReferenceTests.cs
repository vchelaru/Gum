using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A whole-number variable read into an enum row selects the enum value, at creation and when the source
/// changes live (#6446), with and without Gum.Expressions.
/// </summary>
public class IntVariableIntoEnumReferenceTests : BaseTestClass
{
    public IntVariableIntoEnumReferenceTests()
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
    [InlineData(false)]
    [InlineData(true)]
    public void Load_FontSizeIntoEnum_SelectsTheEnumValue(bool useExpressions)
    {
        if (useExpressions) GumExpressionService.Initialize();
        (TextRuntime source, TextRuntime target) = Create("HorizontalAlignment = Source.FontSize", 2);

        target.HorizontalAlignment.ShouldBe(HorizontalAlignment.Right);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceFontSizeChanged_FontSizeIntoEnum_SelectsTheEnumValue(bool useExpressions)
    {
        if (useExpressions) GumExpressionService.Initialize();
        (TextRuntime source, TextRuntime target) = Create("HorizontalAlignment = Source.FontSize", 2);

        source.FontSize = 1;

        target.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceRedChanged_RedIntoEnum_SelectsTheEnumValue(bool useExpressions)
    {
        if (useExpressions) GumExpressionService.Initialize();
        (TextRuntime source, TextRuntime target) = Create("HorizontalAlignment = Source.Red", 2);
        source.Red = 1;

        target.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
    }

    [Fact]
    public void SourceFontSizeChanged_FontSizeIntoFloat_FollowsWithExpressions()
    {
        GumExpressionService.Initialize();
        (TextRuntime source, TextRuntime target) = Create("Rotation = Source.FontSize", 2);

        source.FontSize = 7;

        target.Rotation.ShouldBe(7f);
    }

    [Fact]
    public void SourceFontSizeChanged_FontSizeIntoFontSize_KeepsAFractionWithoutExpressions()
    {
        (TextRuntime source, TextRuntime target) = Create("FontSize = Source.FontSize", 2);

        source.FontSize = 7;
        target.FontSize.ShouldBe(7f);
        source.FontSize = 7.5f;

        target.FontSize.ShouldBe(7.5f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceOutlineThicknessChanged_IntoEnum_SelectsTheEnumValue(bool useExpressions)
    {
        if (useExpressions) GumExpressionService.Initialize();
        (TextRuntime source, TextRuntime target) = Create("HorizontalAlignment = Source.OutlineThickness", 2);

        source.OutlineThickness = 1;

        target.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
    }

    private static (TextRuntime Source, TextRuntime Target) Create(string targetRow, int sourceFontSize)
    {
        ComponentSave card = new ComponentSave { Name = "Card", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = card };
        card.States.Add(state);
        foreach (string name in new[] { "Source", "Target" })
        {
            card.Instances.Add(new InstanceSave { Name = name, BaseType = "Text", ParentContainer = card });
        }
        state.Variables.Add(new VariableSave { Name = "Source.FontSize", Type = "int", Value = sourceFontSize, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Source.Red", Type = "int", Value = sourceFontSize, SetsValue = true });

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
