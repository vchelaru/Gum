using System.Diagnostics;
using System.Text;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaSharp;

namespace SkiaGum.GpuTests;

/// <summary>
/// #7826 research, opt-in: compares Gum's per-sprite DrawImage with one SKCanvas.DrawAtlas call, for draw time,
/// managed allocations and output pixels. Timing runs on the GPU (GL) surface only, since Gum's Skia hosts render on
/// the GPU; pixels are compared on both the CPU raster surface and the GPU surface. Does nothing unless
/// GUM_DRAWATLAS_OUT names a file to write the results to, so it never runs as part of CI. Run in Release:
/// <code>$env:GUM_DRAWATLAS_OUT="$pwd/drawatlas.txt"; dotnet test Tests/SkiaGum.GpuTests -c Release --filter DrawAtlasInvestigationTests</code>
/// </summary>
[Collection(GpuCollection.Name)]
public class DrawAtlasInvestigationTests
{
    static readonly string? OutputPath = Environment.GetEnvironmentVariable("GUM_DRAWATLAS_OUT");

    readonly WglGpuContext _gpu;

    public DrawAtlasInvestigationTests(WglGpuContext gpu)
    {
        _gpu = gpu;
    }

    static void Write(string text)
    {
        File.AppendAllText(OutputPath!, text + Environment.NewLine);
    }

    #region Timing

    enum Mode
    {
        // canvas.DrawImage per sprite with one shared paint (untinted Gum sprites).
        DrawImage,
        // canvas.DrawImage per sprite, each with its own cached paint carrying a Modulate color filter (tinted Gum
        // sprites whose tint does not change).
        DrawImageTinted,
        // One DrawAtlas call, no colors.
        Atlas,
        // One DrawAtlas call with a color per sprite (Modulate), arrays reused across frames.
        AtlasColors,
        // As AtlasColors, but the three arrays are allocated every frame.
        AtlasColorsFreshArrays,
        // One DrawAtlas call per sprite with its color (batches of one), one-element arrays reused across frames.
        AtlasOnePerSprite,
    }

    const int SurfaceSize = 1024;
    const int Cell = 16;
    const float SpriteSize = 32;

    sealed class Workload : IDisposable
    {
        public readonly SKRect[] Sources;
        public readonly SKRect[] Destinations;
        public readonly SKRotationScaleMatrix[] Transforms;
        public readonly SKColor[] Colors;
        public readonly SKPaint SharedPaint = new() { IsAntialias = false };
        public readonly SKPaint[] TintedPaints;
        public readonly (SKRect[] Source, SKRotationScaleMatrix[] Transform, SKColor[] Color)[] Singles;

        public Workload(int count)
        {
            Sources = new SKRect[count];
            Destinations = new SKRect[count];
            Transforms = new SKRotationScaleMatrix[count];
            Colors = new SKColor[count];
            TintedPaints = new SKPaint[count];
            Singles = new (SKRect[], SKRotationScaleMatrix[], SKColor[])[count];
            float scale = SpriteSize / Cell;
            for (int i = 0; i < count; i++)
            {
                int cell = i % 256;
                float sx = cell % 16 * Cell;
                float sy = cell / 16 * Cell;
                Sources[i] = new SKRect(sx, sy, sx + Cell, sy + Cell);
                float x = i * 37 % (SurfaceSize - (int)SpriteSize);
                float y = i * 53 % (SurfaceSize - (int)SpriteSize);
                Destinations[i] = new SKRect(x, y, x + SpriteSize, y + SpriteSize);
                Transforms[i] = new SKRotationScaleMatrix(scale, 0, x, y);
                Colors[i] = new SKColor((byte)(i * 7), (byte)(255 - i * 3), (byte)(i * 11), 255);
                using SKColorFilter filter = SKColorFilter.CreateBlendMode(Colors[i], SKBlendMode.Modulate);
                TintedPaints[i] = new SKPaint { IsAntialias = false, ColorFilter = filter };
                Singles[i] = (new[] { Sources[i] }, new[] { Transforms[i] }, new[] { Colors[i] });
            }
        }

        public void Dispose()
        {
            SharedPaint.Dispose();
            foreach (SKPaint paint in TintedPaints)
            {
                paint.Dispose();
            }
        }
    }

    static SKImage CreateAtlasImage()
    {
        using var bitmap = new SKBitmap(256, 256);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                bitmap.SetPixel(x, y, new SKColor((byte)x, (byte)y, (byte)(x ^ y), (byte)(x % 16 == 0 ? 0 : 255)));
            }
        }
        return SKImage.FromBitmap(bitmap);
    }

    static readonly SKSamplingOptions Nearest = new(SKFilterMode.Nearest, SKMipmapMode.None);

    static void DrawFrame(SKCanvas canvas, SKImage image, Workload work, Mode mode)
    {
        canvas.Clear(SKColors.Black);
        int count = work.Sources.Length;
        switch (mode)
        {
            case Mode.DrawImage:
                for (int i = 0; i < count; i++)
                {
                    canvas.DrawImage(image, work.Sources[i], work.Destinations[i], Nearest, work.SharedPaint);
                }
                break;
            case Mode.DrawImageTinted:
                for (int i = 0; i < count; i++)
                {
                    canvas.DrawImage(image, work.Sources[i], work.Destinations[i], Nearest, work.TintedPaints[i]);
                }
                break;
            case Mode.Atlas:
                canvas.DrawAtlas(image, work.Sources, work.Transforms, Nearest, work.SharedPaint);
                break;
            case Mode.AtlasColors:
                canvas.DrawAtlas(image, work.Sources, work.Transforms, work.Colors, SKBlendMode.Modulate, Nearest, work.SharedPaint);
                break;
            case Mode.AtlasColorsFreshArrays:
                var sources = new SKRect[count];
                var transforms = new SKRotationScaleMatrix[count];
                var colors = new SKColor[count];
                Array.Copy(work.Sources, sources, count);
                Array.Copy(work.Transforms, transforms, count);
                Array.Copy(work.Colors, colors, count);
                canvas.DrawAtlas(image, sources, transforms, colors, SKBlendMode.Modulate, Nearest, work.SharedPaint);
                break;
            case Mode.AtlasOnePerSprite:
                foreach ((SKRect[] source, SKRotationScaleMatrix[] transform, SKColor[] color) in work.Singles)
                {
                    canvas.DrawAtlas(image, source, transform, color, SKBlendMode.Modulate, Nearest, work.SharedPaint);
                }
                break;
        }
    }

    // Microseconds per frame and managed bytes per frame. finish waits for the work to complete (GPU flush + sync).
    static (double Microseconds, double Bytes) Measure(SKCanvas canvas, SKImage image, Workload work, Mode mode, Action finish)
    {
        for (int i = 0; i < 20; i++)
        {
            DrawFrame(canvas, image, work, mode);
            finish();
        }
        int frames = 0;
        long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < 300 && frames < 2000)
        {
            DrawFrame(canvas, image, work, mode);
            finish();
            frames++;
        }
        watch.Stop();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
        return (watch.Elapsed.TotalMilliseconds * 1000 / frames, (double)bytes / frames);
    }

    static readonly int[] Counts = { 1, 10, 100, 1000 };

    void RunTiming(string backend, SKCanvas canvas, SKImage image, Action finish)
    {
        foreach (int count in Counts)
        {
            using var work = new Workload(count);
            var line = new StringBuilder($"{backend,-4} n={count,-5}");
            foreach (Mode mode in Enum.GetValues<Mode>())
            {
                // The fastest of three runs, so a scheduling hiccup in one does not decide the row.
                (double us, double bytes) best = (double.MaxValue, 0);
                for (int run = 0; run < 3; run++)
                {
                    (double us, double bytes) measured = Measure(canvas, image, work, mode, finish);
                    if (measured.us < best.us)
                    {
                        best = measured;
                    }
                }
                line.Append($" | {mode} {best.us,8:F1}us {best.bytes,6:F0}B");
            }
            Write(line.ToString());
        }
    }

    [Fact]
    public void Timing_DrawImageVersusDrawAtlas()
    {
        if (OutputPath == null)
        {
            return;
        }
        Write($"# GPU timing (us per frame incl. clear, flush and synchronous submit), {SurfaceSize}px surface, {SpriteSize}px sprites from a 256px atlas, nearest. GL_RENDERER={_gpu.Renderer}");
        using SKImage image = CreateAtlasImage();
        _gpu.WithSurface(SurfaceSize, SurfaceSize, (surface, context) =>
        {
            using SKImage texture = image.ToTextureImage(context);
            RunTiming("GPU", surface.Canvas, texture, () => context.Flush(true, true));
        });
    }

    #endregion

    #region Pixels

    record Case(
        string Name,
        float X,
        float Y,
        float Width,
        float Height,
        float Rotation = 0,
        bool FlipHorizontal = false,
        SKFilterMode Filter = SKFilterMode.Nearest,
        SKColor? Tint = null,
        int SourceLeft = 4,
        int SourceTop = 4,
        int SourceSize = 16);

    static readonly Case[] Cases =
    {
        new("scale3 nearest", 10, 10, 48, 48),
        new("scale3 linear", 10, 10, 48, 48, Filter: SKFilterMode.Linear),
        new("half-pixel scale1 nearest (#7030)", 10.5f, 10.5f, 16, 16),
        new("fractional scale2.3 nearest", 10.25f, 10.75f, 36.8f, 36.8f),
        new("rotate30 nearest", 40, 40, 48, 48, Rotation: 30),
        new("rotate30 linear", 40, 40, 48, 48, Rotation: 30, Filter: SKFilterMode.Linear),
        new("flipH nearest", 10, 10, 48, 48, FlipHorizontal: true),
        new("non-uniform 48x24 nearest", 10, 10, 48, 24),
        new("full-image source scale2 nearest", 10, 10, 64, 64, SourceLeft: 0, SourceTop: 0, SourceSize: 32),
        new("tint opaque", 10, 10, 48, 48, Tint: new SKColor(255, 128, 64, 255)),
        new("tint alpha128", 10, 10, 48, 48, Tint: new SKColor(255, 128, 64, 128)),
        new("tint alpha128 linear", 10, 10, 48, 48, Filter: SKFilterMode.Linear, Tint: new SKColor(255, 128, 64, 128)),
    };

    static SKBitmap CreateTexture()
    {
        var bitmap = new SKBitmap(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                // Distinct neighbors so a one-texel sampling shift shows; some partial alpha.
                bitmap.SetPixel(x, y, new SKColor((byte)(x * 8), (byte)(y * 8), (byte)((x + y) % 2 * 255), (byte)(x < 8 ? 160 : 255)));
            }
        }
        return bitmap;
    }

    const int PixelSize = 120;

    static void DrawGum(SKCanvas canvas, Case c, SKBitmap texture)
    {
        GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        var managers = new SystemManagers();
        managers.Initialize();
        SystemManagers.Default = managers;
        managers.Canvas = canvas;
        managers.Renderer.ClearsCanvas = false;
        RenderingLibrary.Graphics.Renderer.TextureFilter = c.Filter;
        SKColor tint = c.Tint ?? SKColors.White;
        var sprite = new SpriteRuntime
        {
            Texture = texture,
            TextureAddress = TextureAddress.Custom,
            // Skia Sprite flips only through swapped source edges (Sprite.cs DrawBoundAtOrigin); it ignores FlipHorizontal.
            TextureLeft = c.FlipHorizontal ? c.SourceLeft + c.SourceSize : c.SourceLeft,
            TextureTop = c.SourceTop,
            TextureWidth = c.FlipHorizontal ? -c.SourceSize : c.SourceSize,
            TextureHeight = c.SourceSize,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            X = c.X,
            Y = c.Y,
            Width = c.Width,
            Height = c.Height,
            Rotation = c.Rotation,
            Red = tint.Red,
            Green = tint.Green,
            Blue = tint.Blue,
            Alpha = tint.Alpha,
        };
        sprite.AddToManagers(managers, null);
        canvas.Clear(SKColors.Black);
        managers.Draw();
    }

    // The same sprite with one DrawAtlas call. A uniform, unflipped sprite is a pure RSXform (batchable). A flip
    // or a non-uniform scale cannot be expressed by an RSXform (rotation + uniform scale + translation only), so
    // it needs its own canvas matrix, which means its own DrawAtlas call.
    static void DrawAtlasSprite(SKCanvas canvas, Case c, SKImage image, SKBlendMode? colorMode, bool inset, bool paintCarriesTint, bool squareAlpha = false)
    {
        canvas.Clear(SKColors.Black);
        var source = new SKRect(c.SourceLeft, c.SourceTop, c.SourceLeft + c.SourceSize, c.SourceTop + c.SourceSize);
        float scaleX = c.Width / c.SourceSize;
        float scaleY = c.Height / c.SourceSize;
        if (inset && c.Filter == SKFilterMode.Nearest)
        {
            // Sprite.GetPointSamplingInset with an identity canvas matrix.
            float insetX = MathF.Min(1f / 64f, MathF.Min(0.25f / scaleX, c.SourceSize / 4f));
            float insetY = MathF.Min(1f / 64f, MathF.Min(0.25f / scaleY, c.SourceSize / 4f));
            source.Inflate(-insetX, -insetY);
        }
        // DrawImage maps the (inset) source rectangle onto the whole destination, so the scale is taken after the
        // inset; an RSXform places the source rectangle's own top left at its translation.
        scaleX = c.Width / source.Width;
        scaleY = c.Height / source.Height;
        SKColor tint = c.Tint ?? SKColors.White;
        using var paint = new SKPaint { IsAntialias = false, Color = paintCarriesTint ? tint : SKColors.White };
        var sampling = new SKSamplingOptions(c.Filter, SKMipmapMode.None);
        bool needsMatrix = c.FlipHorizontal || MathF.Abs(scaleX - scaleY) > 1e-4f;
        SKRotationScaleMatrix transform;
        canvas.Save();
        if (needsMatrix)
        {
            canvas.Translate(c.X, c.Y);
            canvas.RotateDegrees(-c.Rotation);
            if (c.FlipHorizontal)
            {
                canvas.Scale(-1, 1, c.Width / 2, c.Height / 2);
            }
            canvas.Scale(scaleX, scaleY);
            transform = new SKRotationScaleMatrix(1, 0, 0, 0);
        }
        else
        {
            float radians = -c.Rotation * MathF.PI / 180f;
            transform = new SKRotationScaleMatrix(scaleX * MathF.Cos(radians), scaleX * MathF.Sin(radians), c.X, c.Y);
        }
        if (colorMode is SKBlendMode mode)
        {
            canvas.DrawAtlas(image, new[] { source }, new[] { transform }, new[] { squareAlpha ? tint.WithAlpha((byte)Math.Round(tint.Alpha * tint.Alpha / 255f)) : tint }, mode, sampling, paint);
        }
        else
        {
            canvas.DrawAtlas(image, new[] { source }, new[] { transform }, sampling, paint);
        }
        canvas.Restore();
    }

    static (int Max, int Count) Diff(SKBitmap a, SKBitmap b)
    {
        int max = 0;
        int count = 0;
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                SKColor p = a.GetPixel(x, y);
                SKColor q = b.GetPixel(x, y);
                int d = Math.Max(Math.Max(Math.Abs(p.Red - q.Red), Math.Abs(p.Green - q.Green)),
                    Math.Max(Math.Abs(p.Blue - q.Blue), Math.Abs(p.Alpha - q.Alpha)));
                if (d > 0)
                {
                    count++;
                }
                max = Math.Max(max, d);
            }
        }
        return (max, count);
    }

    // Writes the image beside the output file, for looking at a mismatch.
    static void SavePng(SKBitmap bitmap, bool gpu, Case c, string kind)
    {
        string name = $"{(gpu ? "gpu" : "cpu")}-{string.Concat(c.Name.Where(char.IsLetterOrDigit))}-{kind}.png";
        string path = Path.Combine(Path.GetDirectoryName(OutputPath)!, "drawatlas-png", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
    }

    static SKBitmap RenderCpu(Action<SKCanvas> draw)
    {
        var info = new SKImageInfo(PixelSize, PixelSize);
        using SKSurface surface = SKSurface.Create(info);
        draw(surface.Canvas);
        var pixels = new SKBitmap(info);
        surface.ReadPixels(info, pixels.GetPixels(), info.RowBytes, 0, 0);
        return pixels;
    }

    SKBitmap Render(bool gpu, Action<SKCanvas> draw) => gpu ? _gpu.Render(PixelSize, PixelSize, 0, draw) : RenderCpu(draw);

    static readonly SKBlendMode[] TintModes =
    {
        SKBlendMode.Modulate, SKBlendMode.Multiply, SKBlendMode.SrcIn, SKBlendMode.DstIn, SKBlendMode.SrcATop, SKBlendMode.DstATop,
    };

    [Fact]
    public void Pixels_DrawAtlasVersusGumSprite()
    {
        if (OutputPath == null)
        {
            return;
        }
        SKFilterMode previousFilter = RenderingLibrary.Graphics.Renderer.TextureFilter;
        Write("# Pixels: max channel difference / differing pixel count, Gum SpriteRuntime vs one DrawAtlas call");
        using SKBitmap texture = CreateTexture();
        using SKImage image = SKImage.FromBitmap(texture);
        try
        {
            foreach (bool gpu in new[] { false, true })
            {
                foreach (Case c in Cases)
                {
                    using SKBitmap reference = Render(gpu, canvas => DrawGum(canvas, c, texture));
                    var line = new StringBuilder($"{(gpu ? "GPU" : "CPU"),-4} {c.Name,-36}");
                    if (c.Tint == null)
                    {
                        using SKBitmap withInset = Render(gpu, canvas => DrawAtlasSprite(canvas, c, image, null, inset: true, paintCarriesTint: false));
                        using SKBitmap noInset = Render(gpu, canvas => DrawAtlasSprite(canvas, c, image, null, inset: false, paintCarriesTint: false));
                        SavePng(reference, gpu, c, "gum");
                        SavePng(withInset, gpu, c, "atlas");
                        (int m1, int n1) = Diff(reference, withInset);
                        (int m2, int n2) = Diff(reference, noInset);
                        line.Append($" | inset {m1}/{n1} | no inset {m2}/{n2}");
                    }
                    else
                    {
                        foreach (bool paintTint in new[] { true, false })
                        {
                            foreach (SKBlendMode mode in TintModes)
                            {
                                using SKBitmap atlas = Render(gpu, canvas => DrawAtlasSprite(canvas, c, image, mode, inset: true, paintCarriesTint: paintTint));
                                (int m, int n) = Diff(reference, atlas);
                                line.Append($" | {mode}{(paintTint ? "+paintTint" : "")} {m}/{n}");
                            }
                        }
                        // Gum's tint alpha reaches the image twice (the Modulate filter and the paint alpha). Folding
                        // both into the per-sprite color (alpha squared) keeps the paint shared, so a batch can mix alphas.
                        using SKBitmap folded = Render(gpu, canvas => DrawAtlasSprite(canvas, c, image, SKBlendMode.Modulate, inset: true, paintCarriesTint: false, squareAlpha: true));
                        (int fm, int fn) = Diff(reference, folded);
                        line.Append($" | Modulate alpha-squared color, white paint {fm}/{fn}");
                    }
                    Write(line.ToString());
                }
            }
        }
        finally
        {
            RenderingLibrary.Graphics.Renderer.TextureFilter = previousFilter;
        }
    }

    #endregion
}
