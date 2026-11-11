using Gum.RenderingLibrary;
using RenderingLibrary;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A dropshadow shape takes the fast blurred-draw path whatever its <see cref="Blend"/> (#7878): project-loaded
/// shapes always have Blend = Normal, so a path that bailed out on any Blend never ran in a real scene.
/// </summary>
public class DropshadowBlendTests
{
    private static readonly SKRect Bounds = new SKRect(0, 0, 100, 100);
    private const int Size = 120;
    private const float Radius = 6f;
    private static readonly float Sigma = global::Gum.Wireframe.DropshadowBlur.GetSigma(8);

    private sealed class ProbeShape : RoundedRectangle
    {
        public SKPaint Paint() => GetCachedPaint(Bounds, absoluteRotation: 0);
        public SKPaint? Shadow() => GetShadowPaint();
    }

    private static ProbeShape CreateProbe(Blend? blend)
    {
        ProbeShape shape = new ProbeShape();
        shape.HasDropshadow = true;
        shape.DropshadowBlurX = 8;
        shape.DropshadowBlurY = 8;
        shape.Blend = blend;
        return shape;
    }

    public static IEnumerable<object?[]> AllBlends() =>
        new object?[][] { new object?[] { null } }
            .Concat(Enum.GetValues<Blend>().Where(b => b != Blend.Replace).Select(b => new object?[] { b }));

    [Theory]
    [MemberData(nameof(AllBlends))]
    public void Shadow_UsesBlurredDraw_ForEveryBlendButReplace(Blend? blend)
    {
        ProbeShape shape = CreateProbe(blend);
        shape.Paint();

        shape.Shadow().ShouldNotBeNull();
    }

    [Theory]
    [MemberData(nameof(AllBlends))]
    public void Shadow_Paint_CarriesTheShapesBlend(Blend? blend)
    {
        ProbeShape shape = CreateProbe(blend);
        shape.Paint();

        SKPaint? shadow = shape.Shadow();
        shadow.ShouldNotBeNull();
        // A whole-bounds blend (ReplaceAlpha, MinAlpha) is applied once to a layer around both draws instead.
        bool layered = blend == Blend.ReplaceAlpha || blend == Blend.MinAlpha;
        SKBlendMode expected = blend.HasValue && !layered ? blend.Value.ToSKBlendMode() : SKBlendMode.SrcOver;
        shadow.BlendMode.ShouldBe(expected);
    }

    private static readonly Blend?[] PixelBlends =
    {
        null, Blend.Normal, Blend.Additive, Blend.SubtractAlpha, Blend.ReplaceAlpha, Blend.MinAlpha,
    };

    public static IEnumerable<object?[]> PixelCases() =>
        PixelBlends.SelectMany(b => new[] { 255, 128 }.Select(a => new object?[] { b, (byte)a }));

    internal static void FillBackdrop(SKCanvas canvas)
    {
        canvas.Clear(new SKColor(60, 120, 200, 220));
    }

    internal static SKBitmap RenderNew(Blend? blend, byte fillAlpha)
    {
        SKBitmap bitmap = new SKBitmap(Size, Size);
        using SKCanvas canvas = new SKCanvas(bitmap);
        FillBackdrop(canvas);
        SystemManagers managers = new SystemManagers();
        managers.Canvas = canvas;
        RoundedRectangle shape = new RoundedRectangle();
        shape.X = 36;
        shape.Y = 36;
        shape.Width = 40;
        shape.Height = 40;
        shape.CornerRadius = Radius;
        shape.Color = new SKColor(200, 80, 80, fillAlpha);
        shape.HasDropshadow = true;
        shape.DropshadowOffsetX = 4;
        shape.DropshadowOffsetY = 4;
        shape.DropshadowBlurX = 8;
        shape.DropshadowBlurY = 8;
        shape.DropshadowColor = new SKColor(0, 0, 0, 160);
        shape.Blend = blend;
        shape.Render(managers);
        return bitmap;
    }

    // What the renderable drew before: the image filter on the shape's paint, with the blend on that paint
    // (or on a layer around it for the whole-bounds blends).
    internal static SKBitmap RenderOld(Blend? blend, byte fillAlpha)
    {
        SKBitmap bitmap = new SKBitmap(Size, Size);
        using SKCanvas canvas = new SKCanvas(bitmap);
        FillBackdrop(canvas);
        using SKImageFilter filter = SKImageFilter.CreateDropShadow(4, 4, Sigma, Sigma, new SKColor(0, 0, 0, 160));
        using SKPaint paint = new SKPaint { Color = new SKColor(200, 80, 80, fillAlpha), IsAntialias = true, ImageFilter = filter };
        bool layered = blend == Blend.ReplaceAlpha || blend == Blend.MinAlpha;
        SKRect rect = new SKRect(36, 36, 76, 76);
        if (layered)
        {
            using SKPaint layerPaint = new SKPaint();
            layerPaint.ApplyBlend(blend!.Value);
            canvas.Save();
            canvas.ClipRect(rect);
            canvas.SaveLayer(layerPaint);
            canvas.DrawRoundRect(rect, Radius, Radius, paint);
            canvas.Restore();
            canvas.Restore();
        }
        else
        {
            if (blend.HasValue)
            {
                paint.ApplyBlend(blend.Value);
            }
            canvas.DrawRoundRect(rect, Radius, Radius, paint);
        }
        return bitmap;
    }

    internal static int MaxDifference(SKBitmap a, SKBitmap b)
    {
        byte[] p = a.Bytes;
        byte[] q = b.Bytes;
        int max = 0;
        for (int i = 0; i < p.Length; i++)
        {
            max = Math.Max(max, Math.Abs(p[i] - q[i]));
        }
        return max;
    }

    [Theory]
    [MemberData(nameof(PixelCases))]
    public void Render_MatchesTheImageFilterOutput_ForEachBlend(Blend? blend, byte fillAlpha)
    {
        using SKBitmap actual = RenderNew(blend, fillAlpha);
        using SKBitmap expected = RenderOld(blend, fillAlpha);

        int difference = MaxDifference(actual, expected);
        // Edge pixels of the anti-aliased rim differ by a few levels (up to 8 for the layered blends, and a few
        // single corner pixels of a layered translucent shape further); count those separately.
        if (blend == Blend.ReplaceAlpha || blend == Blend.MinAlpha)
        {
            CountDifferences(actual, expected, 8).ShouldBeLessThanOrEqualTo(8);
            return;
        }
        difference.ShouldBeLessThanOrEqualTo(5, "max channel difference " + difference);
    }

    internal static int CountDifferences(SKBitmap a, SKBitmap b, int threshold)
    {
        int count = 0;
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                SKColor p = a.GetPixel(x, y);
                SKColor q = b.GetPixel(x, y);
                int d = Math.Max(Math.Max(Math.Abs(p.Red - q.Red), Math.Abs(p.Green - q.Green)), Math.Max(Math.Abs(p.Blue - q.Blue), Math.Abs(p.Alpha - q.Alpha)));
                if (d > threshold)
                {
                    count++;
                }
            }
        }
        return count;
    }

    // A Replace (Src) blend replaces the whole shadow-and-shape bounds when drawn through the image filter, which a
    // blurred draw does not, so it keeps the image filter.
    [Fact]
    public void Shadow_KeepsTheImageFilter_ForReplace()
    {
        ProbeShape shape = CreateProbe(Blend.Replace);
        shape.Paint();

        shape.Shadow().ShouldBeNull();
    }

    // Project-loaded shapes carry Blend = Normal and shapes made in code carry null; they must draw the same.
    [Fact]
    public void Render_NormalBlend_DrawsTheSameAsNoBlend()
    {
        using SKBitmap withNull = RenderNew(null, 255);
        using SKBitmap withNormal = RenderNew(Blend.Normal, 255);

        MaxDifference(withNull, withNormal).ShouldBe(0);
    }

    [Fact]
    public void Normal_AndNullBlend_DrawTheSameAndUseTheSameShadowPath()
    {
        ProbeShape fromCode = CreateProbe(null);
        ProbeShape fromProject = CreateProbe(Blend.Normal);
        fromCode.Paint();
        fromProject.Paint();

        (fromProject.Shadow() != null).ShouldBe(fromCode.Shadow() != null);
        fromProject.Shadow()!.BlendMode.ShouldBe(fromCode.Shadow()!.BlendMode);
        foreach (byte alpha in new byte[] { 255, 128 })
        {
            using SKBitmap withNull = RenderNew(null, alpha);
            using SKBitmap withNormal = RenderNew(Blend.Normal, alpha);
            MaxDifference(withNull, withNormal).ShouldBe(0);
        }
    }

    [Fact]
    public void Shadow_BlendChangedAfterFirstDraw_IsPickedUp()
    {
        ProbeShape shape = CreateProbe(null);
        shape.Paint();

        shape.Blend = Blend.Additive;
        shape.Paint();

        shape.Shadow()!.BlendMode.ShouldBe(SKBlendMode.Plus);
    }
}
