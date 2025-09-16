using Gum.DataTypes;
using Gum.Managers;
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
using Gum.Forms.DefaultFromFileVisuals;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class SplitterTests : BaseTestClass
{
    StackPanel _parentPanel;
    Splitter _splitter;
    Panel _firstPanel;
    Panel _secondPanel;

    [Fact]
    public void ApplyResizeChangeInPixels_ShouldResize_IfSiblingsAreAbsolute()
    {
        SetupVerticalStack();

        _firstPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        _firstPanel.Height = 100;
        _secondPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        _secondPanel.Height = 100;

        _splitter.ApplyResizeChangeInPixels(12);

        _firstPanel.Height.ShouldBe(112);
        _secondPanel.Height.ShouldBe(88);
    }

    [Fact]
    public void ApplyResizeChangeInPixels_ShouldResize_IfSiblingsAreRelativeToChildren()
    {
        SetupVerticalStack();

        _firstPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        _firstPanel.Height = 100;
        _secondPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        _secondPanel.Height = 100;

        var originalAbsoluteHeightBefore = _firstPanel.Visual.GetAbsoluteHeight();
        var originalAbsoluteHeightAfter = _secondPanel.Visual.GetAbsoluteHeight();

        _splitter.ApplyResizeChangeInPixels(12);

        _firstPanel.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightBefore + 12);
        _secondPanel.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightAfter - 12);
    }

    [Fact]
    public void ApplyResizeChangeInPixels_ShouldResize_IfSiblingsArePercentOfParent()
    {
        SetupVerticalStack();

        _parentPanel.Height = 300;
        _parentPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        _firstPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        _firstPanel.Height = 30;
        _secondPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        _secondPanel.Height = 30;

        var originalAbsoluteHeightBefore = _firstPanel.Visual.GetAbsoluteHeight();
        var originalAbsoluteHeightAfter = _secondPanel.Visual.GetAbsoluteHeight();

        _splitter.ApplyResizeChangeInPixels(12);

        _firstPanel.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightBefore + 12);
        _secondPanel.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightAfter - 12);
    }


    [Fact]
    public void ApplyResizeChangeInPixels_ShouldResize_IfSiblingsAreRatio()
    {
        SetupVerticalStack();

        _firstPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Ratio;
        _firstPanel.Height = 1;
        _secondPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Ratio;
        _secondPanel.Height = 1;

        // Since this is ratio, the parent must have a fixed height:
        _parentPanel.Height = 300;
        _parentPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        var originalAbsoluteHeightBefore = _firstPanel.Visual.GetAbsoluteHeight();
        var originalAbsoluteHeightAfter = _secondPanel.Visual.GetAbsoluteHeight();

        _splitter.ApplyResizeChangeInPixels(12);

        _firstPanel.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightBefore + 12);
        _secondPanel.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightAfter - 12);
    }

    [Fact]
    public void ApplyResizeChangeInPixels_ShouldResize_IfSiblingsAreAsymmetricRatio()
    {
        SetupVerticalStack();

        _firstPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Ratio;
        _firstPanel.Height = 2;
        _secondPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Ratio;
        _secondPanel.Height = 1;

        // Since this is ratio, the parent must have a fixed height:
        _parentPanel.Height = 300;
        _parentPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        var originalAbsoluteHeightBefore = _firstPanel.Visual.GetAbsoluteHeight();
        var originalAbsoluteHeightAfter = _secondPanel.Visual.GetAbsoluteHeight();

        _splitter.ApplyResizeChangeInPixels(12);

        _firstPanel.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightBefore + 12, .01f);
        _secondPanel.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightAfter - 12, .01f);
    }

    [Fact]
    public void ApplyResizeChangeInPixels_ShouldResize_IfMultipleRatioObjectsAreInParent()
    {
        SetupVerticalStack();

        _firstPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Ratio;
        _firstPanel.Height = 2;
        _secondPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Ratio;
        _secondPanel.Height = 1;

        var additionalSibling = new StackPanel();
        additionalSibling.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Ratio;
        additionalSibling.Height = 4;
        _parentPanel.AddChild(additionalSibling);

        // Since this is ratio, the parent must have a fixed height:
        _parentPanel.Height = 300;
        _parentPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        var originalAbsoluteHeightBefore = _firstPanel.Visual.GetAbsoluteHeight();
        var originalAbsoluteHeightAfter = _secondPanel.Visual.GetAbsoluteHeight();
        var additionalAbsoluteHeight = additionalSibling.Visual.GetAbsoluteHeight();

        _splitter.ApplyResizeChangeInPixels(12);

        _firstPanel.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightBefore + 12, .01f);
        _secondPanel.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightAfter - 12, .01f);
        additionalSibling.Visual.GetAbsoluteHeight().ShouldBe(additionalAbsoluteHeight, .01f);
    }

    [Fact]
    public void ApplyResizeChangeInPixels_ShouldNotResize_PastHeight0()
    {
        SetupVerticalStack();

        _firstPanel.Height = 10;
        _secondPanel.Height = 10;

        _splitter.ApplyResizeChangeInPixels(20);

        _firstPanel.Height.ShouldBe(20);
        _secondPanel.Height.ShouldBe(0);

        _splitter.ApplyResizeChangeInPixels(-30);


        _firstPanel.Height.ShouldBe(0);
        _secondPanel.Height.ShouldBe(20);
    }

    [Fact]
    public void ApplyResizeChangeInPixels_ShouldNotResize_PastMinHeight()
    {
        SetupVerticalStack();

        _firstPanel.Height = 10;
        _firstPanel.Visual.MinHeight = 5;
        _secondPanel.Height = 10;
        _secondPanel.Visual.MinHeight = 5;

        _splitter.ApplyResizeChangeInPixels(20);

        _firstPanel.Height.ShouldBe(15);
        _secondPanel.Height.ShouldBe(5);

        _splitter.ApplyResizeChangeInPixels(-30);


        _firstPanel.Height.ShouldBe(5);
        _secondPanel.Height.ShouldBe(15);
    }

    [Fact]
    public void ApplyResizeChangeInPixels_ShouldNotResize_PastWidth0()
    {
        SetupHorizontalStack();

        _firstPanel.Width = 10;
        _secondPanel.Width = 10;

        _splitter.ApplyResizeChangeInPixels(20);

        _firstPanel.Width.ShouldBe(20);
        _secondPanel.Width.ShouldBe(0);

        _splitter.ApplyResizeChangeInPixels(-30);

        _firstPanel.Width.ShouldBe(0);
        _secondPanel.Width.ShouldBe(20);
    }

    [Fact]
    public void ApplyResizeChangeInPixels_ShouldNotResize_PastHeight0_IfPercentageOfParent()
    {
        SetupVerticalStack();

        _firstPanel.Height = 40;
        _firstPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent;

        _secondPanel.Height = 40;
        _secondPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent;

        _parentPanel.Height = 100;
        _parentPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        _splitter.ApplyResizeChangeInPixels(60);

        _firstPanel.Height.ShouldBe(80);
        _secondPanel.Height.ShouldBe(0);
    }

    [Fact]
    public void ToGraphicalUiElement_ShouldCreateFromFileSplitter_IfElementExists()
    {
        GumProjectSave gumProject = new GumProjectSave();
        var splitterComponent = new ComponentSave();
        // give it a default state:
        splitterComponent.States.Add(new Gum.DataTypes.Variables.StateSave() { Name = "Default" });
        gumProject.Components.Add(splitterComponent);
        splitterComponent.Name = "TestSplitterComponent";
        splitterComponent.Behaviors.Add(new Gum.DataTypes.Behaviors.ElementBehaviorReference
        { BehaviorName = "SplitterBehavior" });

        ObjectFinder.Self.GumProjectSave = gumProject;

        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        var gue = splitterComponent.ToGraphicalUiElement();

        (gue is DefaultFromFileSplitterRuntime).ShouldBeTrue();
    }

    void SetupVerticalStack()
    {
        _parentPanel = new();
        _firstPanel = new Panel();
        _parentPanel.AddChild(_firstPanel);

        _splitter = new Splitter();
        _splitter.Dock(Gum.Wireframe.Dock.FillHorizontally);
        _parentPanel.AddChild(_splitter);


        _secondPanel = new Panel();
        _parentPanel.AddChild(_secondPanel);
    }

    void SetupHorizontalStack()
    {
        _parentPanel = new();
        _parentPanel.Orientation = Orientation.Horizontal;

        _firstPanel = new Panel();
        _parentPanel.AddChild(_firstPanel);
        _firstPanel.Height = 40;
        _firstPanel.Visual.HeightUnits = DimensionUnitType.Absolute;

        _splitter = new Splitter();
        _splitter.Dock(Gum.Wireframe.Dock.FillVertically);
        _parentPanel.AddChild(_splitter);

        _secondPanel = new Panel();
        _parentPanel.AddChild(_secondPanel);
        _secondPanel.Height = 40;
        _secondPanel.Visual.HeightUnits = DimensionUnitType.Absolute;

    }
}
