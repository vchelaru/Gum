using System;
using System.Diagnostics;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;

namespace SkiaGum.Tests.Renderables;

// SPIKE 2: [wave] animates on Skia now that [Custom] re-runs every draw (#8317).
public class TextBuiltInEffectTests : BaseTestClass
{
    readonly Func<double> _originalClock = BbCodeTextEffect.TimeSeconds;
    readonly ITestOutputHelper _output;

    public TextBuiltInEffectTests(ITestOutputHelper output) => _output = output;

    public override void Dispose()
    {
        BbCodeTextEffect.TimeSeconds = _originalClock;
        base.Dispose();
    }

    private static Text MakeText(string rawText)
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 40;
        text.Width = 1000;
        text.Color = SKColors.Black;
        text.X = 20;
        text.Y = 40;
        text.RawText = rawText;
        return text;
    }

    private static int InkTop(SKSurface surface, Text text)
    {
        surface.Canvas.Clear(SKColors.Transparent);
        SystemManagers managers = new() { Canvas = surface.Canvas };
        text.Render(managers);
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 128)
                {
                    return y;
                }
            }
        }
        return -1;
    }

    [Fact]
    public void Render_WaveOverTime_MovesTheLetter()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 200));
        Text text = MakeText("[wave amp=200 freq=1]I[/wave]");

        // Godot: sin(freq * t + x / 50) * amp / 10, and the letter is at x = 0.
        BbCodeTextEffect.TimeSeconds = () => 0;
        int atZero = InkTop(surface, text);
        BbCodeTextEffect.TimeSeconds = () => Math.PI / 2;
        int atQuarter = InkTop(surface, text);

        ((float)(atQuarter - atZero)).ShouldBe(20f, tolerance: 1);
    }

    [Fact]
    public void Render_FiftyCharWave_ReportsCostPerDraw()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(1200, 200));
        Text text = MakeText("[wave]The quick brown fox jumps over the lazy dog. 0123[/wave]");
        Text plain = MakeText("The quick brown fox jumps over the lazy dog. 0123");
        SystemManagers managers = new() { Canvas = surface.Canvas };
        double time = 0;
        BbCodeTextEffect.TimeSeconds = () => time;

        double Measure(Text target)
        {
            for (int i = 0; i < 50; i++) { time += 0.016; target.Render(managers); }
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 500; i++) { time += 0.016; target.Render(managers); }
            return stopwatch.Elapsed.TotalMilliseconds / 500;
        }

        double wave = Measure(text);
        double baseline = Measure(plain);
        _output.WriteLine($"SPIKE-COST skia wave={wave:F3}ms plain={baseline:F3}ms per draw");
        Console.WriteLine($"SPIKE-COST skia wave={wave:F3}ms plain={baseline:F3}ms per draw");
    }
}
