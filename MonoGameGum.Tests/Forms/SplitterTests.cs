using MonoGameGum.Forms.Controls;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class SplitterTests
{
    StackPanel _parentPanel;
    Splitter _splitter;
    Panel _panelBefore;
    Panel _panelAfter;

    [Fact]
    public void ApplyResizeChangeInPixels_ShouldResize_IfSiblingsAreAbsolute()
    {
        SetupVerticalStack();

        _panelBefore.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        _panelBefore.Height = 100;
        _panelAfter.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        _panelAfter.Height = 100;

        _splitter.ApplyResizeChangeInPixels(12);

        _panelBefore.Height.ShouldBe(112);
        _panelAfter.Height.ShouldBe(88);
    }

    [Fact]
    public void ApplyResizeChangeInPixels_ShouldResize_IfSiblingsAreRelativeToChildren()
    {
        SetupVerticalStack();

        _panelBefore.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        _panelBefore.Height = 100;
        _panelAfter.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        _panelAfter.Height = 100;

        var originalAbsoluteHeightBefore = _panelBefore.Visual.GetAbsoluteHeight();
        var originalAbsoluteHeightAfter = _panelAfter.Visual.GetAbsoluteHeight();

        _splitter.ApplyResizeChangeInPixels(12);

        _panelBefore.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightBefore + 12);
        _panelAfter.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightAfter - 12);
    }

    [Fact]
    public void ApplyResizeChangeInPixels_ShouldResize_IfSiblingsAreRatio()
    {
        SetupVerticalStack();

        _panelBefore.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Ratio;
        _panelBefore.Height = 1;
        _panelAfter.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Ratio;
        _panelAfter.Height = 1;

        // Since this is ratio, the parent must have a fixed height:
        _parentPanel.Height = 300;
        _parentPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        var originalAbsoluteHeightBefore = _panelBefore.Visual.GetAbsoluteHeight();
        var originalAbsoluteHeightAfter = _panelAfter.Visual.GetAbsoluteHeight();

        _splitter.ApplyResizeChangeInPixels(12);
        // this is still failing....
        _panelBefore.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightBefore + 12);
        _panelAfter.Visual.GetAbsoluteHeight().ShouldBe(originalAbsoluteHeightAfter - 12);
    }


    void SetupVerticalStack()
    {
        _parentPanel = new();
        _panelBefore = new Panel();
        _parentPanel.AddChild(_panelBefore);

        _splitter = new Splitter();
        _splitter.Dock(Gum.Wireframe.Dock.FillHorizontally);
        _parentPanel.AddChild(_splitter);


        _panelAfter = new Panel();
        _parentPanel.AddChild(_panelAfter);
    }


}
