using Gum.Forms.Controls;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Reflection;
using Keys = Gum.Forms.Input.Keys;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// End-to-end Raylib keyboard coverage for <see cref="ScrollViewer"/>. Pins the
/// behavior unlocked by flipping the two guards in
/// <c>MonoGameGum/Forms/Controls/ScrollViewer.cs</c> at lines 556 and 771 from
/// <c>!FRB &amp;&amp; !RAYLIB</c> / <c>(MONOGAME || KNI || FNA) &amp;&amp; !FRB</c>
/// to <c>!FRB</c>.
/// </summary>
public class ScrollViewerTests : BaseTestClass
{
    [Fact]
    public void ScrollViewer_ClickComboPushed_DropsFocusIntoItems()
    {
        // Covers Site 2 (line 771): with shift-focus on the ScrollViewer itself
        // (DoItemsHaveFocus = false), pressing a ClickCombo key (default: Enter)
        // should drop focus into items. Pre-flip this block was gated off on Raylib,
        // so DoItemsHaveFocus would stay false even with Enter pushed.
        ScrollViewer scrollViewer = new ScrollViewer();

        scrollViewer.Visual.ShouldNotBeNull();
        scrollViewer.AddToRoot();

        scrollViewer.DoItemsHaveFocus = false;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        // BaseTestClass.Dispose seeds ClickCombos with { PushedKey = Enter, HeldKey = null }.
        // IsComboPushed() then calls keyboard.KeyPushed(Enter) with no held key.
        keyboard.Setup(k => k.KeyPushed(Keys.Enter)).Returns(true);
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        scrollViewer.OnFocusUpdate();

        scrollViewer.DoItemsHaveFocus.ShouldBeTrue();
    }

    [Fact]
    public void ScrollViewer_ShiftHeldDuringMouseWheel_ScrollsHorizontally()
    {
        // Covers Site 1 (line 556): an explicit IInputReceiverKeyboard in
        // KeyboardsForUiControl with IsShiftDown == true should redirect mouse-wheel
        // scroll from vertical to horizontal. Pre-flip this loop was gated off on
        // Raylib, so shift was never detected and the wheel always scrolled vertically.
        ScrollViewer scrollViewer = new ScrollViewer();

        scrollViewer.Visual.ShouldNotBeNull();
        scrollViewer.AddToRoot();

        // Reach into the protected scroll bars to widen their Max range so Value
        // actually has room to change. Without this, ScrollBar.Value clamps to
        // Minimum=Maximum=0 and both axes appear "not to move" regardless of guard,
        // which would make the test a false pass pre-flip.
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

        // Mock the cursor so ZVelocity is a known non-zero value.
        Mock<ICursor> cursor = new Mock<ICursor>();
        cursor.Setup(c => c.ZVelocity).Returns(1f);
        FrameworkElement.MainCursor = cursor.Object;

        // Explicit keyboard with shift held. hasExplicitKeyboardsForUiControl flips
        // true inside the loop, short-circuiting the MainKeyboard fallback.
        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.IsShiftDown).Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        // HandleMouseWheelScroll is private; invoke via reflection rather than drive
        // the full cursor->InteractiveGue event pipeline, which would require
        // simulating raylib mouse state. The design doc explicitly allows this.
        MethodInfo handler = typeof(ScrollViewer).GetMethod(
            "HandleMouseWheelScroll",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        handler.ShouldNotBeNull();

        double verticalBefore = verticalBar.Value;
        double horizontalBefore = horizontalBar.Value;

        handler.Invoke(scrollViewer, new object[] { scrollViewer.Visual, new RoutedEventArgs() });

        verticalBar.Value.ShouldBe(verticalBefore,
            "because shift-held should suppress vertical scroll on Raylib");
        horizontalBar.Value.ShouldNotBe(horizontalBefore,
            "because shift-held should redirect wheel scroll to the horizontal bar");
    }

    [Fact]
    public void ScrollViewer_ShiftHeldOnMainKeyboard_ScrollsHorizontally()
    {
        // Covers the MainKeyboard fallback branch at ScrollViewer.cs:~566. When
        // KeyboardsForUiControl is empty, the ScrollViewer falls back to reading
        // FrameworkElement.MainKeyboard.IsShiftDown. Pre un-gate this line was
        // wrapped in #if !RAYLIB, so shift-scroll fallback was dead on Raylib.
        ScrollViewer scrollViewer = new ScrollViewer();

        scrollViewer.Visual.ShouldNotBeNull();
        scrollViewer.AddToRoot();

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

        Mock<ICursor> cursor = new Mock<ICursor>();
        cursor.Setup(c => c.ZVelocity).Returns(1f);
        FrameworkElement.MainCursor = cursor.Object;

        // Leave KeyboardsForUiControl empty to force the MainKeyboard fallback path.
        FrameworkElement.KeyboardsForUiControl.Count.ShouldBe(0,
            "because BaseTestClass.Dispose clears this and the test must hit the fallback branch");

        IInputReceiverKeyboard? previousMainKeyboard = FrameworkElement.MainKeyboard;
        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.IsShiftDown).Returns(true);
        FrameworkElement.MainKeyboard = keyboard.Object;
        try
        {
            MethodInfo handler = typeof(ScrollViewer).GetMethod(
                "HandleMouseWheelScroll",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            handler.ShouldNotBeNull();

            double verticalBefore = verticalBar.Value;
            double horizontalBefore = horizontalBar.Value;

            handler.Invoke(scrollViewer, new object[] { scrollViewer.Visual, new RoutedEventArgs() });

            verticalBar.Value.ShouldBe(verticalBefore,
                "because shift-held on MainKeyboard should suppress vertical scroll on Raylib");
            horizontalBar.Value.ShouldNotBe(horizontalBefore,
                "because shift-held on MainKeyboard should redirect wheel scroll to the horizontal bar");
        }
        finally
        {
            FrameworkElement.MainKeyboard = previousMainKeyboard;
        }
    }
}
