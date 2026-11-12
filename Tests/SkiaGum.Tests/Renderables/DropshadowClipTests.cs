using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A dropshadow shape under a clipping ancestor takes the blurred mask-filter draw like an unclipped one (#7945).
/// The clip is applied to the canvas, so the shadow is cropped at the clip edge by the canvas clip in both draws.
/// </summary>
public class DropshadowClipTests
{
    private const int Size = 160;
    private static readonly SKColor DropColor = new SKColor(0, 0, 0, 160);

    internal sealed record Scene(string Name, float ClipX, float ClipY, float ClipW, float ClipH, float ClipRotation,
        float X, float Y, float W, float H, float ChildRotation, float OffsetX, float OffsetY, float BlurX, float BlurY);

    internal static readonly Scene[] Scenes =
    {
        new("Inside clip, blur crosses the right edge", 20, 20, 100, 100, 0, 70, 50, 40, 40, 0, 4, 4, 12, 12),
        new("Shape straddles the clip edge", 20, 20, 100, 100, 0, 95, 50, 50, 40, 0, 4, 4, 8, 8),
        new("Shape fully outside, shadow inside (offset x)", 20, 20, 100, 100, 0, 106, 50, 30, 30, 0, -14, 0, 8, 8),
        new("Shape fully outside, shadow inside (offset y)", 20, 20, 100, 100, 0, 50, 106, 30, 30, 0, 0, -14, 8, 8),
        new("Baseline: same as the first, clip covers the canvas", 0, 0, 160, 160, 0, 70, 50, 40, 40, 0, 4, 4, 12, 12),
        new("Baseline: same as straddle, clip covers the canvas", 0, 0, 160, 160, 0, 95, 50, 50, 40, 0, 4, 4, 8, 8),
        new("Shape fully inside, nothing reaches the edge", 20, 20, 100, 100, 0, 50, 50, 30, 30, 0, 3, 3, 6, 6),
        new("Rotated clipper, shape counter-rotated to 0", 40, 20, 90, 90, 30, 60, 40, 30, 30, -30, 4, 4, 8, 8),
        new("Rotated clipper, blur crosses clip edge", 40, 20, 90, 90, 30, 70, 10, 30, 30, -30, 6, 6, 12, 12),
        new("Shape rotated 1 degree (image filter)", 20, 20, 100, 100, 0, 60, 50, 40, 40, 1, 4, 4, 8, 8),
        new("Unequal blur 8/9 (image filter)", 20, 20, 100, 100, 0, 60, 50, 40, 40, 0, 4, 4, 8, 9),
    };

    private static void Configure(RoundedRectangle shape, Scene s)
    {
        shape.X = s.X;
        shape.Y = s.Y;
        shape.Width = s.W;
        shape.Height = s.H;
        shape.Rotation = s.ChildRotation;
        shape.CornerRadius = 6;
        shape.Color = new SKColor(200, 80, 80, 255);
        shape.HasDropshadow = true;
        shape.DropshadowOffsetX = s.OffsetX;
        shape.DropshadowOffsetY = s.OffsetY;
        shape.DropshadowBlurX = s.BlurX;
        shape.DropshadowBlurY = s.BlurY;
        shape.DropshadowColor = DropColor;
    }

    private static RoundedRectangle CreateClipper(Scene s)
    {
        RoundedRectangle clipper = new RoundedRectangle();
        clipper.X = s.ClipX;
        clipper.Y = s.ClipY;
        clipper.Width = s.ClipW;
        clipper.Height = s.ClipH;
        clipper.Rotation = s.ClipRotation;
        clipper.ClipsChildren = true;
        return clipper;
    }

    // The real shape under a real clipper, drawn the way the renderer does: the canvas clip, then Render.
    internal static SKBitmap RenderNew(Scene s)
    {
        SKBitmap bitmap = new SKBitmap(Size, Size);
        using SKCanvas canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(60, 120, 200, 255));
        SystemManagers managers = new SystemManagers();
        managers.Canvas = canvas;
        RoundedRectangle clipper = CreateClipper(s);
        RoundedRectangle shape = new RoundedRectangle();
        Configure(shape, s);
        shape.Parent = clipper;
        canvas.Save();
        canvas.ClipTo(clipper);
        shape.Render(managers);
        canvas.Restore();
        return bitmap;
    }

    // What the shape drew under a clip before: the drop-shadow image filter on the shape's paint, same clip.
    internal static SKBitmap RenderOld(Scene s)
    {
        SKBitmap bitmap = new SKBitmap(Size, Size);
        using SKCanvas canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(60, 120, 200, 255));
        RoundedRectangle clipper = CreateClipper(s);
        RoundedRectangle shape = new RoundedRectangle();
        Configure(shape, s);
        shape.Parent = clipper;
        float x = shape.GetAbsoluteX();
        float y = shape.GetAbsoluteY();
        float rotation = shape.GetAbsoluteRotation();
        using SKImageFilter filter = SKImageFilter.CreateDropShadow(s.OffsetX, s.OffsetY,
            global::Gum.Wireframe.DropshadowBlur.GetSigma(s.BlurX), global::Gum.Wireframe.DropshadowBlur.GetSigma(s.BlurY), DropColor);
        using SKPaint paint = new SKPaint { Color = shape.Color, IsAntialias = true, ImageFilter = filter };
        canvas.Save();
        canvas.ClipTo(clipper);
        if (rotation != 0)
        {
            canvas.Translate(x, y);
            canvas.RotateDegrees(-rotation);
            canvas.DrawRoundRect(new SKRect(0, 0, s.W, s.H), 6, 6, paint);
        }
        else
        {
            canvas.DrawRoundRect(new SKRect(x, y, x + s.W, y + s.H), 6, 6, paint);
        }
        canvas.Restore();
        return bitmap;
    }

    public static IEnumerable<object[]> AllScenes() => Scenes.Select(s => new object[] { s.Name });

    [Theory]
    [MemberData(nameof(AllScenes))]
    public void Render_UnderAClip_MatchesTheImageFilterOutput(string name)
    {
        Scene scene = Scenes.Single(s => s.Name == name);
        using SKBitmap actual = RenderNew(scene);
        using SKBitmap expected = RenderOld(scene);

        // The mask-filter draw differs from the image filter by up to 2 levels on an unclipped shape too (the
        // "Baseline" scenes, whose clip covers the canvas); a clip adds nothing to that.
        DropshadowBlendTests.MaxDifference(actual, expected).ShouldBeLessThanOrEqualTo(2, name);
    }

    private sealed class ProbeShape : RoundedRectangle
    {
        public SKPaint Paint() => GetCachedPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);
        public SKPaint? Shadow() => GetShadowPaint();
    }

    private static ProbeShape CreateClippedProbe(float x, float y)
    {
        RoundedRectangle clipper = new RoundedRectangle();
        clipper.X = 100;
        clipper.Y = 100;
        clipper.Width = 100;
        clipper.Height = 100;
        clipper.ClipsChildren = true;
        ProbeShape shape = new ProbeShape();
        shape.Width = 20;
        shape.Height = 20;
        shape.X = x;
        shape.Y = y;
        shape.HasDropshadow = true;
        shape.DropshadowBlurX = 6;
        shape.DropshadowBlurY = 6;
        shape.Parent = clipper;
        return shape;
    }

    [Fact]
    public void Shadow_ShapeInsideItsClip_UsesTheBlurredDraw()
    {
        ProbeShape shape = CreateClippedProbe(30, 30);
        SKPaint paint = shape.Paint();

        shape.Shadow().ShouldNotBeNull();
        paint.ImageFilter.ShouldBeNull();
    }

    [Theory]
    [InlineData(90, 30)]
    [InlineData(30, 90)]
    [InlineData(-10, 30)]
    [InlineData(150, 30)]
    public void Shadow_ShapeCrossingOrOutsideItsClip_KeepsTheImageFilter(float x, float y)
    {
        ProbeShape shape = CreateClippedProbe(x, y);
        SKPaint paint = shape.Paint();

        shape.Shadow().ShouldBeNull();
        paint.ImageFilter.ShouldNotBeNull();
    }

    [Fact]
    public void Shadow_ShapeScrollsAcrossTheClipEdgeAndBack_SwitchesDrawEachTime()
    {
        ProbeShape shape = CreateClippedProbe(30, 30);
        shape.Paint().ImageFilter.ShouldBeNull();

        shape.X = 90;
        shape.Paint().ImageFilter.ShouldNotBeNull();
        shape.Shadow().ShouldBeNull();

        shape.X = 30;
        shape.Paint().ImageFilter.ShouldBeNull();
        shape.Shadow().ShouldNotBeNull();
    }

    [Fact]
    public void Shadow_ShapeRotatedAfterFirstDraw_SwitchesToTheImageFilter()
    {
        ProbeShape shape = CreateClippedProbe(30, 30);
        shape.Paint().ImageFilter.ShouldBeNull();

        shape.Rotation = 20;

        shape.Paint().ImageFilter.ShouldNotBeNull();
        shape.Shadow().ShouldBeNull();
    }

    [Fact]
    public void Shadow_ClippedShapeWithClipperResizedToCoverIt_SwitchesToTheBlurredDraw()
    {
        ProbeShape shape = CreateClippedProbe(90, 30);
        shape.Paint().ImageFilter.ShouldNotBeNull();

        shape.Parent!.Width = 200;

        shape.Paint().ImageFilter.ShouldBeNull();
        shape.Shadow().ShouldNotBeNull();
    }

    [Fact]
    public void ContactSheet_WhenRequested_IsWritten()
    {
        string? directory = Environment.GetEnvironmentVariable("GUM_7945_SHEET_DIR");
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }
        const int amplify = 16;
        const int tile = Size;
        int labelHeight = 28;
        using SKBitmap sheet = new SKBitmap(tile * 3 + 40, Scenes.Length * (tile + labelHeight) + 10);
        using SKCanvas canvas = new SKCanvas(sheet);
        canvas.Clear(SKColors.White);
        using SKPaint text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        using SKFont font = new SKFont { Size = 12 };
        using SKPaint box = new SKPaint { Color = SKColors.Yellow, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        string report = "";
        for (int i = 0; i < Scenes.Length; i++)
        {
            Scene s = Scenes[i];
            using SKBitmap oldBitmap = RenderOld(s);
            using SKBitmap newBitmap = RenderNew(s);
            using SKBitmap diff = new SKBitmap(Size, Size);
            int max = 0;
            int count = 0;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    SKColor a = oldBitmap.GetPixel(x, y);
                    SKColor b = newBitmap.GetPixel(x, y);
                    int d = Math.Max(Math.Max(Math.Abs(a.Red - b.Red), Math.Abs(a.Green - b.Green)), Math.Abs(a.Blue - b.Blue));
                    max = Math.Max(max, d);
                    count += d > 1 ? 1 : 0;
                    if (d > 4)
                    {
                        report += $"   ({x},{y}) old {a} new {b}\n";
                    }
                    byte v = (byte)Math.Min(255, d * amplify);
                    diff.SetPixel(x, y, new SKColor(v, v, v));
                }
            }
            float top = i * (tile + labelHeight) + labelHeight;
            string label = $"{s.Name} | clip {s.ClipW}x{s.ClipH} rot {s.ClipRotation} | shape {s.W}x{s.H} child rot {s.ChildRotation} | off {s.OffsetX},{s.OffsetY} blur {s.BlurX},{s.BlurY} | max diff {max}, px>1: {count}";
            canvas.DrawText(label, 4, top - 10, font, text);
            canvas.DrawBitmap(oldBitmap, 0, top);
            canvas.DrawBitmap(newBitmap, tile + 20, top);
            canvas.DrawBitmap(diff, tile * 2 + 40, top);
            // Box: the clip rectangle (turned with the clipper), on the old and new tiles.
            foreach (float left in new[] { 0f, tile + 20f })
            {
                canvas.Save();
                canvas.Translate(left, top);
                canvas.Concat(SKMatrix.CreateTranslation(s.ClipX, s.ClipY).PreConcat(SKMatrix.CreateRotationDegrees(-s.ClipRotation)));
                canvas.DrawRect(new SKRect(0, 0, s.ClipW, s.ClipH), box);
                canvas.Restore();
            }
            report += $"{s.Name}: max {max}, px>1 {count}\n";
        }
        Directory.CreateDirectory(directory);
        using SKImage image = SKImage.FromBitmap(sheet);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(directory, "7945-sheet.png"), data.ToArray());
        File.WriteAllText(Path.Combine(directory, "7945-report.txt"), report);
    }
}
