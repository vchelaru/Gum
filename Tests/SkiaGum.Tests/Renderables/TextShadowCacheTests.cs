using Shouldly;
using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
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

    private static SKBitmap Render(TextRuntime text, bool forceLayer, int frames = SettledFrames)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(Width, Height));
        GumService.Default.Initialize(surface.Canvas, Width, Height);
        GumService.Default.Root.Children.Clear();
        ((Text)text.RenderableComponent).ForceLayerShadow = forceLayer;
        GumService.Default.Root.Children.Add(text);
        for (int i = 0; i < frames; i++)
        {
            surface.Canvas.Clear(new SKColor(128, 128, 128));
            GumService.Default.Draw();
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
}
