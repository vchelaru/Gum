using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics.Fonts;
using KernSmith.Gum;
using RenderingLibrary.Content;
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

    // ---- Table of variants x sizes (issue #6388 follow-up) ----

    private enum TextSize { Small1, Small30, Medium200, Medium300, Large2400 }

    private sealed record Variant(
        string Name,
        Action<TextRuntime> Configure,
        bool RenderTarget = false,
        bool ChangesEachFrame = false,
        bool BbCode = false,
        bool NeedsShadowFont = false);

    private static readonly Variant[] Variants =
    {
        new("plain-left", t => { }),
        new("plain-center", t => t.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center),
        new("shadow", t =>
        {
            t.HasDropshadow = true;
            t.DropshadowOffsetX = 2;
            t.DropshadowOffsetY = 2;
            t.DropshadowBlur = 2;
        }, NeedsShadowFont: true),
        new("outline2", t => t.OutlineThickness = 2),
        new("bbcode-color", t => { }, BbCode: true),
        new("fontscale1.5", t => t.FontScale = 1.5f),
        new("free-floating", t => t.TextRenderingPositionMode = TextRenderingPositionMode.FreeFloating),
        new("text-changes-each-frame", t => { }, ChangesEachFrame: true),
        new("rendertarget-steady", t => { }, RenderTarget: true),
        new("rendertarget-changes-each-frame", t => { }, RenderTarget: true, ChangesEachFrame: true),
    };

    private static readonly string FontPath =
        Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf");

    private static string BuildText(int characters, bool bbCode)
    {
        const string words = "lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor ";
        StringBuilder builder = new StringBuilder();
        if (characters <= 10)
        {
            builder.Append("abcdefghij".Substring(0, characters));
        }
        else
        {
            while (builder.Length < characters)
            {
                builder.Append(words);
            }
            builder.Length = characters;
            // No trailing space, so a wrapped last line is not shorter than it looks.
            if (builder[builder.Length - 1] == ' ')
            {
                builder[builder.Length - 1] = 'x';
            }
        }

        if (bbCode)
        {
            // A few colored runs of 4 letters, evenly spread.
            int runs = System.Math.Max(1, System.Math.Min(5, characters / 20));
            for (int run = runs - 1; run >= 0; run--)
            {
                int start = System.Math.Min(builder.Length - 4, characters * run / runs);
                builder.Insert(start + 4, "[/Color]");
                builder.Insert(start, "[Color=Red]");
            }
        }
        return builder.ToString();
    }

    private static (int TextCount, int Characters, int Width) Describe(TextSize size) => size switch
    {
        TextSize.Small1 => (1, 10, 0),
        TextSize.Small30 => (30, 10, 0),
        TextSize.Medium200 => (1, 200, 300),
        TextSize.Medium300 => (1, 300, 300),
        _ => (1, 2400, 500),
    };

    private sealed record CaseResult(
        string Variant, string Size, double MinMicrosPerFrame, double MedianMicrosPerFrame,
        double SpritesPerFrame, double BytesPerFrame, int Lines);

    private static CaseResult RunCase(MinimalGame game, Variant variant, TextSize size,
        int frames, int batches, int warmupFrames, int warmupMilliseconds)
    {
        (int textCount, int characters, int width) = Describe(size);
        string text = BuildText(characters, variant.BbCode);
        // Same length, one letter different, so a "changes each frame" text re-wraps to the same shape.
        string alternate = variant.BbCode
            ? text.Replace("[Color=Red]", "[Color=Blue]")
            : (text[0] == 'l' ? "m" : "l") + text.Substring(1);

        TextRenderingMode originalMode = Text.TextRenderingMode;
        Text.TextRenderingMode = variant.RenderTarget ? TextRenderingMode.RenderTarget : TextRenderingMode.CharacterByCharacter;
        List<TextRuntime> texts = new List<TextRuntime>();
        try
        {
            for (int i = 0; i < textCount; i++)
            {
                TextRuntime t = new TextRuntime();
                t.UseCustomFont = true;
                t.CustomFontFile = FontPath;
                t.FontSize = 16;
                if (width > 0)
                {
                    t.WidthUnits = DimensionUnitType.Absolute;
                    t.Width = width;
                    t.HeightUnits = DimensionUnitType.RelativeToChildren;
                }
                variant.Configure(t);
                t.X = 10;
                t.Y = 10 + i * 20;
                t.Text = text;
                t.AddToRoot();
                texts.Add(t);
            }

            global::Gum.GumService.Default.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0)));

            Text first = (Text)texts[0].RenderableComponent;
            if (variant.NeedsShadowFont)
            {
                first.BitmapFont?.ShadowFont.ShouldNotBeNull("the shadow variant must have a real ShadowFont");
            }

            bool flip = false;
            Action frame = () =>
            {
                if (variant.ChangesEachFrame)
                {
                    flip = !flip;
                    string value = flip ? alternate : text;
                    foreach (TextRuntime t in texts)
                    {
                        t.Text = value;
                    }
                }
                global::Gum.GumService.Default.Draw();
            };

            // Liveness: something must have been drawn.
            long before = game.GraphicsDevice.Metrics.SpriteCount;
            frame();
            (game.GraphicsDevice.Metrics.SpriteCount - before).ShouldBeGreaterThan(0);

            long warmupEnd = Stopwatch.GetTimestamp() + Stopwatch.Frequency * warmupMilliseconds / 1000;
            for (int i = 0; i < warmupFrames || Stopwatch.GetTimestamp() < warmupEnd; i++)
            {
                frame();
            }

            List<double> perFrame = new List<double>();
            long spriteStart = game.GraphicsDevice.Metrics.SpriteCount;
            long bytes = 0;
            for (int batch = 0; batch < batches; batch++)
            {
                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                long startTicks = Stopwatch.GetTimestamp();
                for (int i = 0; i < frames; i++)
                {
                    frame();
                }
                long elapsed = Stopwatch.GetTimestamp() - startTicks;
                bytes += GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                perFrame.Add(elapsed * 1_000_000.0 / Stopwatch.Frequency / frames);
            }
            double sprites = (game.GraphicsDevice.Metrics.SpriteCount - spriteStart) / (double)(frames * batches);
            perFrame.Sort();
            return new CaseResult(variant.Name, size.ToString(), perFrame[0], perFrame[perFrame.Count / 2],
                sprites, bytes / (double)(frames * batches), first.WrappedText.Count);
        }
        finally
        {
            foreach (TextRuntime t in texts)
            {
                t.RemoveFromRoot();
            }
            Text.TextRenderingMode = originalMode;
        }
    }

    private static string FormatRow(CaseResult r) =>
        $"{r.Variant}\t{r.Size}\t{r.Lines}\t{r.MinMicrosPerFrame:F1}\t{r.MedianMicrosPerFrame:F1}\t{r.SpritesPerFrame:F0}\t" +
        $"{r.MinMicrosPerFrame / System.Math.Max(1, r.SpritesPerFrame):F3}\t{r.BytesPerFrame:F0}";

    private static void WithKernSmith(MinimalGame game, Action body)
    {
        IInMemoryFontCreator? saved = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
        try
        {
            body();
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = saved;
        }
    }

    /// <summary>
    /// The variant x size table. By default it runs a handful of frames per case so it stays quick. Set
    /// GUM_TEXT_BENCH_TABLE_OUT to a file to append the rows, and GUM_TEXT_BENCH_FRAMES / _BATCHES / _WARMUP_MS
    /// for real measurements. GUM_TEXT_BENCH_REVERSE=1 runs the cases in reverse order, so two alternating
    /// runs do not always measure the same case first.
    /// </summary>
    [Fact]
    public void VariantSizeTable_EveryCaseDraws()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        int frames = ReadInt("GUM_TEXT_BENCH_FRAMES", 5);
        int batches = ReadInt("GUM_TEXT_BENCH_BATCHES", 2);
        int warmupMs = ReadInt("GUM_TEXT_BENCH_WARMUP_MS", 0);
        string? outPath = Environment.GetEnvironmentVariable("GUM_TEXT_BENCH_TABLE_OUT");
        bool reverse = Environment.GetEnvironmentVariable("GUM_TEXT_BENCH_REVERSE") == "1";
        bool quick = outPath == null;

        List<(Variant, TextSize)> cases = new List<(Variant, TextSize)>();
        foreach (Variant variant in Variants)
        {
            foreach (TextSize size in Enum.GetValues<TextSize>())
            {
                // The default run only proves each variant draws, at the two cheapest sizes.
                if (quick && size != TextSize.Small1 && size != TextSize.Medium200)
                {
                    continue;
                }
                cases.Add((variant, size));
            }
        }
        if (reverse)
        {
            cases.Reverse();
        }

        WithKernSmith(game, () =>
        {
            if (!quick)
            {
                // Discarded: whichever case runs first in a process pays the JIT and font creation.
                RunCase(game, Variants[0], TextSize.Medium200, 20, 1, warmupFrames: 20, warmupMilliseconds: 1000);
            }
            foreach ((Variant variant, TextSize size) in cases)
            {
                CaseResult result = RunCase(game, variant, size, frames, batches, warmupFrames: 20, warmupMs);
                if (outPath != null)
                {
                    File.AppendAllText(outPath, FormatRow(result) + "\n");
                }
            }
        });
    }

    /// <summary>
    /// One case, from a fresh process, so tiered-JIT warmup is controlled: GUM_TEXT_BENCH_COLD is
    /// plain-cold, plain-warm, shadow-cold or shadow-warm. Cold is the issue's method (30 warmup frames, 1000
    /// measured); warm is a long warmup then best of 9 x 200. Run it filtered to this test alone.
    /// </summary>
    [Fact]
    public void ColdVersusWarm_SingleCase()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        string mode = Environment.GetEnvironmentVariable("GUM_TEXT_BENCH_COLD") ?? "plain-cold";
        bool shadow = mode.StartsWith("shadow");
        bool cold = mode.EndsWith("cold");
        bool quick = Environment.GetEnvironmentVariable("GUM_TEXT_BENCH_COLD") == null;
        Variant variant = Variants.Single(v => v.Name == (shadow ? "shadow" : "plain-left"));

        WithKernSmith(game, () =>
        {
            CaseResult result = quick
                ? RunCase(game, variant, TextSize.Medium200, 5, 1, 5, 0)
                : cold
                    ? RunCase(game, variant, TextSize.Medium200, 1000, 1, 29, 0)
                    : RunCase(game, variant, TextSize.Medium200, 200, 9, 100, 1000);
            string? outPath = Environment.GetEnvironmentVariable("GUM_TEXT_BENCH_TABLE_OUT");
            if (outPath != null)
            {
                File.AppendAllText(outPath, mode + "\t" + FormatRow(result) + "\n");
            }
        });
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
