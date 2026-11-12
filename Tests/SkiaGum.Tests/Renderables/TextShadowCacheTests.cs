using Shouldly;
using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Issues #7910, #7944: a Text drop shadow is the text drawn again, tinted and blurred through ConfigurePaint, instead of an offscreen layer with
/// an image filter. These compare it with the layer path (forced through <c>ForceLayerShadow</c>).
/// </summary>
public class TextShadowCacheTests
{
    private const int Width = 320;
    private const int Height = 140;

    public TextShadowCacheTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    private static TextRuntime CreateText(float blur, float offsetX, float offsetY, string markup = "Score: 1234", float fontSize = 20)
    {
        TextRuntime text = new TextRuntime();
        text.Text = markup;
        text.FontSize = (int)fontSize;
        text.X = 40;
        text.Y = 40;
        text.HasDropshadow = true;
        text.DropshadowColor = SKColors.Black;
        text.DropshadowBlur = blur;
        text.DropshadowOffsetX = offsetX;
        text.DropshadowOffsetY = offsetY;
        return text;
    }

    // The mask is built once the inputs held still for a few frames, so the cached path needs several.
    private const int SettledFrames = 4;

    private static SKBitmap Render(TextRuntime text, bool forceLayer, int frames = SettledFrames, float canvasScale = 1)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(Width, Height));
        GumService.Default.Initialize(surface.Canvas, Width, Height);
        GumService.Default.Root.Children.Clear();
        ((Text)text.RenderableComponent).ForceLayerShadow = forceLayer;
        GumService.Default.Root.Children.Add(text);
        for (int i = 0; i < frames; i++)
        {
            surface.Canvas.Clear(new SKColor(128, 128, 128));
            surface.Canvas.Save();
            surface.Canvas.Scale(canvasScale);
            GumService.Default.Draw();
            surface.Canvas.Restore();
        }
        GumService.Default.Root.Children.Clear();
        return SKBitmap.FromImage(surface.Snapshot());
    }

    // Compared over a fixed backdrop so a blend that writes alpha is judged by what it shows, not by unpremultiplied noise at near-zero alpha.
    private static SKColor Over(SKColor c)
    {
        float alpha = c.Alpha / 255f;
        return new SKColor((byte)(c.Red * alpha + 255 * (1 - alpha)), (byte)(c.Green * alpha), (byte)(c.Blue * alpha + 255 * (1 - alpha)));
    }

    // glyphMask: pixels within two pixels of text in this render are skipped, to judge only the shadow.
    private static bool NearGlyph(SKBitmap mask, int x, int y)
    {
        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int px = x + dx;
                int py = y + dy;
                if (px >= 0 && py >= 0 && px < mask.Width && py < mask.Height && mask.GetPixel(px, py) != new SKColor(128, 128, 128))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static (int MaxDifference, double MeanDifference) Compare(SKBitmap a, SKBitmap b, SKBitmap? glyphMask = null)
    {
        int max = 0;
        double sum = 0;
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                if (glyphMask != null && NearGlyph(glyphMask, x, y))
                {
                    continue;
                }
                SKColor ca = Over(a.GetPixel(x, y));
                SKColor cb = Over(b.GetPixel(x, y));
                int d = System.Math.Max(System.Math.Abs(ca.Red - cb.Red), System.Math.Max(System.Math.Abs(ca.Green - cb.Green), System.Math.Abs(ca.Blue - cb.Blue)));
                max = System.Math.Max(max, d);
                sum += d;
            }
        }
        return (max, sum / (a.Width * a.Height));
    }

    // The farthest pixel left of the text's start still darker than the 5% shadow level against the gray background.
    private static int ReachLeft(SKBitmap bitmap, int row)
    {
        int reach = 0;
        for (int x = 40; x >= 0; x--)
        {
            if (128 - bitmap.GetPixel(x, row).Red >= 6)
            {
                reach = 40 - x;
            }
        }
        return reach;
    }

    [Theory]
    [InlineData(1, 0, 0.75f)]
    [InlineData(2, 0, 0.75f)]
    [InlineData(8, 4, 4)]
    [InlineData(20, 4, 4)]
    [InlineData(8, -3, 6)]
    public void Draw_TextDropshadow_MatchesLayerPath(float blur, float offsetX, float offsetY)
    {
        using SKBitmap layer = Render(CreateText(blur, offsetX, offsetY), forceLayer: true);
        using SKBitmap cached = Render(CreateText(blur, offsetX, offsetY), forceLayer: false);

        (int max, double mean) = Compare(layer, cached);
        max.ShouldBeLessThanOrEqualTo(40);
        mean.ShouldBeLessThan(0.5);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    [InlineData(20)]
    public void Draw_TextDropshadowWithBbCodeAndTranslucentColor_MatchesLayerPath(float blur)
    {
        TextRuntime Create()
        {
            TextRuntime text = CreateText(blur, 4, 4, "[Color=Red]Score:[/Color] 1234");
            text.Color = new SKColor(255, 255, 255, 128);
            return text;
        }

        using SKBitmap layer = Render(Create(), forceLayer: true);
        using SKBitmap cached = Render(Create(), forceLayer: false);

        (int max, double mean) = Compare(layer, cached);
        max.ShouldBeLessThanOrEqualTo(40);
        mean.ShouldBeLessThan(0.5);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    [InlineData(20)]
    public void Draw_TextDropshadow_ReachesAsFarAsLayerPath(float blur)
    {
        using SKBitmap layer = Render(CreateText(blur, 0, 0), forceLayer: true);
        using SKBitmap cached = Render(CreateText(blur, 0, 0), forceLayer: false);

        int row = 50;
        ReachLeft(cached, row).ShouldBeInRange(ReachLeft(layer, row) - 1, ReachLeft(layer, row) + 1);
    }

    [Theory]
    [InlineData(0, 4, 4, 0f)]
    [InlineData(8, 4, 4, 0f)]
    [InlineData(8, 4, 4, 15f)]
    public void Draw_TextDropshadowWithEqualBlur_NeverUsesAnOffscreenLayer(float blur, float offsetX, float offsetY, float degrees)
    {
        TextRuntime text = CreateText(blur, offsetX, offsetY);
        text.Rotation = degrees;
        using SKBitmap bitmap = Render(text, forceLayer: false, frames: 1);
        ((Text)text.RenderableComponent!).LayerShadowDrawCount.ShouldBe(0);
    }

    [Fact]
    public void Draw_TextDropshadowWithUnequalBlur_UsesTheLayerPath()
    {
        TextRuntime text = CreateText(8, 4, 4);
        text.DropshadowBlurY = 2;
        using SKBitmap bitmap = Render(text, forceLayer: false);
        ((Text)text.RenderableComponent!).LayerShadowDrawCount.ShouldBeGreaterThan(0);
    }

    // The settled (cached mask) draw must match the forced layer draw, and must actually draw a shadow.
    private static void AssertSettledMatchesLayerPath(Func<TextRuntime> create, float canvasScale = 1, double minShadowMean = 0.02, bool shadowPixelsOnly = false, int maxDifference = 40)
    {
        using SKBitmap layer = Render(create(), forceLayer: true, canvasScale: canvasScale);
        TextRuntime cachedText = create();
        using SKBitmap cached = Render(cachedText, forceLayer: false, canvasScale: canvasScale);
        ((Text)cachedText.RenderableComponent!).LayerShadowDrawCount.ShouldBe(0);
        TextRuntime noShadowText = create();
        noShadowText.HasDropshadow = false;
        using SKBitmap noShadow = Render(noShadowText, forceLayer: false, canvasScale: canvasScale);

        Compare(noShadow, layer).MeanDifference.ShouldBeGreaterThan(minShadowMean, "the layer path should draw a shadow");
        (int max, double mean) = Compare(layer, cached, shadowPixelsOnly ? noShadow : null);
        max.ShouldBeLessThanOrEqualTo(maxDifference);
        mean.ShouldBeLessThan(0.5);
    }

    private static TextRuntime CreateBoxed(HorizontalAlignment horizontal, VerticalAlignment vertical = VerticalAlignment.Top,
        string markup = "Score: 1234", float boxWidth = 200, float boxHeight = 60, float blur = 6)
    {
        TextRuntime text = CreateText(blur, 4, 4, markup);
        text.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        text.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        text.Width = boxWidth;
        text.Height = boxHeight;
        text.HorizontalAlignment = horizontal;
        text.VerticalAlignment = vertical;
        return text;
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void Draw_TextDropshadowInWiderBox_MatchesLayerPath(HorizontalAlignment horizontal)
    {
        AssertSettledMatchesLayerPath(() => CreateBoxed(horizontal));
    }

    [Theory]
    [InlineData(VerticalAlignment.Center)]
    [InlineData(VerticalAlignment.Bottom)]
    public void Draw_TextDropshadowWithVerticalAlignment_MatchesLayerPath(VerticalAlignment vertical)
    {
        AssertSettledMatchesLayerPath(() => CreateBoxed(HorizontalAlignment.Center, vertical));
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void Draw_TextDropshadowMultiLine_MatchesLayerPath(HorizontalAlignment horizontal)
    {
        AssertSettledMatchesLayerPath(() => CreateBoxed(horizontal, markup: "Score: 1234\nPlayer one\nx", boxHeight: 90));
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void Draw_TextDropshadowWiderThanItsBox_MatchesLayerPath(HorizontalAlignment horizontal)
    {
        AssertSettledMatchesLayerPath(() => CreateBoxed(horizontal, markup: "Score: 1234 and some more words", boxWidth: 60));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(-20)]
    public void Draw_RotatedTextDropshadow_MatchesLayerPath(float degrees)
    {
        AssertSettledMatchesLayerPath(() =>
        {
            TextRuntime text = CreateText(6, 4, 4);
            text.Rotation = degrees;
            return text;
        }, shadowPixelsOnly: true); // the layer path resamples rotated glyph edges; the shadow is what is compared
    }

    [Theory]
    [InlineData(2f)]
    [InlineData(0.5f)]
    public void Draw_TextDropshadowOnScaledCanvas_MatchesLayerPath(float scale)
    {
        AssertSettledMatchesLayerPath(() => CreateText(6, 4, 4), scale);
    }

    [Fact]
    public void Draw_OutlinedTextDropshadow_MatchesLayerPath()
    {
        AssertSettledMatchesLayerPath(() =>
        {
            TextRuntime text = CreateText(6, 4, 4);
            text.OutlineThickness = 2;
            text.OutlineColor = SKColors.Blue;
            return text;
        });
    }

    [Theory]
    [InlineData(10, 1)]
    [InlineData(10, 8)]
    [InlineData(20, 2)]
    public void Draw_SmallAndTranslucentTextDropshadow_MatchesLayerPath(float fontSize, float blur)
    {
        AssertSettledMatchesLayerPath(() =>
        {
            TextRuntime text = CreateText(blur, 3, 3, "[Color=Red]Score:[/Color] 1234", fontSize);
            text.Color = new SKColor(255, 255, 255, 128);
            return text;
        });
    }

    public static IEnumerable<object?[]> Blends() =>
        new[] { new object?[] { null } }.Concat(Enum.GetValues<Gum.RenderingLibrary.Blend>().Select(b => new object?[] { b }));

    // A project load assigns Blend = Normal explicitly (#7914), so a path that bailed out on any Blend never ran there.
    [Theory]
    [MemberData(nameof(Blends))]
    public void Draw_TextDropshadowWithBlend_MatchesLayerPath(Gum.RenderingLibrary.Blend? blend)
    {
        AssertSettledMatchesLayerPath(() =>
        {
            TextRuntime text = CreateBoxed(HorizontalAlignment.Left);
            text.DropshadowAlpha = 181;
            text.DropshadowColor = new SKColor(255, 255, 255, 181);
            ((Text)text.RenderableComponent).Blend = blend;
            return text;
        });
    }

    // The alpha-erasing blends clip to the measured text (not the box), with or without a shadow, as before the mask existed.
    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha, HorizontalAlignment.Left, false)]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha, HorizontalAlignment.Center, false)]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha, HorizontalAlignment.Right, false)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha, HorizontalAlignment.Left, true)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha, HorizontalAlignment.Center, true)]
    [InlineData(Gum.RenderingLibrary.Blend.Replace, HorizontalAlignment.Left, false)]
    [InlineData(Gum.RenderingLibrary.Blend.Replace, HorizontalAlignment.Right, true)]
    public void Draw_AlphaWritingBlend_LeavesTheBoxBeyondTheMeasuredTextAlone(Gum.RenderingLibrary.Blend blend, HorizontalAlignment horizontal, bool shadow)
    {
        TextRuntime text = CreateBoxed(horizontal);
        text.HasDropshadow = shadow;
        ((Text)text.RenderableComponent).Blend = blend;
        using SKBitmap bitmap = Render(text, forceLayer: false);

        // Box spans x 40..240; the measured text is about 110 wide, so x 235 is outside the blend clip.
        bitmap.GetPixel(235, 50).Alpha.ShouldBe((byte)255);
    }

    [Theory]
    [InlineData(1, -6, 3, 0.5f, HorizontalAlignment.Right, false)]
    [InlineData(1, 0, 0.75f, 1f, HorizontalAlignment.Right, true)]
    [InlineData(20, 4, 4, 1f, HorizontalAlignment.Left, false)]
    public void Draw_SubtractAlphaTextDropshadow_MatchesLayerPath(float blur, float offsetX, float offsetY, float scale, HorizontalAlignment horizontal, bool outline)
    {
        AssertSettledMatchesLayerPath(() =>
        {
            TextRuntime text = CreateBoxed(horizontal, blur: blur);
            text.DropshadowOffsetX = offsetX;
            text.DropshadowOffsetY = offsetY;
            text.DropshadowColor = new SKColor(255, 255, 255, 181);
            if (outline)
            {
                text.OutlineThickness = 2;
            }
            ((Text)text.RenderableComponent).Blend = Gum.RenderingLibrary.Blend.SubtractAlpha;
            return text;
        }, scale, minShadowMean: 0.001, maxDifference: blur * scale < 1 ? 55 : 40); // a sub-pixel blur differs most between the two blur kinds
    }
}
