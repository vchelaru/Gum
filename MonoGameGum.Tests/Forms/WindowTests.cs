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

    [Fact]
    public void Resizing_ShouldNotShrinkOrShift_BeyondMinimumWidth_LeftSide()
    {
        Mock<ICursor> cursor = CreateMockCursor();

        Window sut = new();
        sut.AddToRoot();

        InteractiveGue left =
            (InteractiveGue)sut.Visual.GetChildByNameRecursively("BorderLeftInstance");

        sut.Visual.Width = 20;
        sut.Visual.MinWidth = 20;

        left.TryCallPush();
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(300);
        left.TryCallDragging();
        sut.X.ShouldBe(0);
        sut.Width.ShouldBe(20);

        sut.Visual.X = 10;
        sut.Visual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
        left.TryCallDragging();
        sut.X.ShouldBe(10);
        sut.Width.ShouldBe(20);

        sut.Visual.X = 20;
        sut.Visual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;
        left.TryCallDragging();
        sut.X.ShouldBe(20);
        sut.Width.ShouldBe(20);
    }

    [Fact]
    public void Resizing_ShouldNotShrinkOrShift_BeyondMinimumWidth_RightSide()
    {
        Mock<ICursor> cursor = CreateMockCursor();

        Window sut = new();
        sut.AddToRoot();

        InteractiveGue right =
            (InteractiveGue)sut.Visual.GetChildByNameRecursively("BorderRightInstance");

        sut.Visual.Width = 20;
        sut.Visual.MinWidth = 20;
        sut.X = 100;

        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(119);

        right.TryCallPush();
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(0);
        right.TryCallDragging();
        sut.X.ShouldBe(100);
        sut.Width.ShouldBe(20);

        sut.X = 110;
        sut.Visual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
        right.TryCallDragging();
        sut.X.ShouldBe(110);
        sut.Width.ShouldBe(20);

        sut.X = 120;
        sut.Visual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;
        right.TryCallDragging();
        sut.X.ShouldBe(120);
        sut.Width.ShouldBe(20);
    }

    [Fact]
    public void Resizing_ShouldNotShrinkOrShift_BeyondMinimumWidth_TopSide()
    {
        Mock<ICursor> cursor = CreateMockCursor();

        Window sut = new();
        sut.AddToRoot();

        InteractiveGue top =
            (InteractiveGue)sut.Visual.GetChildByNameRecursively("BorderTopInstance");

        sut.Visual.Height = 20;
        sut.Visual.MinHeight = 20;

        top.TryCallPush();
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(300);
        top.TryCallDragging();
        sut.Y.ShouldBe(0);
        sut.Height.ShouldBe(20);

        sut.Visual.Y = 10;
        sut.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
        top.TryCallDragging();
        sut.Y.ShouldBe(10);
        sut.Height.ShouldBe(20);

        sut.Visual.Y = 20;
        sut.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        top.TryCallDragging();
        sut.Y.ShouldBe(20);
        sut.Height.ShouldBe(20);
    }

    [Fact]
    public void Resizing_ShouldNotShrinkOrShift_BeyondMinimumHeight_BottomSide()
    {
        Mock<ICursor> cursor = CreateMockCursor();

        Window sut = new();
        sut.AddToRoot();

        InteractiveGue bottom =
            (InteractiveGue)sut.Visual.GetChildByNameRecursively("BorderBottomInstance");

        sut.Visual.Height = 20;
        sut.Visual.MinHeight = 20;
        sut.Y = 100;

        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(119);

        bottom.TryCallPush();
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(0);
        bottom.TryCallDragging();
        sut.Y.ShouldBe(100);
        sut.Height.ShouldBe(20);

        sut.Y = 110;
        sut.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
        bottom.TryCallDragging();
        sut.Y.ShouldBe(110);
        sut.Height.ShouldBe(20);

        sut.Y = 120;
        sut.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        bottom.TryCallDragging();
        sut.Y.ShouldBe(120);
        sut.Height.ShouldBe(20);
    }

    private static Mock<ICursor> CreateMockCursor()
    {
        Mock<ICursor> cursor = new();
        FormsUtilities.SetCursor(cursor.Object);
        cursor.SetupProperty(x => x.WindowPushed);
        cursor.SetupProperty(x => x.WindowOver);
        return cursor;
    }
}
