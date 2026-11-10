using MonoGameGum.TestsCommon;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Performance;

public class SkiaAllocationMeasurerTests
{
    [Fact]
    public void MeasureMinimum_HandleTableAlreadyLarge_ReadsSameAsWhenSmall()
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

        AllocationResult first = SkiaAllocationMeasurer.MeasureMinimum(Frame, attempts: 1, warmupIterations: 10, measuredIterations: 300);

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

        AllocationResult second = SkiaAllocationMeasurer.MeasureMinimum(Frame, attempts: 1, warmupIterations: 10, measuredIterations: 300);

        first.TotalBytes.ShouldBe(second.TotalBytes);
        first.BytesPerIteration.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void MeasureMinimum_EveryAttempt_ReplaysTheSameFrameNumbers()
    {
        const int Attempts = 3;
        const int Warmup = 5;
        const int Measured = 20;
        Dictionary<int, int> calls = new Dictionary<int, int>();

        SkiaAllocationMeasurer.MeasureMinimum(
            frame => calls[frame] = calls.GetValueOrDefault(frame) + 1,
            attempts: Attempts,
            warmupIterations: Warmup,
            measuredIterations: Measured);

        // Each attempt runs frames 0..24 in a pass that grows the handle table and again as warm-up plus window.
        calls.Keys.Min().ShouldBe(0);
        calls.Keys.Max().ShouldBe(Warmup + Measured - 1);
        calls.Values.ShouldAllBe(count => count == 2 * Attempts);
    }
}
