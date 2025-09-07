using Gum.Wireframe;
using Gum.Forms;
using Gum.Forms.Controls;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Gum.Forms.DefaultVisuals;

namespace MonoGameGum.Tests.Forms;
public class WindowTests : BaseTestClass
{
    [Fact]
    public void Constructor_CreatesVisual()
    {
        var window = new Window();

        window.Visual.ShouldNotBeNull();

        window.InnerPanel.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateWindowWithEdgesAndTitleBar()
    {
        var window = new Window();


        window.GetFrameworkElement("BorderTopLeftInstance").ShouldNotBeNull();
        window.GetFrameworkElement("BorderTopRightInstance").ShouldNotBeNull();
        window.GetFrameworkElement("BorderBottomLeftInstance").ShouldNotBeNull();
        window.GetFrameworkElement("BorderBottomRightInstance").ShouldNotBeNull();

        window.GetFrameworkElement("BorderTopInstance").ShouldNotBeNull();
        window.GetFrameworkElement("BorderBottomInstance").ShouldNotBeNull();
        window.GetFrameworkElement("BorderRightInstance").ShouldNotBeNull();
        window.GetFrameworkElement("BorderLeftInstance").ShouldNotBeNull();
    }

    [Fact]
    public void Dragging_ShouldMoveWindow()
    {
        Mock<ICursor> cursor = new ();
        Window sut = new();
        FrameworkElement.MainCursor = cursor.Object;


        var titleBar = (InteractiveGue)sut.GetVisual("TitleBarInstance")!;
        titleBar.TryCallPush();
        titleBar.TryCallDragging();

        sut.X.ShouldBe(0);
        sut.Y.ShouldBe(0);

        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(10);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(20);

        titleBar.TryCallDragging();

        sut.X.ShouldBe(10);
        sut.Y.ShouldBe(20);
    }

    [Fact]
    public void Dragging_ShouldMoveWindow_IfInsideChild()
    {

        Mock<ICursor> cursor = new();

        Panel parentPanel = new Panel();
        parentPanel.Width = 200;
        parentPanel.Height = 200;
        parentPanel.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        parentPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        Window sut = new();
        parentPanel.AddChild(sut);
        FrameworkElement.MainCursor = cursor.Object;


        var titleBar = (InteractiveGue)sut.GetVisual("TitleBarInstance")!;
        titleBar.TryCallPush();
        titleBar.TryCallDragging();

        sut.X.ShouldBe(0);
        sut.Y.ShouldBe(0);

        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(100);

        titleBar.TryCallDragging();

        sut.X.ShouldBe(100);
    }

    [Fact]
    public void Dragging_ShouldNotMoveWindow_IfOutsideOfChild()
    {

        Mock<ICursor> cursor = new();

        Panel parentPanel = new Panel();
        parentPanel.Width = 10;
        parentPanel.Height = 10;
        parentPanel.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        parentPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        Window sut = new();
        parentPanel.AddChild(sut);
        FrameworkElement.MainCursor = cursor.Object;


        var titleBar = (InteractiveGue)sut.GetVisual("TitleBarInstance")!;
        titleBar.TryCallPush();
        titleBar.TryCallDragging();

        sut.X.ShouldBe(0);
        sut.Y.ShouldBe(0);

        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(100);

        titleBar.TryCallDragging();

        sut.X.ShouldBe(0, "because the cursor was moved it too far out");
    }
}
