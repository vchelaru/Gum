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

    const int CaseSize = 240;

    static SKBitmap RenderCaseGum(byte fillAlpha, float rotation, float offsetX, float offsetY, float blur)
    {
        RoundedRectangleRuntime shape = CreateShape(fillAlpha);
        shape.X = 100;
        shape.Y = 100;
        shape.Rotation = rotation;
        shape.DropshadowOffsetX = offsetX;
        shape.DropshadowOffsetY = offsetY;
        shape.DropshadowBlurX = blur;
        shape.DropshadowBlurY = blur;
        var bitmap = new SKBitmap(CaseSize, CaseSize);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        SystemManagers managers = CreateManagers(canvas);
        shape.AddToManagers(managers, null);
        managers.Draw();
        shape.RemoveFromManagers();
        return bitmap;
    }

    // What the renderable drew before the blurred shadow draw: the image filter on the shape's paint.
    static SKBitmap RenderCaseReference(byte fillAlpha, float rotation, float offsetX, float offsetY, float blur)
    {
        var bitmap = new SKBitmap(CaseSize, CaseSize);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        float sigma = DropshadowBlur.GetSigma(blur);
        using var filter = SKImageFilter.CreateDropShadow(offsetX, offsetY, sigma, sigma, new SKColor(0, 0, 0, 160));
        using var paint = new SKPaint { Color = new SKColor(200, 80, 80, fillAlpha), IsAntialias = true, ImageFilter = filter };
        if (rotation != 0)
        {
            canvas.Translate(100, 100);
            canvas.RotateDegrees(-rotation);
            canvas.DrawRoundRect(new SKRect(0, 0, 48, 48), 5, 5, paint);
        }
        else
        {
            canvas.DrawRoundRect(new SKRect(100, 100, 148, 148), 5, 5, paint);
        }
        return bitmap;
    }

    [Theory]
    [InlineData(255, 209f, -6f, -17f, 21f)]
    [InlineData(255, 58f, 16f, 13f, 19f)]
    [InlineData(255, 90f, 8f, 0f, 8f)]
    public void Render_RotatedShape_MatchesImageFilterReference(byte fillAlpha, float rotation, float offsetX, float offsetY, float blur)
    {
        using SKBitmap actual = RenderCaseGum(fillAlpha, rotation, offsetX, offsetY, blur);
        using SKBitmap expected = RenderCaseReference(fillAlpha, rotation, offsetX, offsetY, blur);
        MaxChannelDifference(actual, expected).ShouldBeLessThanOrEqualTo(2);
    }

    [Theory]
    [InlineData(255, 0f, 4f, 4f, 1f)]
    [InlineData(128, 0f, 4f, 4f, 1f)]
    [InlineData(255, 0f, 20f, -20f, 100f)]
    [InlineData(255, 0f, -12f, 3f, 100f)]
    [InlineData(255, 0f, 3f, -13f, 39f)]
    [InlineData(255, 0f, 4f, 4f, 40f)]
    [InlineData(128, 0f, 20f, -20f, 5f)]
    [InlineData(128, 0f, 20f, -20f, 40f)]
    [InlineData(255, 0f, 20f, -20f, 4.9f)]
    [InlineData(255, 0f, 20f, -20f, 40.1f)]
    public void Render_BlurRange_MatchesImageFilterReference(byte fillAlpha, float rotation, float offsetX, float offsetY, float blur)
    {
        using SKBitmap actual = RenderCaseGum(fillAlpha, rotation, offsetX, offsetY, blur);
        using SKBitmap expected = RenderCaseReference(fillAlpha, rotation, offsetX, offsetY, blur);
        MaxChannelDifference(actual, expected).ShouldBeLessThanOrEqualTo(2);
    }


    [Theory]
    [InlineData(110f, 110f, 30f, 30f)]
    [InlineData(120f, 120f, 12f, 12f)]
    [InlineData(95f, 105f, 40f, 17f)]
    [InlineData(130f, 90f, 25f, 70f)]
    public void Render_ShapeUnderParentClip_MatchesImageFilterReference(float clipX, float clipY, float clipWidth, float clipHeight)
    {
        RoundedRectangleRuntime shape = CreateShape();
        shape.X = 100 - clipX;
        shape.Y = 100 - clipY;
        shape.DropshadowOffsetX = 1;
        shape.DropshadowOffsetY = -17;
        shape.DropshadowBlurX = 18;
        shape.DropshadowBlurY = 18;
        var parent = new ContainerRuntime();
        parent.X = clipX;
        parent.Y = clipY;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.Width = clipWidth;
        parent.Height = clipHeight;
        parent.ClipsChildren = true;
        shape.Parent = parent;

        using var actual = new SKBitmap(CaseSize, CaseSize);
        using (var canvas = new SKCanvas(actual))
        {
            canvas.Clear(SKColors.Transparent);
            SystemManagers managers = CreateManagers(canvas);
            parent.AddToManagers(managers, null);
            managers.Draw();
            parent.RemoveFromManagers();
        }

        using var expected = new SKBitmap(CaseSize, CaseSize);
        using (var canvas = new SKCanvas(expected))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.ClipRect(new SKRect(clipX, clipY, clipX + clipWidth, clipY + clipHeight), antialias: true);
            float sigma = DropshadowBlur.GetSigma(18);
            using var filter = SKImageFilter.CreateDropShadow(1, -17, sigma, sigma, new SKColor(0, 0, 0, 160));
            using var paint = new SKPaint { Color = new SKColor(200, 80, 80, 255), IsAntialias = true, ImageFilter = filter };
            canvas.DrawRoundRect(new SKRect(100, 100, 148, 148), 5, 5, paint);
        }

        MaxChannelDifference(actual, expected).ShouldBeLessThanOrEqualTo(2);
    }
}
