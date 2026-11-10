using System.Diagnostics;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaSharp;

namespace SkiaGum.GpuTests;

/// <summary>
/// Opt-in GPU check for #7826: renders Sprite scenes on the GL surface and, when GUM_SPRITE_GPU_DIR names a folder,
/// writes each render there the first time and compares against it afterwards, so a draw-path change can be shown
/// to keep GPU pixels identical. It also times 1/10/100/1000 same-texture sprites through Gum. Results go to
/// <c>results.txt</c> in that folder. Does nothing without the variable, so CI never runs it.
/// </summary>
[Collection(GpuCollection.Name)]
public class SpriteGpuSnapshotTests
{
    static readonly string? Folder = Environment.GetEnvironmentVariable("GUM_SPRITE_GPU_DIR");

    readonly WglGpuContext _gpu;

    public SpriteGpuSnapshotTests(WglGpuContext gpu)
    {
        _gpu = gpu;
    }

    static SKBitmap CreateTexture()
    {
        SKBitmap bitmap = new(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                bitmap.SetPixel(x, y, new SKColor((byte)(x * 8), (byte)(y * 8), (byte)((x + y) % 2 * 255), (byte)(x < 8 ? 160 : 255)));
            }
        }
        return bitmap;
    }

    static SpriteRuntime Sprite(SKBitmap texture, float x, float y, float w, float h, int left = 4, int top = 4, int size = 16) => new()
    {
        Texture = texture,
        TextureAddress = TextureAddress.Custom,
        TextureLeft = left,
        TextureTop = top,
        TextureWidth = size,
        TextureHeight = size,
        WidthUnits = DimensionUnitType.Absolute,
        HeightUnits = DimensionUnitType.Absolute,
        X = x,
        Y = y,
        Width = w,
        Height = h,
    };

    static readonly (string Name, SKFilterMode Filter, Func<SKBitmap, IEnumerable<GraphicalUiElement>> Build)[] Scenes =
    {
        ("scale3-nearest", SKFilterMode.Nearest, t => new[] { Sprite(t, 10, 10, 48, 48) }),
        ("scale3-linear", SKFilterMode.Linear, t => new[] { Sprite(t, 10, 10, 48, 48) }),
        ("fractional", SKFilterMode.Nearest, t => new[] { Sprite(t, 10.25f, 10.75f, 36.8f, 36.8f) }),
        ("nonuniform", SKFilterMode.Nearest, t => new[] { Sprite(t, 10, 10, 48, 24) }),
        ("rotate30-nearest", SKFilterMode.Nearest, t => { var s = Sprite(t, 40, 30, 40, 40); s.Rotation = 30; return new[] { s }; }),
        ("rotate30-linear", SKFilterMode.Linear, t => { var s = Sprite(t, 40, 30, 40, 40); s.Rotation = 30; return new[] { s }; }),
        ("flip", SKFilterMode.Nearest, t => { var a = Sprite(t, 4, 4, 40, 40); a.FlipHorizontal = true; var b = Sprite(t, 50, 50, 40, 40); b.FlipVertical = true; return new[] { a, b }; }),
        ("halfpixel-row", SKFilterMode.Nearest, t => Enumerable.Range(0, 4).Select(i => Sprite(t, 10.5f + i * 16, 10.5f, 16, 16, 8, 8, 8))),
        ("tint-alpha-nearest", SKFilterMode.Nearest, t => { var a = Sprite(t, 4, 4, 40, 40); a.Color = new SKColor(255, 128, 64, 255); var b = Sprite(t, 50, 50, 40, 40); b.Color = new SKColor(255, 128, 64, 128); return new[] { a, b }; }),
        ("tint-alpha-linear", SKFilterMode.Linear, t => { var a = Sprite(t, 4, 4, 40, 40); a.Color = new SKColor(255, 128, 64, 255); var b = Sprite(t, 50, 50, 40, 40); b.Color = new SKColor(255, 128, 64, 128); return new[] { a, b }; }),
        ("many-rotated", SKFilterMode.Nearest, t => Enumerable.Range(0, 6).Select(i => { var s = Sprite(t, i * 12, i * 10, 24, 24, i * 4, 0, 8); s.Rotation = i == 3 ? 15 : 0; return s; })),
    };

    static SystemManagers CreateManagers(SKCanvas canvas)
    {
        GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        var managers = new SystemManagers();
        managers.Initialize();
        SystemManagers.Default = managers;
        managers.Canvas = canvas;
        return managers;
    }

    [Fact]
    public void Snapshots_MatchFirstRun()
    {
        if (Folder == null)
        {
            return;
        }
        Directory.CreateDirectory(Folder);
        SKFilterMode previous = RenderingLibrary.Graphics.Renderer.TextureFilter;
        using SKBitmap texture = CreateTexture();
        try
        {
            foreach (var scene in Scenes)
            {
                RenderingLibrary.Graphics.Renderer.TextureFilter = scene.Filter;
                using SKBitmap actual = _gpu.Render(96, 96, 0, canvas =>
                {
                    SystemManagers managers = CreateManagers(canvas);
                    foreach (GraphicalUiElement element in scene.Build(texture))
                    {
                        element.AddToManagers(managers, null);
                    }
                    managers.Draw();
                });
                string path = Path.Combine(Folder, scene.Name + ".png");
                if (!File.Exists(path))
                {
                    using SKData data = actual.Encode(SKEncodedImageFormat.Png, 100);
                    File.WriteAllBytes(path, data.ToArray());
                    File.AppendAllText(Path.Combine(Folder, "results.txt"), $"{scene.Name}: written{Environment.NewLine}");
                    continue;
                }
                using SKBitmap expected = SKBitmap.Decode(path);
                int max = 0;
                int count = 0;
                for (int y = 0; y < 96; y++)
                {
                    for (int x = 0; x < 96; x++)
                    {
                        SKColor p = expected.GetPixel(x, y);
                        SKColor q = actual.GetPixel(x, y);
                        int d = Math.Max(Math.Max(Math.Abs(p.Red - q.Red), Math.Abs(p.Green - q.Green)), Math.Max(Math.Abs(p.Blue - q.Blue), Math.Abs(p.Alpha - q.Alpha)));
                        max = Math.Max(max, d);
                        count += d > 0 ? 1 : 0;
                    }
                }
                File.AppendAllText(Path.Combine(Folder, "results.txt"), $"{scene.Name}: max {max}, pixels {count}{Environment.NewLine}");
            }
        }
        finally
        {
            RenderingLibrary.Graphics.Renderer.TextureFilter = previous;
        }
    }

    [Fact]
    public void Timing_SameTextureSprites()
    {
        if (Folder == null)
        {
            return;
        }
        Directory.CreateDirectory(Folder);
        using SKBitmap texture = CreateTexture();
        _gpu.WithSurface(1024, 1024, (surface, context) =>
        {
            foreach (int count in new[] { 1, 10, 100, 1000 })
            {
                foreach (bool tinted in new[] { false, true })
                {
                    SystemManagers managers = CreateManagers(surface.Canvas);
                    for (int i = 0; i < count; i++)
                    {
                        SpriteRuntime sprite = Sprite(texture, i * 37 % 992, i * 53 % 992, 32, 32, i % 4 * 8, i / 4 % 4 * 8, 8);
                        if (tinted)
                        {
                            sprite.Color = new SKColor((byte)(i * 7), (byte)(255 - i * 3), (byte)(i * 11), 255);
                        }
                        sprite.AddToManagers(managers, null);
                    }
                    double best = double.MaxValue;
                    for (int run = 0; run < 3; run++)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            managers.Draw();
                            context.Flush(true, true);
                        }
                        int frames = 0;
                        var watch = Stopwatch.StartNew();
                        while (watch.ElapsedMilliseconds < 300 && frames < 2000)
                        {
                            managers.Draw();
                            context.Flush(true, true);
                            frames++;
                        }
                        best = Math.Min(best, watch.Elapsed.TotalMilliseconds * 1000 / frames);
                    }
                    int drawCalls = managers.Renderer.RenderStateChangeStatistics.DrawCallCount;
                    File.AppendAllText(Path.Combine(Folder, "results.txt"),
                        $"timing n={count} tinted={tinted}: {best:F1} us/frame, {drawCalls} draw calls, GL_RENDERER={_gpu.Renderer}{Environment.NewLine}");
                }
            }
        });
    }
}
