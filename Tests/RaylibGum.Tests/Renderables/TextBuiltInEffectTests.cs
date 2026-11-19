using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using System.Collections.Generic;
using Shouldly;
using Xunit;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

// raylib draws built-in effects through the shared BbCodeTextEffect math, from one effect call per letter.
public class TextBuiltInEffectTests : BaseTestClass
{
    [Theory]
    [InlineData("[Wave amp=50 freq=5]abc[/Wave]", "Wave")]
    [InlineData("[Shake]abc[/Shake]", "Shake")]
    [InlineData("[Tornado]abc[/Tornado]", "Tornado")]
    [InlineData("[Fade]abc[/Fade]", "Fade")]
    [InlineData("[Rainbow]abc[/Rainbow]", "Rainbow")]
    [InlineData("[Pulse]abc[/Pulse]", "Pulse")]
    public void Text_EffectTag_AssignsOneEffectCallPerLetterAndDraws(string markup, string kind)
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = markup;
        Text internalText = (Text)textRuntime.RenderableComponent!;

        internalText.InlineVariables.Count.ShouldBe(3);
        for (int i = 0; i < 3; i++)
        {
            var call = (ParameterizedLetterCustomizationCall)internalText.InlineVariables[i].Value!;
            call.BuiltInEffect!.Kind.ToString().ShouldBe(kind);
            call.CharacterIndex.ShouldBe(i);
        }
        Should.NotThrow(() => internalText.Render(RenderingLibrary.SystemManagers.Default));
    }

    // The same table runs on MonoGame and Skia (BbCodeEffectParityCases), so every runtime draws the same offsets
    // and colors. raylib reports each run's offset and color through a test hook, exactly.
    [Theory]
    [MemberData(nameof(BbCodeEffectParityCases.All), MemberType = typeof(BbCodeEffectParityCases))]
    public void Render_Effect_MatchesTheParityTable(string open, string close, double time, float dx, float dy,
        int red, int green, int blue, int alpha)
    {
        System.Func<double> originalClock = BbCodeTextEffect.TimeSeconds;
        List<(string Text, float X, float Y, Raylib_cs.Color Color)> runs = new();
        Text.BuiltInEffectRunObserver = (text, x, y, color) => runs.Add((text, x, y, color));
        try
        {
            TextRuntime textRuntime = new() { Width = 500 };
            textRuntime.Red = 255;
            textRuntime.Green = 255;
            textRuntime.Blue = 255;
            textRuntime.Text = $"{open}A{close}";
            BbCodeTextEffect.TimeSeconds = () => time;

            ((Text)textRuntime.RenderableComponent!).Render(RenderingLibrary.SystemManagers.Default);

            runs.Count.ShouldBeGreaterThan(0);
            runs[0].Text.ShouldBe("A");
            runs[0].X.ShouldBe(dx, 0.01f);
            runs[0].Y.ShouldBe(dy, 0.01f);
            ((int)runs[0].Color.R).ShouldBe(red);
            ((int)runs[0].Color.G).ShouldBe(green);
            ((int)runs[0].Color.B).ShouldBe(blue);
            ((int)runs[0].Color.A).ShouldBe(alpha);
        }
        finally
        {
            Text.BuiltInEffectRunObserver = null;
            BbCodeTextEffect.TimeSeconds = originalClock;
        }
    }
}
