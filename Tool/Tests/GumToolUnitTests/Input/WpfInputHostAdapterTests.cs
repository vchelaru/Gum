using InputLibrary;
using Shouldly;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GumToolUnitTests.Input;

public class WpfInputHostAdapterTests
{
    [StaFact]
    public void Cursor_Get_ForwardsToWrappedElement()
    {
        Border element = new Border { Cursor = Cursors.Hand };
        WpfInputHostAdapter adapter = new WpfInputHostAdapter(element);

        adapter.Cursor.ShouldBe(CursorKind.Hand);
    }

    [StaFact]
    public void Cursor_Get_UnmappedWpfCursorFallsBackToArrow()
    {
        Border element = new Border { Cursor = Cursors.Wait };
        WpfInputHostAdapter adapter = new WpfInputHostAdapter(element);

        adapter.Cursor.ShouldBe(CursorKind.Arrow);
    }

    [StaTheory]
    [InlineData(CursorKind.Arrow)]
    [InlineData(CursorKind.Cross)]
    [InlineData(CursorKind.Hand)]
    [InlineData(CursorKind.SizeAll)]
    [InlineData(CursorKind.SizeNS)]
    [InlineData(CursorKind.SizeWE)]
    [InlineData(CursorKind.SizeNESW)]
    [InlineData(CursorKind.SizeNWSE)]
    public void Cursor_RoundTripsThroughWrappedElement(CursorKind cursorKind)
    {
        Border element = new Border();
        WpfInputHostAdapter adapter = new WpfInputHostAdapter(element);

        adapter.Cursor = cursorKind;

        adapter.Cursor.ShouldBe(cursorKind);
    }

    [StaFact]
    public void Cursor_Set_ForwardsToWrappedElement()
    {
        Border element = new Border();
        WpfInputHostAdapter adapter = new WpfInputHostAdapter(element);

        adapter.Cursor = CursorKind.Cross;

        element.Cursor.ShouldBe(Cursors.Cross);
    }

    [StaFact]
    public void WidthAndHeight_ForwardToWrappedElementAfterLayout()
    {
        Border element = new Border { Width = 320, Height = 240 };
        // FrameworkElement.ActualWidth/ActualHeight are only populated after a measure+arrange
        // pass. Both can run directly against an off-screen element - no live window is needed.
        element.Measure(new Size(320, 240));
        element.Arrange(new Rect(0, 0, 320, 240));
        WpfInputHostAdapter adapter = new WpfInputHostAdapter(element);

        adapter.Width.ShouldBe(320);
        adapter.Height.ShouldBe(240);
    }
}
