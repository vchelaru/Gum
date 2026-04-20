using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
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

        List<ContainerRuntime> children = new();
        visual.FillListWithChildrenByTypeRecursively<ContainerRuntime>(children);

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
}
