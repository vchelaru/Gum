using Shouldly;
using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Issue #7910: a Text drop shadow is a cached blurred mask drawn under the text instead of an offscreen layer with
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

    private static (int MaxDifference, double MeanDifference) Compare(SKBitmap a, SKBitmap b)
    {
        int max = 0;
        double sum = 0;
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                SKColor ca = a.GetPixel(x, y);
                SKColor cb = b.GetPixel(x, y);
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

    [Fact]
    public void Draw_TextDropshadow_BuildsTheMaskOnceAndReusesIt()
    {
        TextRuntime text = CreateText(8, 4, 4);
        using SKBitmap first = Render(text, forceLayer: false, frames: SettledFrames);
        Text renderable = (Text)text.RenderableComponent;
        renderable.HasCachedShadowMask.ShouldBeTrue();
    }

    [Fact]
    public void Draw_TextDropshadowWithUnequalBlur_UsesTheLayerPath()
    {
        TextRuntime text = CreateText(8, 4, 4);
        text.DropshadowBlurY = 2;
        using SKBitmap bitmap = Render(text, forceLayer: false);
        ((Text)text.RenderableComponent).HasCachedShadowMask.ShouldBeFalse();
    }

    // The settled (cached mask) draw must match the forced layer draw, and must actually draw a shadow.
    private static void AssertSettledMatchesLayerPath(Func<TextRuntime> create, float canvasScale = 1, bool expectMask = true)
    {
        using SKBitmap layer = Render(create(), forceLayer: true, canvasScale: canvasScale);
        TextRuntime cachedText = create();
        using SKBitmap cached = Render(cachedText, forceLayer: false, canvasScale: canvasScale);
        ((Text)cachedText.RenderableComponent).HasCachedShadowMask.ShouldBe(expectMask);
        TextRuntime noShadowText = create();
        noShadowText.HasDropshadow = false;
        using SKBitmap noShadow = Render(noShadowText, forceLayer: false, canvasScale: canvasScale);

        Compare(noShadow, layer).MeanDifference.ShouldBeGreaterThan(0.02, "the layer path should draw a shadow");
        (int max, double mean) = Compare(layer, cached);
        max.ShouldBeLessThanOrEqualTo(40);
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
        }, expectMask: false);
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

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void Draw_TextDropshadowInBox_SettlesIntoTheMask(HorizontalAlignment horizontal)
    {
        TextRuntime text = CreateBoxed(horizontal);
        using SKBitmap bitmap = Render(text, forceLayer: false);
        ((Text)text.RenderableComponent).HasCachedShadowMask.ShouldBeTrue();
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
        }, expectMask: blend is not (Gum.RenderingLibrary.Blend.ReplaceAlpha or Gum.RenderingLibrary.Blend.MinAlpha or Gum.RenderingLibrary.Blend.Replace));
    }

    [Fact]
    public void Draw_ProjectLoadedTextDropshadow_UsesTheCachedMaskEveryDrawOnceSettled()
    {
        TextRuntime text = CreateText(2, 0, 0.75f);
        Text renderable = (Text)text.RenderableComponent;
        renderable.Blend = Gum.RenderingLibrary.Blend.Normal;
        text.DropshadowAlpha = 181;

        using SKBitmap bitmap = Render(text, forceLayer: false, frames: 10);

        // The first draws earn the mask, every later one uses it.
        renderable.CachedShadowDrawCount.ShouldBeGreaterThanOrEqualTo(7);
    }
}
