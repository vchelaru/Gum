using System;
using System.Diagnostics;
using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using Shouldly;
using Xunit;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

// SPIKE 2: [wave] rides the [Custom] path on raylib; this checks it parses to per-letter effect calls,
// draws, and reports the cost of a 50-letter animated string.
public class TextBuiltInEffectTests : BaseTestClass
{
    [Fact]
    public void Text_Wave_AssignsOneEffectCallPerLetterAndDraws()
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = "[wave amp=50 freq=5]abc[/wave]";
        Text internalText = (Text)textRuntime.RenderableComponent;

        internalText.InlineVariables.Count.ShouldBe(3);
        ((ParameterizedLetterCustomizationCall)internalText.InlineVariables[0].Value).BuiltInEffect!.Amplitude.ShouldBe(50f);
        Should.NotThrow(() => internalText.Render(RenderingLibrary.SystemManagers.Default));
    }

    [Fact]
    public void Render_FiftyCharWave_ReportsCostPerDraw()
    {
        const string letters = "The quick brown fox jumps over the lazy dog. 0123";
        TextRuntime wave = new() { Width = 2000 };
        wave.Text = $"[wave]{letters}[/wave]";
        TextRuntime colored = new() { Width = 2000 };
        colored.Text = $"[Color=Red]{letters}[/Color]";
        Func<double> original = BbCodeTextEffect.TimeSeconds;
        double time = 0;
        BbCodeTextEffect.TimeSeconds = () => time;
        try
        {
            double Measure(TextRuntime runtime)
            {
                Text text = (Text)runtime.RenderableComponent;
                for (int i = 0; i < 50; i++) { time += 0.016; text.Render(RenderingLibrary.SystemManagers.Default); }
                Stopwatch stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < 1000; i++) { time += 0.016; text.Render(RenderingLibrary.SystemManagers.Default); }
                return stopwatch.Elapsed.TotalMilliseconds / 1000;
            }

            double waveCost = Measure(wave);
            double colorCost = Measure(colored);
            System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "spike2-cost.txt"), $"SPIKE-COST raylib wave={waveCost:F4}ms colorRun={colorCost:F4}ms per Render call (outside BeginDrawing)\n");
        }
        finally
        {
            BbCodeTextEffect.TimeSeconds = original;
        }
    }
}
