using Gum.Forms.Controls;
using Gum.Wireframe;
using Gum.GueDeriving;
using MonoGameGum.Input;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;

public class ScrollViewerTests
{
    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        ScrollViewer scrollViewer = new();
        InteractiveGue visual = scrollViewer.Visual;

        List<ContainerRuntime> children = visual.Descendants().OfType<ContainerRuntime>().ToList();

        foreach (var child in children)
        {
            if (child.Name != "ThumbContainer")
            {
                child.HasEvents.ShouldBeFalse(
                    $"Because child {child.Name} with parent {child.Parent?.Name} should not be clickable, but it is so it eats events");
            }
        }
    }

    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var scrollViewer = new Gum.Forms.Controls.ScrollViewer();
        scrollViewer.Visual.ShouldNotBeNull();
        (scrollViewer.Visual is Gum.Forms.DefaultVisuals.ScrollViewerVisual).ShouldBeTrue();
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        ScrollViewer sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void MouseWheelScroll_ShouldMoveVerticalByMouseWheelScrollSpeed()
    {
        ScrollViewer scrollViewer = new();
        (ScrollBar verticalBar, _) = GetScrollBars(scrollViewer);
        verticalBar.Minimum = 0;
        verticalBar.Maximum = 1000;
        verticalBar.Value = 500;

        scrollViewer.MouseWheelScrollSpeed = 40;

        InvokeMouseWheel(scrollViewer, zVelocity: 1f);

        // One notch (positive ZVelocity) subtracts MouseWheelScrollSpeed from the value.
        verticalBar.Value.ShouldBe(460);
    }

    [Fact]
    public void MouseWheelScroll_WithShiftHeld_ShouldMoveHorizontalByMouseWheelScrollSpeed()
    {
        ScrollViewer scrollViewer = new();
        (ScrollBar verticalBar, ScrollBar horizontalBar) = GetScrollBars(scrollViewer);
        verticalBar.Minimum = 0;
        verticalBar.Maximum = 1000;
        verticalBar.Value = 500;
        horizontalBar.Minimum = 0;
        horizontalBar.Maximum = 1000;
        horizontalBar.Value = 500;

        scrollViewer.MouseWheelScrollSpeed = 40;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = new();
        mockKeyboard.Setup(k => k.IsShiftDown).Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);
        try
        {
            InvokeMouseWheel(scrollViewer, zVelocity: 1f);
        }
        finally
        {
            FrameworkElement.KeyboardsForUiControl.Clear();
        }

        verticalBar.Value.ShouldBe(500);
        horizontalBar.Value.ShouldBe(460);
    }

    [Fact]
    public void MouseWheelScrollSpeed_ShouldBeIndependentOfSmallChange()
    {
        ScrollViewer scrollViewer = new();
        (ScrollBar verticalBar, _) = GetScrollBars(scrollViewer);
        verticalBar.Minimum = 0;
        verticalBar.Maximum = 1000;
        verticalBar.Value = 500;

        // SmallChange is the arrow-button / line increment; it must no longer
        // affect mouse-wheel speed now that the two are decoupled.
        scrollViewer.SmallChange = 5;

        InvokeMouseWheel(scrollViewer, zVelocity: 1f);

        // Moves by the default MouseWheelScrollSpeed (30), not by SmallChange (5).
        verticalBar.Value.ShouldBe(470);
    }

    [Fact]
    public void MouseWheelScrollSpeed_ShouldDefaultToThirty()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.MouseWheelScrollSpeed.ShouldBe(30);
    }

    [Fact]
    public void ScrollViewerVisual_ShouldCreateScrollViewerForms()
    {
        var visual = new Gum.Forms.DefaultVisuals.ScrollViewerVisual();
        visual.FormsControl.ShouldNotBeNull();
    }

    [Fact]
    public void ShiftHeldDuringMouseWheel_ShouldRedirectScrollToHorizontal()
    {
        // Regression test pinning MonoGame V2 behavior after ScrollViewer.cs:~556
        // was flipped from !FRB && !RAYLIB to !FRB. MonoGame was already matched
        // by the previous guard (via !RAYLIB), so behavior must be unchanged.
        ScrollViewer scrollViewer = new();

        FieldInfo verticalField = typeof(ScrollViewer).GetField(
            "verticalScrollBar",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo horizontalField = typeof(ScrollViewer).GetField(
            "horizontalScrollBar",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        ScrollBar verticalBar = (ScrollBar)verticalField.GetValue(scrollViewer)!;
        ScrollBar horizontalBar = (ScrollBar)horizontalField.GetValue(scrollViewer)!;
        verticalBar.ShouldNotBeNull();
        horizontalBar.ShouldNotBeNull();
        verticalBar.Minimum = 0;
        verticalBar.Maximum = 1000;
        verticalBar.Value = 500;
        horizontalBar.Minimum = 0;
        horizontalBar.Maximum = 1000;
        horizontalBar.Value = 500;

        Mock<ICursor> mockCursor = new();
        mockCursor.Setup(c => c.ZVelocity).Returns(1f);
        ICursor? previousCursor = FrameworkElement.MainCursor;
        FrameworkElement.MainCursor = mockCursor.Object;
        try
        {
            Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = new();
            mockKeyboard.Setup(k => k.IsShiftDown).Returns(true);
            FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

            MethodInfo handler = typeof(ScrollViewer).GetMethod(
                "HandleMouseWheelScroll",
                BindingFlags.Instance | BindingFlags.NonPublic)!;

            double verticalBefore = verticalBar.Value;
            double horizontalBefore = horizontalBar.Value;

            handler.Invoke(scrollViewer, new object[] { scrollViewer.Visual, new RoutedEventArgs() });

            verticalBar.Value.ShouldBe(verticalBefore);
            horizontalBar.Value.ShouldNotBe(horizontalBefore);
        }
        finally
        {
            FrameworkElement.MainCursor = previousCursor;
            FrameworkElement.KeyboardsForUiControl.Clear();
        }
    }

    [Fact]
    public void ShiftHeldOnMainKeyboard_ShouldRedirectScrollToHorizontal()
    {
        // Regression test pinning MonoGame V2 behavior after the ScrollViewer.cs:~566
        // MainKeyboard fallback was un-gated (previously #if !RAYLIB). MonoGame was
        // already matched by the previous guard, so behavior must be unchanged. Also
        // confirms the new null-check guard is a no-op when MainKeyboard is non-null.
        ScrollViewer scrollViewer = new();

        FieldInfo verticalField = typeof(ScrollViewer).GetField(
            "verticalScrollBar",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo horizontalField = typeof(ScrollViewer).GetField(
            "horizontalScrollBar",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        ScrollBar verticalBar = (ScrollBar)verticalField.GetValue(scrollViewer)!;
        ScrollBar horizontalBar = (ScrollBar)horizontalField.GetValue(scrollViewer)!;
        verticalBar.ShouldNotBeNull();
        horizontalBar.ShouldNotBeNull();
        verticalBar.Minimum = 0;
        verticalBar.Maximum = 1000;
        verticalBar.Value = 500;
        horizontalBar.Minimum = 0;
        horizontalBar.Maximum = 1000;
        horizontalBar.Value = 500;

        Mock<ICursor> mockCursor = new();
        mockCursor.Setup(c => c.ZVelocity).Returns(1f);
        ICursor? previousCursor = FrameworkElement.MainCursor;
        FrameworkElement.MainCursor = mockCursor.Object;

        // Force the MainKeyboard fallback path by leaving KeyboardsForUiControl empty.
        FrameworkElement.KeyboardsForUiControl.Count.ShouldBe(0);

        IInputReceiverKeyboard? previousMainKeyboard = FrameworkElement.MainKeyboard;
        Mock<IInputReceiverKeyboard> mockKeyboard = new();
        mockKeyboard.Setup(k => k.IsShiftDown).Returns(true);
        FrameworkElement.MainKeyboard = mockKeyboard.Object;
        try
        {
            MethodInfo handler = typeof(ScrollViewer).GetMethod(
                "HandleMouseWheelScroll",
                BindingFlags.Instance | BindingFlags.NonPublic)!;

            double verticalBefore = verticalBar.Value;
            double horizontalBefore = horizontalBar.Value;

            handler.Invoke(scrollViewer, new object[] { scrollViewer.Visual, new RoutedEventArgs() });

            verticalBar.Value.ShouldBe(verticalBefore);
            horizontalBar.Value.ShouldNotBe(horizontalBefore);
        }
        finally
        {
            FrameworkElement.MainCursor = previousCursor;
            FrameworkElement.MainKeyboard = previousMainKeyboard;
        }
    }

    static (ScrollBar vertical, ScrollBar horizontal) GetScrollBars(ScrollViewer scrollViewer)
    {
        FieldInfo verticalField = typeof(ScrollViewer).GetField(
            "verticalScrollBar",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo horizontalField = typeof(ScrollViewer).GetField(
            "horizontalScrollBar",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        ScrollBar vertical = (ScrollBar)verticalField.GetValue(scrollViewer)!;
        ScrollBar horizontal = (ScrollBar)horizontalField.GetValue(scrollViewer)!;
        vertical.ShouldNotBeNull();
        horizontal.ShouldNotBeNull();
        return (vertical, horizontal);
    }

    static void InvokeMouseWheel(ScrollViewer scrollViewer, float zVelocity)
    {
        Mock<ICursor> mockCursor = new();
        mockCursor.Setup(c => c.ZVelocity).Returns(zVelocity);
        ICursor? previousCursor = FrameworkElement.MainCursor;
        FrameworkElement.MainCursor = mockCursor.Object;
        try
        {
            MethodInfo handler = typeof(ScrollViewer).GetMethod(
                "HandleMouseWheelScroll",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            handler.Invoke(scrollViewer, new object[] { scrollViewer.Visual, new RoutedEventArgs() });
        }
        finally
        {
            FrameworkElement.MainCursor = previousCursor;
        }
    }
}
