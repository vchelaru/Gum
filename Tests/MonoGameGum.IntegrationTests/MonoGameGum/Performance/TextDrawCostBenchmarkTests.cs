using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Performance;

/// <summary>
/// Stopwatch harness for the CPU cost of drawing text glyph by glyph (issue #6388): it times
/// <c>GumService.Draw()</c> over a scene of unchanging <c>CharacterByCharacter</c> texts, and a raw
/// <c>SpriteBatch.Draw</c> baseline of the same number of sprites on the same texture. The assertions only
/// prove the scene drew; the numbers go to <c>GUM_TEXT_BENCH_OUT</c> (a file) when it is set, for
/// before/after comparison. <c>GUM_TEXT_BENCH_FRAMES</c> and <c>GUM_TEXT_BENCH_BATCHES</c> raise the
/// measured work above the small defaults CI runs.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class TextDrawCostBenchmarkTests : BaseTestClass
{
    [Fact]
    public void CharacterByCharacterDraw_VersusRawSpriteBatch_MeasuresPerGlyphCost()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        TextRenderingMode originalMode = Text.TextRenderingMode;
        Text.TextRenderingMode = TextRenderingMode.CharacterByCharacter;
        Camera camera = SystemManagers.Default.Renderer.Camera;
        float originalZoom = camera.Zoom;
        try
        {
            int frames = ReadInt("GUM_TEXT_BENCH_FRAMES", 20);
            int batches = ReadInt("GUM_TEXT_BENCH_BATCHES", 3);
            StringBuilder report = new StringBuilder();
            report.AppendLine($"frames/batch={frames} batches={batches}");

            // 12 texts x 30 chars (the shape of a HUD), the same at zoom 3, and 3 long texts (per-text vs per-glyph cost).
            report.AppendLine(RunScene(game, "12x30 zoom1", 12, 30, 1, frames, batches));
            report.AppendLine(RunScene(game, "12x30 zoom3", 12, 30, 3, frames, batches));
            report.AppendLine(RunScene(game, "3x120 zoom1", 3, 120, 1, frames, batches));
            report.AppendLine(RunRaw(game, 360, frames, batches));

            string? outPath = Environment.GetEnvironmentVariable("GUM_TEXT_BENCH_OUT");
            if (!string.IsNullOrEmpty(outPath))
            {
                File.AppendAllText(outPath, report.ToString());
            }
        }
        finally
        {
            camera.Zoom = originalZoom;
            Text.TextRenderingMode = originalMode;
        }
    }

    private static string RunScene(MinimalGame game, string label, int textCount, int charsPerText, float zoom,
        int frames, int batches)
    {
        string line = new string('x', charsPerText).Insert(charsPerText / 2, " ").Remove(charsPerText);
        List<TextRuntime> texts = new List<TextRuntime>();
        for (int i = 0; i < textCount; i++)
        {
            TextRuntime text = new TextRuntime();
            text.X = 10;
            text.Y = 10 + i * 24;
            text.Text = line;
            text.AddToRoot();
            texts.Add(text);
        }
        SystemManagers.Default.Renderer.Camera.Zoom = zoom;
        // A space is drawn as a zero-size sprite too, so every character is a draw.
        int glyphCount = textCount * charsPerText;

        try
        {
            global::Gum.GumService.Default.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0)));

            // Liveness: one draw must submit a sprite for every character, otherwise the numbers mean nothing.
            long spritesBefore = game.GraphicsDevice.Metrics.SpriteCount;
            global::Gum.GumService.Default.Draw();
            long spriteCount = game.GraphicsDevice.Metrics.SpriteCount - spritesBefore;
            spriteCount.ShouldBe(glyphCount);

            BatchResult gum = Measure(() => global::Gum.GumService.Default.Draw(), frames, batches);
            return $"{label}: {glyphCount} draws/frame, GumService.Draw min {gum.MinMillisecondsPerFrame:F4} ms = " +
                $"{gum.MinMillisecondsPerFrame * 1000 / glyphCount:F3} us/draw, median {gum.MedianMillisecondsPerFrame:F4} ms, " +
                $"{gum.BytesPerFrame:N0} B/frame";
        }
        finally
        {
            foreach (TextRuntime text in texts)
            {
                text.RemoveFromRoot();
            }
        }
    }

    private static string RunRaw(MinimalGame game, int spriteCount, int frames, int batches)
    {
        Texture2D fontTexture = Text.DefaultBitmapFont!.Texture!;
        SpriteBatch spriteBatch = new SpriteBatch(game.GraphicsDevice);
        Rectangle source = new Rectangle(0, 0, 7, 10);
        BatchResult raw = Measure(() =>
        {
            spriteBatch.Begin();
            for (int i = 0; i < spriteCount; i++)
            {
                spriteBatch.Draw(fontTexture, new Vector2(i % 100, i / 100), source, Color.White, 0f,
                    Vector2.Zero, new Vector2(3, 3), SpriteEffects.None, 0f);
            }
            spriteBatch.End();
        }, frames, batches);
        spriteBatch.Dispose();
        return $"raw SpriteBatch: {spriteCount} sprites/frame, min {raw.MinMillisecondsPerFrame:F4} ms = " +
            $"{raw.MinMillisecondsPerFrame * 1000 / spriteCount:F3} us/sprite, median {raw.MedianMillisecondsPerFrame:F4} ms";
    }

    private static int ReadInt(string name, int defaultValue)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out int parsed) ? parsed : defaultValue;
    }

    private readonly record struct BatchResult(
        double MinMillisecondsPerFrame, double MedianMillisecondsPerFrame, long BytesPerFrame);

    private static BatchResult Measure(Action draw, int framesPerBatch, int batches)
    {
        // Settle JIT and tiered compilation before timing. Tier-1 promotion starts only after a quiet 100 ms
        // and is compiled on a background thread, so a frame count is not enough; run for a fixed time too.
        long warmupEnd = Stopwatch.GetTimestamp() + Stopwatch.Frequency * ReadInt("GUM_TEXT_BENCH_WARMUP_MS", 100) / 1000;
        int warmupFrames = ReadInt("GUM_TEXT_BENCH_WARMUP_FRAMES", 100);
        for (int i = 0; i < warmupFrames || Stopwatch.GetTimestamp() < warmupEnd; i++)
        {
            draw();
        }

        List<double> perFrame = new List<double>();
        long bytes = 0;
        for (int batch = 0; batch < batches; batch++)
        {
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long startTicks = Stopwatch.GetTimestamp();
            for (int i = 0; i < framesPerBatch; i++)
            {
                draw();
            }
            long elapsedTicks = Stopwatch.GetTimestamp() - startTicks;
            bytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            perFrame.Add(elapsedTicks * 1000.0 / Stopwatch.Frequency / framesPerBatch);
        }

        perFrame.Sort();
        return new BatchResult(perFrame[0], perFrame[perFrame.Count / 2], bytes / framesPerBatch);
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
