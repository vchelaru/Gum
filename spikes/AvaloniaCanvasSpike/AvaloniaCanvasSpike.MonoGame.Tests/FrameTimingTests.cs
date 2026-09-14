using Xunit;
using Xunit.Abstractions;

namespace AvaloniaCanvasSpike.Tests;

/// <summary>
/// Not a pass/fail gate: renders a fixed number of frames with and without presenting the
/// backend's hidden window and reports the draw / readback / present split, so the backends
/// can be compared from test output on any machine.
/// </summary>
public class FrameTimingTests
{
    private const int WarmupFrames = 10;
    private const int MeasuredFrames = 60;

    private readonly ITestOutputHelper _output;

    public FrameTimingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(1024, 720)]
    [InlineData(3840, 2160)]
    public void ReportFrameTimings(int width, int height)
    {
        SpikeOptions options = SpikeOptions.Parse("test", Array.Empty<string>());
        using GumCanvasGame renderer = new GumCanvasGame(options.ProjectPath, options.ElementName);
        renderer.Resize(width, height);

        foreach (bool skipPresent in new[] { false, true })
        {
            renderer.SkipPresent = skipPresent;
            for (int i = 0; i < WarmupFrames; i++)
            {
                renderer.RenderFrame();
            }

            double totalFrame = 0;
            double totalDraw = 0;
            double totalReadback = 0;
            double totalPresent = 0;
            for (int i = 0; i < MeasuredFrames; i++)
            {
                renderer.RenderFrame();
                totalFrame += renderer.LastFrameMilliseconds;
                totalDraw += renderer.LastDrawMilliseconds;
                totalReadback += renderer.LastReadbackMilliseconds;
                totalPresent += renderer.LastPresentMilliseconds;
            }

            _output.WriteLine(
                $"{width}x{height} present={(skipPresent ? "skipped" : "on")}: " +
                $"frame {totalFrame / MeasuredFrames:0.00} ms = " +
                $"draw {totalDraw / MeasuredFrames:0.00} + " +
                $"readback {totalReadback / MeasuredFrames:0.00} + " +
                $"present {totalPresent / MeasuredFrames:0.00}");
        }

        Assert.True(renderer.LastFrameMilliseconds >= 0);
    }
}
