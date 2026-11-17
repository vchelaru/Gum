using System.Runtime;
using MonoGameGum.TestsCommon;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Performance;

public class SkiaAllocationMeasurerTests
{
    [Fact]
    public void Measure_HandleTableAlreadyLarge_ReadsSameAsWhenSmall()
    {
        // Like RichTextKit, each frame creates SKFonts it never disposes, so SkiaSharp's handle table grows until
        // finalizers run (#7860).
        static void Frame(int frameNumber)
        {
            for (int i = 0; i < 8; i++)
            {
                _ = new SKFont();
            }
        }

        AllocationResult first = SkiaAllocationMeasurer.Measure(Frame, warmupIterations: 10, measuredIterations: 300);

        // Leaves the handle table with room for far more handles than the workload creates.
        List<SKFont> held = new List<SKFont>();
        for (int i = 0; i < 40000; i++)
        {
            held.Add(new SKFont());
        }
        foreach (SKFont font in held)
        {
            font.Dispose();
        }

        AllocationResult second = SkiaAllocationMeasurer.Measure(Frame, warmupIterations: 10, measuredIterations: 300);

        first.TotalBytes.ShouldBe(second.TotalBytes);
        first.BytesPerIteration.ShouldBeGreaterThan(0);
    }

    // Warmup 5 + measured 20: call 1 is in the peak pass's no-GC region, call 35 in the measured window's.
    [Theory]
    [InlineData(1)]
    [InlineData(35)]
    public void Measure_FrameThrows_PropagatesAndLeavesNoGcRegionOpen(int throwOnCall)
    {
        int callCount = 0;
        void ThrowingFrame(int frameNumber)
        {
            callCount++;
            if (callCount == throwOnCall)
            {
                throw new InvalidOperationException("frame failed");
            }
        }

        try
        {
            Should.Throw<InvalidOperationException>(() =>
                SkiaAllocationMeasurer.Measure(ThrowingFrame, warmupIterations: 5, measuredIterations: 20))
                .Message.ShouldBe("frame failed");

            GCSettings.LatencyMode.ShouldNotBe(GCLatencyMode.NoGCRegion);

            AllocationResult next = SkiaAllocationMeasurer.Measure(
                frame => { }, warmupIterations: 5, measuredIterations: 20);
            next.Iterations.ShouldBe(20);
        }
        finally
        {
            // Keeps a leaked region from failing every later test in the process.
            if (GCSettings.LatencyMode == GCLatencyMode.NoGCRegion)
            {
                GC.EndNoGCRegion();
            }
        }
    }

    [Fact]
    public void Measure_ReplaysTheSameFrameNumbers_InThePeakPassAndTheWindow()
    {
        const int Warmup = 5;
        const int Measured = 20;
        Dictionary<int, int> calls = new Dictionary<int, int>();

        SkiaAllocationMeasurer.Measure(
            frame => calls[frame] = calls.GetValueOrDefault(frame) + 1,
            warmupIterations: Warmup,
            measuredIterations: Measured);

        // Frames 0..24 run in a pass that grows the handle table and again as warm-up plus window.
        calls.Keys.Min().ShouldBe(0);
        calls.Keys.Max().ShouldBe(Warmup + Measured - 1);
        calls.Values.ShouldAllBe(count => count == 2);
    }
}
