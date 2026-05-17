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
    public void ResizeMode_ShouldSetCursor()
    {
        Window sut = new();
        InteractiveGue Visual = sut.Visual;

        var borderTopLeft = GetFrameworkElement("BorderTopLeftInstance");
        var borderTop = GetFrameworkElement("BorderTopInstance");
        var borderTopRight = GetFrameworkElement("BorderTopRightInstance");
        var borderLeft = GetFrameworkElement("BorderLeftInstance");
        var borderRight = GetFrameworkElement("BorderRightInstance");
        var borderBottomLeft = GetFrameworkElement("BorderBottomLeftInstance");
        var borderBottom = GetFrameworkElement("BorderBottomInstance");
        var borderBottomRight = GetFrameworkElement("BorderBottomRightInstance");

        sut.ResizeMode = ResizeMode.CanResize;

        borderTopLeft.CustomCursor.ShouldNotBe(null);
        borderTop.CustomCursor.ShouldNotBe(null);
        borderTopRight.CustomCursor.ShouldNotBe(null);
        borderLeft.CustomCursor.ShouldNotBe(null);
        borderRight.CustomCursor.ShouldNotBe(null);
        borderBottomLeft.CustomCursor.ShouldNotBe(null);
        borderBottom.CustomCursor.ShouldNotBe(null);
        borderBottomRight.CustomCursor.ShouldNotBe(null);

        sut.ResizeMode = ResizeMode.NoResize;

        borderTopLeft.CustomCursor.ShouldBe(null);
        borderTop.CustomCursor.ShouldBe(null);
        borderTopRight.CustomCursor.ShouldBe(null);
        borderLeft.CustomCursor.ShouldBe(null);
        borderRight.CustomCursor.ShouldBe(null);
        borderBottomLeft.CustomCursor.ShouldBe(null);
        borderBottom.CustomCursor.ShouldBe(null);
        borderBottomRight.CustomCursor.ShouldBe(null);

        FrameworkElement GetFrameworkElement(string name)
        {
            InteractiveGue visual = (InteractiveGue)Visual.GetGraphicalUiElementByName("BorderTopLeftInstance")!;
            return (FrameworkElement)visual.FormsControlAsObject;
        }
    }

    [Fact]
    public void Resizing_ShouldNotShrinkOrShift_BeyondMinimumWidth_LeftSide()
    {
        Mock<ICursor> cursor = CreateMockCursor();

        Window sut = new();
        sut.AddToRoot();

        InteractiveGue left =
            sut.Visual.Find<InteractiveGue>("BorderLeftInstance")!;

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
            sut.Visual.Find<InteractiveGue>("BorderRightInstance")!;

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
            sut.Visual.Find<InteractiveGue>("BorderTopInstance")!;

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
            sut.Visual.Find<InteractiveGue>("BorderBottomInstance")!;

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

    [Fact]
    public void RemoveChild_ShouldRemoveChildFromWindow()
    {
        Window sut = new();
        Panel child = new();
        sut.AddChild(child);
        sut.RemoveChild(child);
        sut.InnerPanel.Children.ShouldNotContain(child.Visual);
        sut.Children.ShouldNotContain(child);
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        Window sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }
    [Fact]
    public void Resizing_ShouldNotShiftWindow_WhenDraggingBottomBeyondMinHeight_StartingAboveMin()
    {
        // Scenario from issue #2762: window starts taller than MinHeight,
        // user drags the bottom edge up far past the min — window should
        // shrink to MinHeight and stop, without the window getting pushed.
        Mock<ICursor> cursor = CreateMockCursor();

        Window sut = new();
        sut.AddToRoot();

        InteractiveGue bottom =
            sut.Visual.Find<InteractiveGue>("BorderBottomInstance")!;

        sut.Visual.Y = 100;
        sut.Visual.Height = 200;
        sut.Visual.MinHeight = 50;

        // Push the bottom edge with the cursor near the bottom.
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(300);
        bottom.TryCallPush();

        // Drag the cursor up to where shrinking would take height below the min.
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(50);
        bottom.TryCallDragging();

        sut.Y.ShouldBe(100);
        sut.Height.ShouldBe(50);

        // And a second drag at the same extreme cursor position should not
        // accumulate any further shift.
        bottom.TryCallDragging();
        sut.Y.ShouldBe(100);
        sut.Height.ShouldBe(50);
    }

    [Fact]
    public void Resizing_ShouldNotShiftWindow_WhenDraggingTopBeyondMinHeight_HeightUnitsRelativeToChildren()
    {
        // Repro from issue: HeightUnits = RelativeToChildren with Height = 0 and
        // MinHeight = 256 means MinHeight is what determines the absolute height.
        // Dragging the top edge must not shift the window downward.
        Mock<ICursor> cursor = CreateMockCursor();

        Window sut = new();
        sut.AddToRoot();

        // Add a small child so InnerPanel reports a non-zero children size,
        // but well below MinHeight so MinHeight is the binding constraint.
        Panel child = new();
        child.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        child.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        child.Width = 30;
        child.Height = 30;
        sut.AddChild(child);

        sut.Visual.Y = 100;
        sut.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Top;
        sut.Visual.Height = 0;
        sut.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        sut.Visual.MinHeight = 256;

        InteractiveGue top =
            sut.Visual.Find<InteractiveGue>("BorderTopInstance")!;

        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(100);
        top.TryCallPush();

        // Drag the top edge down past where shrinking would violate the min.
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(400);
        top.TryCallDragging();

        sut.Y.ShouldBe(100);
        sut.Visual.GetAbsoluteHeight().ShouldBe(256);

        // A second drag at the same cursor position must not push further.
        top.TryCallDragging();
        sut.Y.ShouldBe(100);
        sut.Visual.GetAbsoluteHeight().ShouldBe(256);
    }

    [Fact]
    public void Resizing_ShouldNotShiftWindow_WhenDraggingTopBeyondMinHeight_StartingAboveMin()
    {
        Mock<ICursor> cursor = CreateMockCursor();

        Window sut = new();
        sut.AddToRoot();

        InteractiveGue top =
            sut.Visual.Find<InteractiveGue>("BorderTopInstance")!;

        sut.Visual.Y = 100;
        sut.Visual.Height = 200;
        sut.Visual.MinHeight = 50;
        // AbsoluteTop = 100, AbsoluteBottom = 300

        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(100);
        top.TryCallPush();

        // Drag the top edge down — past where height would shrink below the min.
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(400);
        top.TryCallDragging();

        // Top can move down until height hits min; AbsoluteBottom should stay at 300.
        sut.Y.ShouldBe(250);
        sut.Height.ShouldBe(50);

        // A second drag at the same cursor position must not push further.
        top.TryCallDragging();
        sut.Y.ShouldBe(250);
        sut.Height.ShouldBe(50);
    }

    #region Utilities

    private static Mock<ICursor> CreateMockCursor()
    {
        Mock<ICursor> cursor = new();
        FormsUtilities.SetCursor(cursor.Object);
        cursor.SetupProperty(x => x.WindowPushed);
        cursor.SetupProperty(x => x.VisualOver);
        return cursor;
    }

    #endregion
}
