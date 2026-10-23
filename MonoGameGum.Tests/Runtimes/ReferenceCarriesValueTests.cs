using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A same-component reference carries a value set in code unchanged, even one the saved variable type cannot hold:
/// X/Y units with no <see cref="PositionUnitType"/> value, and a fractional FontSize (saved as an int) (#6414).
/// <see cref="VariableReferenceTypedSetterTests"/> sweeps every variable; these pin the cases by name.
/// </summary>
public class ReferenceCarriesValueTests : BaseTestClass
{
    public ReferenceCarriesValueTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void XUnits_PercentageOfFile_ReferenceCarriesIt(bool useExpressions)
    {
        GraphicalUiElement target = CreatePair(useExpressions, "Container", "XUnits = Source.XUnits", out GraphicalUiElement source);

        source.XUnits = GeneralUnitType.PercentageOfFile;

        target.XUnits.ShouldBe(GeneralUnitType.PercentageOfFile);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void YUnits_PercentageOfOtherDimension_ReferenceCarriesIt(bool useExpressions)
    {
        GraphicalUiElement target = CreatePair(useExpressions, "Container", "YUnits = Source.YUnits", out GraphicalUiElement source);

        source.YUnits = GeneralUnitType.PercentageOfOtherDimension;

        target.YUnits.ShouldBe(GeneralUnitType.PercentageOfOtherDimension);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void XUnits_MaintainFileAspectRatio_ReferenceCarriesIt(bool useExpressions)
    {
        GraphicalUiElement target = CreatePair(useExpressions, "Container", "XUnits = Source.XUnits", out GraphicalUiElement source);

        source.XUnits = GeneralUnitType.MaintainFileAspectRatio;

        target.XUnits.ShouldBe(GeneralUnitType.MaintainFileAspectRatio);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void XUnits_ObsoletePixelsFromMiddleInverted_ReferenceCarriesIt(bool useExpressions)
    {
        GraphicalUiElement target = CreatePair(useExpressions, "Container", "XUnits = Source.XUnits", out GraphicalUiElement source);

#pragma warning disable CS0618 // the obsolete value is the one with no PositionUnitType counterpart on X
        source.XUnits = GeneralUnitType.PixelsFromMiddleInverted;

        target.XUnits.ShouldBe(GeneralUnitType.PixelsFromMiddleInverted);
#pragma warning restore CS0618
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void XUnits_ValueWithPositionUnitCounterpart_ReferenceStillCarriesIt(bool useExpressions)
    {
        GraphicalUiElement target = CreatePair(useExpressions, "Container", "XUnits = Source.XUnits", out GraphicalUiElement source);

        source.XUnits = GeneralUnitType.PixelsFromLarge;

        target.XUnits.ShouldBe(GeneralUnitType.PixelsFromLarge);
    }

    [Fact]
    public void TryGetProperty_XUnitsWithNoPositionUnitCounterpart_StaysAPositionUnitType()
    {
        // The snapshot serializer writes this value into a saved variable, so the public read stays one a project can hold.
        ContainerRuntime container = new ContainerRuntime();
        container.XUnits = GeneralUnitType.PercentageOfFile;

        container.TryGetProperty("XUnits", out object? value).ShouldBeTrue();

        value.ShouldBeOfType<PositionUnitType>();
    }

    [Fact]
    public void FontSize_Fractional_ReferenceCarriesIt()
    {
        GraphicalUiElement target = CreatePair(useExpressions: false, "Text", "FontSize = Source.FontSize", out GraphicalUiElement source);

        ((TextRuntime)source).FontSize = 12.5f;

        ((TextRuntime)target).FontSize.ShouldBe(12.5f);
    }

    [Fact]
    public void FontSize_Whole_ReferenceCarriesIt()
    {
        GraphicalUiElement target = CreatePair(useExpressions: true, "Text", "FontSize = Source.FontSize", out GraphicalUiElement source);

        ((TextRuntime)source).FontSize = 24;

        ((TextRuntime)target).FontSize.ShouldBe(24f);
    }

    [Fact]
    public void SetProperty_FontSizeAsFloat_Applies()
    {
        TextRuntime text = new TextRuntime();

        text.SetProperty("FontSize", 12.5f);

        text.FontSize.ShouldBe(12.5f);
    }

    [Fact]
    public void SetProperty_FontSizeAsInt_Applies()
    {
        TextRuntime text = new TextRuntime();

        text.SetProperty("FontSize", 30);

        text.FontSize.ShouldBe(30f);
    }

    // A component "Pair" holding instances Source and Target of the standard, with the given row on Target.
    private static GraphicalUiElement CreatePair(bool useExpressions, string standard, string row, out GraphicalUiElement source)
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
        pair.Instances.Add(new InstanceSave { Name = "Source", BaseType = standard, ParentContainer = pair });
        pair.Instances.Add(new InstanceSave { Name = "Target", BaseType = standard, ParentContainer = pair });
        VariableListSave<string> rows = new VariableListSave<string> { Type = "string", Name = "Target.VariableReferences" };
        rows.Value.Add(row);
        state.VariableLists.Add(rows);
        project.Components.Add(pair);

        GraphicalUiElement root = pair.ToGraphicalUiElement();
        source = root.GetGraphicalUiElementByName("Source")!;
        return root.GetGraphicalUiElementByName("Target")!;
    }
}
