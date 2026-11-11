using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;
using Xunit;

#pragma warning disable CS0618

namespace SkiaGum.GpuTests;

public class DropshadowRenderTests
{
    const int Size = 120;

    static RoundedRectangleRuntime CreateShape(byte fillAlpha = 255)
    {
        var r = new RoundedRectangleRuntime();
        r.X = 30;
        r.Y = 30;
        r.WidthUnits = DimensionUnitType.Absolute;
        r.HeightUnits = DimensionUnitType.Absolute;
        r.Width = 48;
        r.Height = 48;
        r.Color = new SKColor(200, 80, 80, fillAlpha);
        r.HasDropshadow = true;
        r.DropshadowOffsetX = 4;
        r.DropshadowOffsetY = 4;
        r.DropshadowBlurX = 8;
        r.DropshadowBlurY = 8;
        r.DropshadowColor = new SKColor(0, 0, 0, 160);
        return r;
    }

    static SystemManagers CreateManagers(SKCanvas canvas)
    {
        GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        var managers = new SystemManagers();
        managers.Initialize();
        SystemManagers.Default = managers;
        managers.Canvas = canvas;
        return managers;
    }

    static SKBitmap RenderGum(RoundedRectangleRuntime shape)
    {
        var bitmap = new SKBitmap(Size, Size);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        SystemManagers managers = CreateManagers(canvas);
        shape.AddToManagers(managers, null);
        managers.Draw();
        shape.RemoveFromManagers();
        return bitmap;
    }

    // The reference: the drop shadow image filter drawn directly, which is what the renderable used before.
    static SKBitmap RenderImageFilterReference(byte fillAlpha)
    {
        var bitmap = new SKBitmap(Size, Size);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        float sigma = DropshadowBlur.GetSigma(8);
        using var filter = SKImageFilter.CreateDropShadow(4, 4, sigma, sigma, new SKColor(0, 0, 0, 160));
        using var paint = new SKPaint { Color = new SKColor(200, 80, 80, fillAlpha), IsAntialias = true, ImageFilter = filter };
        canvas.DrawRoundRect(new SKRect(30, 30, 78, 78), 5, 5, paint);
        return bitmap;
    }

    static int MaxChannelDifference(SKBitmap a, SKBitmap b)
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

    [Fact]
    public void Render_OpaqueFill_MatchesImageFilterReference()
    {
        using SKBitmap actual = RenderGum(CreateShape());
        using SKBitmap expected = RenderImageFilterReference(255);
        int d = MaxChannelDifference(actual, expected);
        d.ShouldBeLessThanOrEqualTo(2, "d=" + d);
    }

    [Fact]
    public void Render_TranslucentFill_MatchesImageFilterReference()
    {
        using SKBitmap actual = RenderGum(CreateShape(128));
        using SKBitmap expected = RenderImageFilterReference(128);
        int d = MaxChannelDifference(actual, expected);
        d.ShouldBeLessThanOrEqualTo(2, "d=" + d);
    }

    [Fact]
    public void Render_ShadowedRoundedRectangle_DrawsShadowAsItsOwnOperation()
    {
        // The slow part on the GPU is the image filter (an offscreen layer per shape). The shadow is a plain
        // blurred draw instead, so the shape records a shadow draw and a fill draw, not one draw wrapped in a filter layer (2 operations).
        using var recorder = new SKPictureRecorder();
        SKCanvas canvas = recorder.BeginRecording(new SKRect(0, 0, Size, Size));
        SystemManagers managers = CreateManagers(canvas);
        RoundedRectangleRuntime shape = CreateShape();
        shape.AddToManagers(managers, null);
        managers.Draw();
        shape.RemoveFromManagers();
        using SKPicture picture = recorder.EndRecording();

        picture.ApproximateOperationCount.ShouldBeGreaterThan(2);
    }
}
