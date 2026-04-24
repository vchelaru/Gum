using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Input;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class TooltipTests : BaseTestClass
{
    public TooltipTests()
    {
        ToolTipService.ResetForTesting();
        ToolTipService.InitialShowDelay = TimeSpan.FromMilliseconds(500);
        ToolTipService.ShowDuration = TimeSpan.FromMilliseconds(5000);
        ToolTipService.BetweenShowDelay = TimeSpan.FromMilliseconds(100);
    }

    public override void Dispose()
    {
        ToolTipService.ResetForTesting();
        base.Dispose();
    }

    [Fact]
    public void BetweenShowDelay_SkipsDelayOnRapidReHover()
    {
        Button button = new Button();
        button.AddToRoot();
        button.ToolTip = "hi";

        Button other = new Button();
        other.Visual.X = 200;
        other.AddToRoot();

        Mock<ICursor> cursor = SetupCursor();
        cursor.Setup(x => x.VisualOver).Returns(button.Visual);

        TickTo(cursor, totalSeconds: 0.0);
        TickTo(cursor, totalSeconds: 0.6);
        FrameworkElement.PopupRoot.Children.Count.ShouldBe(1);

        // leave the hover, closing the tooltip
        cursor.Setup(x => x.VisualOver).Returns(other.Visual);
        TickTo(cursor, totalSeconds: 0.65);
        FrameworkElement.PopupRoot.Children.Count.ShouldBe(0);

        // re-hover within BetweenShowDelay (100ms). Next tick should re-open immediately.
        cursor.Setup(x => x.VisualOver).Returns(button.Visual);
        TickTo(cursor, totalSeconds: 0.70);
        FrameworkElement.PopupRoot.Children.Count.ShouldBe(1,
            "because re-hover within BetweenShowDelay should skip the InitialShowDelay");
    }

    [Fact]
    public void Hover_AfterInitialShowDelay_AddsVisualToPopupRoot()
    {
        Button button = new Button();
        button.AddToRoot();
        button.ToolTip = "hello";

        Mock<ICursor> cursor = SetupCursor();
        cursor.Setup(x => x.VisualOver).Returns(button.Visual);

        // First tick establishes hover-start; second tick measures elapsed.
        TickTo(cursor, totalSeconds: 0.0);
        TickTo(cursor, totalSeconds: 0.6);

        FrameworkElement.PopupRoot.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void Hover_BeforeInitialShowDelay_DoesNotShow()
    {
        Button button = new Button();
        button.AddToRoot();
        button.ToolTip = "hello";

        Mock<ICursor> cursor = SetupCursor();
        cursor.Setup(x => x.VisualOver).Returns(button.Visual);

        TickTo(cursor, totalSeconds: 0.1);

        FrameworkElement.PopupRoot.Children.Count.ShouldBe(0);
    }

    [Fact]
    public void Hover_WithMovement_ResetsDelay()
    {
        Button button = new Button();
        button.AddToRoot();
        button.ToolTip = "hello";

        Mock<ICursor> cursor = SetupCursor();
        cursor.Setup(x => x.VisualOver).Returns(button.Visual);
        cursor.Setup(x => x.XChange).Returns(5);

        TickTo(cursor, totalSeconds: 0.0);
        TickTo(cursor, totalSeconds: 0.6);

        FrameworkElement.PopupRoot.Children.Count.ShouldBe(0,
            "because cursor movement should reset the stationary-hover timer");
    }

    [Fact]
    public void Leave_WhileOpen_RemovesVisualFromPopupRoot()
    {
        Button button = new Button();
        button.AddToRoot();
        button.ToolTip = "hello";

        Mock<ICursor> cursor = SetupCursor();
        cursor.Setup(x => x.VisualOver).Returns(button.Visual);
        TickTo(cursor, totalSeconds: 0.0);
        TickTo(cursor, totalSeconds: 0.6);
        FrameworkElement.PopupRoot.Children.Count.ShouldBe(1);

        cursor.Setup(x => x.VisualOver).Returns((InteractiveGue?)null);
        TickTo(cursor, totalSeconds: 0.65);

        FrameworkElement.PopupRoot.Children.Count.ShouldBe(0);
    }

    [Fact]
    public void Positioning_NearRightEdge_ReposToStayOnScreen()
    {
        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;

        Tooltip tooltip = new Tooltip();
        tooltip.Content = "The quick brown fox jumps over the lazy dog";

        tooltip.Show(cursorX: 790, cursorY: 10);

        tooltip.Visual.X.ShouldBeLessThanOrEqualTo(800 - tooltip.Visual.GetAbsoluteWidth());
    }

    [Fact]
    public void Show_Hide_Programmatic_RaisesOpenedClosedEvents()
    {
        Tooltip tooltip = new Tooltip();
        tooltip.Content = "hi";

        bool opened = false;
        bool closed = false;
        tooltip.Opened += (_, _) => opened = true;
        tooltip.Closed += (_, _) => closed = true;

        tooltip.Show(cursorX: 10, cursorY: 10);
        opened.ShouldBeTrue();
        tooltip.IsOpen.ShouldBeTrue();

        tooltip.Hide();
        closed.ShouldBeTrue();
        tooltip.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void ShowDuration_Elapsed_AutoHides()
    {
        ToolTipService.ShowDuration = TimeSpan.FromMilliseconds(1000);

        Button button = new Button();
        button.AddToRoot();
        button.ToolTip = "hello";

        Mock<ICursor> cursor = SetupCursor();
        cursor.Setup(x => x.VisualOver).Returns(button.Visual);

        TickTo(cursor, totalSeconds: 0.0);
        TickTo(cursor, totalSeconds: 0.6);
        FrameworkElement.PopupRoot.Children.Count.ShouldBe(1);

        // 0.6 (show) + 1.0 (duration) + small margin
        TickTo(cursor, totalSeconds: 1.8);
        FrameworkElement.PopupRoot.Children.Count.ShouldBe(0);
    }

    [Fact]
    public void ToolTip_NonStringContent_Throws()
    {
        Button button = new Button();
        Should.Throw<NotSupportedException>(() => button.ToolTip = 42);
    }

    [Fact]
    public void ToolTip_SetString_AssignsContent()
    {
        Button button = new Button();

        button.ToolTip = "hello";

        button.ToolTip.ShouldBe("hello");
        Tooltip? associated = ToolTipService.GetTooltip(button);
        associated.ShouldNotBeNull();
        associated!.Content.ShouldBe("hello");
    }

    private static Mock<ICursor> SetupCursor()
    {
        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.SetupProperty(x => x.VisualOver);
        cursor.SetupProperty(x => x.WindowPushed);
        cursor.Setup(x => x.X).Returns(100);
        cursor.Setup(x => x.Y).Returns(100);
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(100f);
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(100f);
        FormsUtilities.SetCursor(cursor.Object);
        return cursor;
    }

    private static void TickTo(Mock<ICursor> cursor, double totalSeconds)
    {
        GameTime gameTime = new GameTime(
            totalGameTime: TimeSpan.FromSeconds(totalSeconds),
            elapsedGameTime: TimeSpan.FromMilliseconds(16));
        GumService.Default.Update(gameTime);
    }
}
