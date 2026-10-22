using System;
using System.Collections.Generic;
using System.Text;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameGum.TestsCommon;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Performance;

/// <summary>
/// Issue #6420: a wrapped <see cref="TextRuntime"/> whose text changes every frame (a counter, a timer, a
/// scrolling log) must not allocate per word or per concatenation, through the full frame of setting the
/// text, layout and drawing. The caller makes its strings, so the test hands over pre-made ones. What
/// remains per frame is the string of every wrapped line whose content changed.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class WrappedTextChangeAllocationTests : BaseTestClass
{
    [Theory]
    [InlineData(200, 300)]
    [InlineData(2400, 500)]
    public void TextChangedEveryFrame_AllocatesOnlyTheChangedLineStrings(int letters, int width)
    {
        string a = MakeWords(letters, seed: 1);
        string b = MakeWords(letters, seed: 2);

        using MinimalGame game = new();
        game.RunOneFrame();

        TextRenderingMode originalMode = Text.TextRenderingMode;
        Text.TextRenderingMode = TextRenderingMode.CharacterByCharacter;
        TextRuntime text = new();
        try
        {
            text.WidthUnits = DimensionUnitType.Absolute;
            text.Width = width;
            text.HeightUnits = DimensionUnitType.RelativeToChildren;
            text.X = 10;
            text.Y = 10;
            text.Text = a;
            text.AddToRoot();

            GameTime gameTime = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));
            global::Gum.GumService.Default.Update(gameTime);

            Text renderable = (Text)text.RenderableComponent!;
            renderable.WrappedText.Count.ShouldBeGreaterThan(5, "because the string must really wrap");
            long lineBytesA = LineStringBytes(renderable.WrappedText);
            text.Text = b;
            long lineBytesB = LineStringBytes(renderable.WrappedText);

            bool flip = false;
            AllocationResult result = AllocationMeasurer.MeasureMinimum(
                () =>
                {
                    flip = !flip;
                    text.Text = flip ? a : b;
                    global::Gum.GumService.Default.Draw();
                },
                attempts: 3,
                warmupIterations: 30,
                measuredIterations: 20);


            // Every line differs between the two strings, so the new line strings are the floor; the
            // headroom covers the layout pass.
            result.BytesPerIteration.ShouldBeLessThanOrEqualTo(
                System.Math.Max(lineBytesA, lineBytesB) + 256,
                $"measured {result.BytesPerIteration:N0} bytes/frame");
        }
        finally
        {
            text.RemoveFromRoot();
            Text.TextRenderingMode = originalMode;
        }
    }

    // The room a line string takes: object header and length (22 bytes), 2 bytes a char, rounded up to 8.
    private static long LineStringBytes(IEnumerable<string> lines)
    {
        long total = 0;
        foreach (string line in lines)
        {
            total += (22 + 2L * line.Length + 7) / 8 * 8;
        }
        return total;
    }

    // Deterministic lowercase words of 2 to 8 letters joined by single spaces, exactly `letters` long.
    private static string MakeWords(int letters, int seed)
    {
        Random random = new(seed);
        StringBuilder builder = new();
        while (builder.Length < letters)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }
            int length = 2 + random.Next(7);
            for (int i = 0; i < length; i++)
            {
                builder.Append((char)('a' + random.Next(26)));
            }
        }
        return builder.ToString(0, letters);
    }

    private sealed class MinimalGame : SharedGameLease
    {
        protected override void OnInitialize() =>
            global::Gum.GumService.Default.Initialize(Game, global::Gum.Forms.DefaultVisualsVersion.Newest);

        protected override void OnDispose()
        {
            if (global::Gum.GumService.Default.IsInitialized)
            {
                global::Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
