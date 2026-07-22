using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// Per-frame allocation baseline and regression guard for scrolling a <see cref="ListBox"/>
/// (issue #1934). Gum's ListBox does not virtualize/recycle item containers — every item already
/// has a persistent visual — so a steady scroll (changing <see cref="ScrollViewer.VerticalScrollBarValue"/>
/// each frame) only repositions existing visuals and should allocate nothing per frame. This test
/// measures that to confirm rather than assume, and pins it as a regression guard.
/// </summary>
public class ListBoxScrollAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public ListBoxScrollAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Builds a fixed-height <see cref="ListBox"/> populated with <paramref name="itemCount"/>
    /// string items and lays it out. The item count is chosen so the content overflows the clip
    /// region, giving a non-zero scroll range to drive.
    /// </summary>
    private static ListBox BuildScrollableListBox(int itemCount)
    {
        ListBox listBox = new();
        listBox.Width = 200;
        listBox.Height = 200;

        for (int i = 0; i < itemCount; i++)
        {
            listBox.Items!.Add("Item " + i);
        }

        listBox.Visual.UpdateLayout();
        return listBox;
    }

    [Fact]
    public void Scrolling_ListBox_IsZeroAllocation()
    {
        const int itemCount = 60;

        // Use a real Cursor rather than the Moq mock BaseTestClass installs: the ScrollBar value
        // setter reads MainCursor.WindowPushed twice per change, and each read through a Moq proxy
        // allocates (~296 bytes), which would pollute the measurement with a pure test artifact.
        // Production always uses a real Cursor, so this measures the real per-frame cost.
        FrameworkElement.MainCursor = new MonoGameGum.Input.Cursor(null);

        ListBox listBox = BuildScrollableListBox(itemCount);

        // Sanity: the content must actually overflow, otherwise there is no scroll range to drive
        // and a zero-allocation result would be meaningless.
        listBox.VerticalScrollBarMaximum.ShouldBeGreaterThan(0);

        // Oscillate the scroll value between the top and the midpoint every frame so each frame is a
        // real reposition of every item visual (a steady drag). Both endpoints are inside the valid
        // range, so the value genuinely changes on every call and cannot silently saturate.
        double midpoint = listBox.VerticalScrollBarMaximum / 2.0;

        int scrollChangedCount = 0;
        listBox.ScrollChanged += (_, _) => scrollChangedCount++;

        const int measuredIterations = 500;
        const int attempts = 3;

        bool atTop = true;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                listBox.VerticalScrollBarValue = atTop ? midpoint : 0;
                atTop = !atTop;
            },
            attempts: attempts,
            warmupIterations: 50,
            measuredIterations: measuredIterations);

        _output.WriteLine($"Scrolling a {itemCount}-item ListBox (no virtualization): " +
            $"{result.BytesPerIteration:N0} bytes/frame ({result.TotalBytes:N0} bytes over {result.Iterations} frames)");

        // Liveness: prove every scroll actually repositioned content (ScrollChanged only fires when
        // innerPanel.Y changes), so a silent no-op cannot fake a zero-allocation result. Scaled by
        // attempts because MeasureMinimum runs the action across all of them, not just one.
        scrollChangedCount.ShouldBeGreaterThanOrEqualTo(measuredIterations * attempts);

        // Scrolling a ListBox with persistent (non-recycled) item visuals allocates nothing in steady
        // state; this guards against a regression reintroducing per-frame allocations (#1934).
        result.TotalBytes.ShouldBe(0);
    }
}
