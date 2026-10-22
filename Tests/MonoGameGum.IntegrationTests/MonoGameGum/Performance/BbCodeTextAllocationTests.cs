using System;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameGum.TestsCommon;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Performance;

/// <summary>
/// Issue #6419: drawing a BBCode-coloured <see cref="Text"/> whose text, colours and wrap width did not
/// change must allocate nothing per frame, like plain text does. Each guard draws a warmed-up scene a
/// fixed small number of frames and uses <see cref="AllocationMeasurer.MeasureMinimum"/>.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class BbCodeTextAllocationTests : BaseTestClass
{
    private const string ColoredSingleLine = "ab[Color=Red]cdef[/Color]ghij";
    private const string ColoredWrapped =
        "lorem [Color=Red]ipsum dolor[/Color] sit amet consectetur [Color=Blue]adipiscing elit[/Color] " +
        "sed do eiusmod tempor [Color=Green]incididunt ut labore[/Color] et dolore magna aliqua";

    [Fact]
    public void Draw_PlainText_AllocatesNothingPerFrame()
    {
        AllocationResult result = MeasureDraw("abcdefghij", wrapWidth: 0, out int sprites, out int lines);

        sprites.ShouldBeGreaterThan(0);
        lines.ShouldBe(1);
        result.TotalBytes.ShouldBe(0, $"measured {result.BytesPerIteration:N0} bytes/frame");
    }

    [Fact]
    public void Draw_UnchangedBbCodeColoredText_AllocatesNothingPerFrame()
    {
        AllocationResult result = MeasureDraw(ColoredSingleLine, wrapWidth: 0, out int sprites, out int lines);

        sprites.ShouldBeGreaterThan(0);
        lines.ShouldBe(1);
        result.TotalBytes.ShouldBe(0, $"measured {result.BytesPerIteration:N0} bytes/frame");
    }

    [Fact]
    public void Draw_UnchangedBbCodeColoredWrappedText_AllocatesNothingPerFrame()
    {
        AllocationResult result = MeasureDraw(ColoredWrapped, wrapWidth: 150, out int sprites, out int lines);

        sprites.ShouldBeGreaterThan(0);
        lines.ShouldBeGreaterThan(2);
        result.TotalBytes.ShouldBe(0, $"measured {result.BytesPerIteration:N0} bytes/frame");
    }

    private static AllocationResult MeasureDraw(string markup, float wrapWidth, out int spritesPerDraw, out int wrappedLineCount)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        TextRenderingMode originalMode = Text.TextRenderingMode;
        Text.TextRenderingMode = TextRenderingMode.CharacterByCharacter;
        TextRuntime text = new();
        try
        {
            if (wrapWidth > 0)
            {
                text.WidthUnits = DimensionUnitType.Absolute;
                text.Width = wrapWidth;
                text.HeightUnits = DimensionUnitType.RelativeToChildren;
            }
            text.X = 10;
            text.Y = 10;
            text.Text = markup;
            text.AddToRoot();

            global::Gum.GumService.Default.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0)));

            Text renderable = (Text)text.RenderableComponent;
            wrappedLineCount = renderable.WrappedText.Count;

            long spritesBefore = game.GraphicsDevice.Metrics.SpriteCount;
            global::Gum.GumService.Default.Draw();
            spritesPerDraw = (int)(game.GraphicsDevice.Metrics.SpriteCount - spritesBefore);

            // Liveness for the BBCode cases: the colour runs must exist, or a zero result proves nothing.
            if (markup.Contains('['))
            {
                renderable.InlineVariables.Count.ShouldBeGreaterThan(0);
            }

            return AllocationMeasurer.MeasureMinimum(
                () => global::Gum.GumService.Default.Draw(),
                attempts: 3,
                warmupIterations: 30,
                measuredIterations: 20);
        }
        finally
        {
            text.RemoveFromRoot();
            Text.TextRenderingMode = originalMode;
        }
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
