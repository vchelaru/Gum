using InputLibrary;
using Shouldly;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        // VisualTreeHelper.GetDpi falls back to the *system's* current DPI even with no live
        // PresentationSource - it is NOT guaranteed to be 1.0 here, so the expectation has to be
        // computed from whatever this machine reports rather than hardcoded, or this test would be
        // flaky across dev machines/CI runners at different display scales.
        double dpiScale = VisualTreeHelper.GetDpi(element).DpiScaleX;
        adapter.Width.ShouldBe(WpfInputHostAdapter.ToPhysicalPixels(320, dpiScale));
        adapter.Height.ShouldBe(WpfInputHostAdapter.ToPhysicalPixels(240, dpiScale));
    }

    // #4681: Cursor/SelectionManager/the drag handlers all feed IInputHostControl.Width/Height and
    // PointToClient into RenderingLibrary.Camera, which now works in physical pixels (the render
    // target XnaAndWinforms.WpfGraphicsDeviceControl sizes itself to). WPF's own units are DIU, so
    // this adapter must convert - otherwise hit-testing and dragging read as if the camera were still
    // zoomed by the display's DPI scale, independent of the (correct) rendered visuals. The live DPI
    // read can't be driven from a unit test (no real per-monitor-scaled window to source it from), so
    // this pins only the pure conversion math, not the end-to-end behavior - that's a manual check.
    [Theory]
    [InlineData(320.0, 1.0, 320)]
    [InlineData(320.0, 2.0, 640)]
    [InlineData(160.6, 1.0, 161)]
    [InlineData(0.0, 2.0, 0)]
    [InlineData(-50.0, 2.0, -100)]
    public void ToPhysicalPixels_ConvertsDiuValueByDpiScale(double diuValue, double dpiScale, int expected)
    {
        WpfInputHostAdapter.ToPhysicalPixels(diuValue, dpiScale).ShouldBe(expected);
    }
}
