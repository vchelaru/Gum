using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaSharp;

namespace SkiaGum.Tests.GoldenImages;

/// <summary>
/// Pixel-level proof that a <c>ContainerRuntime.IsRenderTarget</c> subtree bakes to an offscreen
/// surface and composites back honoring group alpha (#3988). Two overlapping opaque rectangles under
/// a render target at <c>Alpha = 128</c> must flatten into one group and fade together — the overlap
/// stays a solid 50% blend rather than double-darkening, which only happens if the group is baked
/// once and tinted on the single composite blit.
/// </summary>
public class RenderTargetGoldenImageTests
{
    public RenderTargetGoldenImageTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void RenderTargetGroupAlpha_MatchesGoldenBaseline()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        surface.Canvas.Clear(SKColors.Black);

        ContainerRuntime renderTarget = new()
        {
            X = 8,
            Y = 8,
            Width = 48,
            Height = 48,
            IsRenderTarget = true,
            Alpha = 128,
        };
        renderTarget.Children.Add(new RectangleRuntime
        {
            X = 4,
            Y = 4,
            Width = 28,
            Height = 28,
            IsFilled = true,
            FillColor = SKColors.Red,
        });
        renderTarget.Children.Add(new RectangleRuntime
        {
            X = 16,
            Y = 16,
            Width = 28,
            Height = 28,
            IsFilled = true,
            FillColor = SKColors.Lime,
        });
        GumService.Default.Root.Children.Add(renderTarget);

        GumService.Default.Draw();

        GoldenImageAssert.Matches(surface, "RenderTarget_GroupAlpha");
    }
}
