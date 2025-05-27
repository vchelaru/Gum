using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class LayoutTests
{
    [Fact]
    public void Dock_ShouldSetCorrectValues()
    {
        var sut = new GraphicalUiElement(new InvisibleRenderable());

        sut.X = 100;
        sut.XOrigin = HorizontalAlignment.Center;
        sut.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        sut.Width = 100;
        sut.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        sut.Dock(Dock.Left);
        sut.X.ShouldBe(0);
        sut.XOrigin.ShouldBe(HorizontalAlignment.Left);
        sut.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        sut.Y.ShouldBe(0);
        sut.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        sut.Height.ShouldBe(0);
        sut.HeightUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToParent);

        sut.Dock(Dock.Right);
        sut.X.ShouldBe(0);
        sut.XOrigin.ShouldBe(HorizontalAlignment.Right);
        sut.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        sut.Y.ShouldBe(0);
        sut.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        sut.Height.ShouldBe(0);
        sut.HeightUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToParent);
    }

    [Fact]
    public void Anchor_ShouldSetCorrectValues()
    {
        var sut = new GraphicalUiElement(new InvisibleRenderable());

        sut.Anchor(Anchor.TopLeft);
        sut.X.ShouldBe(0);
        sut.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        sut.XOrigin.ShouldBe(HorizontalAlignment.Left);
        sut.Y.ShouldBe(0);
        sut.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        sut.YOrigin.ShouldBe(VerticalAlignment.Top);

        sut.Anchor(Anchor.Left);
        sut.X.ShouldBe(0);
        sut.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        sut.XOrigin.ShouldBe(HorizontalAlignment.Left);
        sut.Y.ShouldBe(0);
        sut.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        sut.YOrigin.ShouldBe(VerticalAlignment.Center);

        sut.Anchor(Anchor.BottomLeft);
        sut.X.ShouldBe(0);
        sut.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        sut.XOrigin.ShouldBe(HorizontalAlignment.Left);
        sut.Y.ShouldBe(0);
        sut.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        sut.YOrigin.ShouldBe(VerticalAlignment.Bottom);

        sut.Anchor(Anchor.Bottom);
        sut.X.ShouldBe(0);
        sut.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        sut.XOrigin.ShouldBe(HorizontalAlignment.Center);
        sut.Y.ShouldBe(0);
        sut.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        sut.YOrigin.ShouldBe(VerticalAlignment.Bottom);

        sut.Anchor(Anchor.BottomRight);
        sut.X.ShouldBe(0);
        sut.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        sut.XOrigin.ShouldBe(HorizontalAlignment.Right);
        sut.Y.ShouldBe(0);
        sut.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        sut.YOrigin.ShouldBe(VerticalAlignment.Bottom);

        sut.Anchor(Anchor.Right);
        sut.X.ShouldBe(0);
        sut.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        sut.XOrigin.ShouldBe(HorizontalAlignment.Right);
        sut.Y.ShouldBe(0);
        sut.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        sut.YOrigin.ShouldBe(VerticalAlignment.Center);

        sut.Anchor(Anchor.TopRight);
        sut.X.ShouldBe(0);
        sut.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        sut.XOrigin.ShouldBe(HorizontalAlignment.Right);
        sut.Y.ShouldBe(0);
        sut.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        sut.YOrigin.ShouldBe(VerticalAlignment.Top);

        sut.Anchor(Anchor.Top);
        sut.X.ShouldBe(0);
        sut.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        sut.XOrigin.ShouldBe(HorizontalAlignment.Center);
        sut.Y.ShouldBe(0);
        sut.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        sut.YOrigin.ShouldBe(VerticalAlignment.Top);

        sut.Anchor(Anchor.Center);
        sut.X.ShouldBe(0);
        sut.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        sut.XOrigin.ShouldBe(HorizontalAlignment.Center);
        sut.Y.ShouldBe(0);
        sut.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        sut.YOrigin.ShouldBe(VerticalAlignment.Center);
    }

    [Fact]
    public void GetAnchor_ShouldReturnAnchorCorrectly()
    {
        var sut = new GraphicalUiElement(new InvisibleRenderable());

        SetAndCheck(Anchor.TopLeft);
        SetAndCheck(Anchor.Top);
        SetAndCheck(Anchor.TopRight);

        SetAndCheck(Anchor.Left);
        SetAndCheck(Anchor.Center);
        SetAndCheck(Anchor.Right);

        SetAndCheck(Anchor.BottomLeft);
        SetAndCheck(Anchor.Bottom);
        SetAndCheck(Anchor.BottomRight);



        void SetAndCheck(Anchor anchor)
        {
            sut.Anchor(anchor);
            sut.GetAnchor().ShouldBe(anchor);
        }
    }

    [Fact]
    public void GetDock_ShouldReturnDockCorrectly()
    {
        var sut = new GraphicalUiElement(new InvisibleRenderable());

        SetAndCheck(Dock.Top);
        SetAndCheck(Dock.Left);
        SetAndCheck(Dock.Fill);

        SetAndCheck(Dock.Right);
        SetAndCheck(Dock.Bottom);

        // can't do this because need to set Y to something
        // non-zero so it doesn't get classified as Bottom
        //SetAndCheck(Dock.FillHorizontally);
        sut.Dock(Dock.FillHorizontally);
        sut.Y = 10;
        sut.GetDock().ShouldBe(Dock.FillHorizontally);

        sut.Dock(Dock.FillVertically);
        sut.X = 10;
        sut.GetDock().ShouldBe(Dock.FillVertically);

        SetAndCheck(Dock.SizeToChildren);


        void SetAndCheck(Dock dock)
        {
            sut.Dock(dock);
            sut.GetDock().ShouldBe(dock);
        }
    }

}
