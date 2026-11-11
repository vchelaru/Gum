using Gum.RenderingLibrary;
using RenderingLibrary;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A renderable created in code has the same Blend a project-loaded one has: Normal, not null (#7878).
/// Normal and null draw identically, so nothing about the output changes.
/// </summary>
public class BlendDefaultTests
{
    public static IEnumerable<object[]> Shapes() => new[]
    {
        new object[] { new Circle() },
        new object[] { new Arc() },
        new object[] { new RoundedRectangle() },
        new object[] { new LineRectangle() },
        new object[] { new LineGrid() },
        new object[] { new Polygon() },
        new object[] { new Sprite() },
        new object[] { new NineSlice() },
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public void Shape_Blend_DefaultsToNormal(RenderableShapeBase sut)
    {
        sut.Blend.ShouldBe(Blend.Normal);
    }

    [Fact]
    public void Text_Blend_DefaultsToNormal_AndNeedsNoRenderPaint()
    {
        Text sut = new Text();

        sut.Blend.ShouldBe(Blend.Normal);
        // No SaveLayer for a plain text: Normal takes the same cheap path null does.
        sut.GetRenderPaint().ShouldBeNull();
    }

    [Fact]
    public void Text_NormalAndNullBlend_DrawTheSame()
    {
        byte[] fromDefault = RenderText(null);
        byte[] fromNull = RenderText(() => { });
        fromDefault.ShouldBe(fromNull);
    }

    private static byte[] RenderText(Action? setNull)
    {
        Text text = new Text();
        text.RawText = "Hello";
        text.FontSize = 24;
        text.Width = 200;
        text.Height = 100;
        if (setNull != null)
        {
            text.Blend = null;
        }
        using SKSurface surface = SKSurface.Create(new SKImageInfo(260, 160));
        SystemManagers managers = new SystemManagers { Canvas = surface.Canvas };
        surface.Canvas.Clear(SKColors.White);
        text.Render(managers);
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    [Theory]
    [InlineData(255)]
    [InlineData(128)]
    public void ShadowedShape_DefaultAndNullBlend_DrawTheSame(byte alpha)
    {
        using SKBitmap fromDefault = RenderDefault(alpha);
        using SKBitmap fromNull = DropshadowBlendTests.RenderNew(null, alpha);

        fromDefault.Bytes.ShouldBe(fromNull.Bytes);
    }

    private static SKBitmap RenderDefault(byte alpha)
    {
        SKBitmap bitmap = new SKBitmap(120, 120);
        using SKCanvas canvas = new SKCanvas(bitmap);
        DropshadowBlendTests.FillBackdrop(canvas);
        SystemManagers managers = new SystemManagers { Canvas = canvas };
        RoundedRectangle shape = new RoundedRectangle();
        shape.X = 36;
        shape.Y = 36;
        shape.Width = 40;
        shape.Height = 40;
        shape.CornerRadius = 6f;
        shape.Color = new SKColor(200, 80, 80, alpha);
        shape.HasDropshadow = true;
        shape.DropshadowOffsetX = 4;
        shape.DropshadowOffsetY = 4;
        shape.DropshadowBlurX = 8;
        shape.DropshadowBlurY = 8;
        shape.DropshadowColor = new SKColor(0, 0, 0, 160);
        shape.Render(managers);
        return bitmap;
    }
}
