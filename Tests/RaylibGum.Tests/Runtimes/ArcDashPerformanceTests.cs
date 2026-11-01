using Gum.GueDeriving;
using Shouldly;
using System.Diagnostics;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// #7383 - a dashed Arc costs a bounded amount per frame: past a dash count the stroke is drawn solid (as Skia
/// does past its own limit), and drawing dashes does not allocate.
/// </summary>
public class ArcDashPerformanceTests : BaseTestClass
{
    private static ArcRuntime AddArc(float width, float height, float dash, float gap)
    {
        ArcRuntime arc = new() { StartAngle = 30, SweepAngle = 300, Thickness = 10, IsEndRounded = true };
        arc.X = 100;
        arc.Y = 100;
        arc.Width = width;
        arc.Height = height;
        arc.Rotation = 20;
        arc.StrokeDashLength = dash;
        arc.StrokeGapLength = gap;
        Gum.GumService.Default.Root.Children.Add(arc);
        Gum.GumService.Default.Root.UpdateLayout();
        return arc;
    }

    private static void DrawFrame()
    {
        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();
    }

    [Theory]
    [InlineData(200f, 100f)]
    [InlineData(200f, 200f)]
    public void TinyDashes_DrawAFrameQuickly(float width, float height)
    {
        AddArc(width, height, dash: 0.0001f, gap: 0.0001f);
        DrawFrame();

        Stopwatch stopwatch = Stopwatch.StartNew();
        DrawFrame();
        stopwatch.Stop();

        stopwatch.Elapsed.TotalMilliseconds.ShouldBeLessThan(100);
        Gum.GumService.Default.Root.Children.Clear();
    }

    [Fact]
    public void DashedArc_AllocatesNoMoreThanASolidOne()
    {
        long Allocated(float dash, float gap)
        {
            AddArc(4000, 2000, dash, gap);
            DrawFrame();
            DrawFrame();
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            DrawFrame();
            long bytes = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Gum.GumService.Default.Root.Children.Clear();
            return bytes;
        }

        long solid = Allocated(0, 0);
        long dashed = Allocated(12, 8);

        (dashed - solid).ShouldBeLessThanOrEqualTo(1024);
    }
}
