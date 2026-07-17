using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// Regression coverage for issue #3735: a <c>ClipsChildren</c> container's clip only applied to
/// the first descendant rendered under it. <see cref="RenderingLibrary.Graphics.Renderer"/>'s
/// private <c>Draw</c> method called an unconditional <c>canvas.Restore()</c> at the end of
/// *every* recursive invocation -- including the trivial calls made for leaf nodes with no
/// children -- with no matching <c>canvas.Save()</c> anywhere in that call. The first sibling's
/// own (empty-children) recursive Draw() consumed that extra Restore(), popping the clip
/// container's still-open Save early, so every subsequent sibling rendered unclipped.
/// </summary>
public class RendererClippingTests
{
    public RendererClippingTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void SecondSiblingUnderClipContainer_IsClippedToContainerBounds()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        surface.Canvas.Clear(SKColors.Black);

        ContainerRuntime clipContainer = new()
        {
            X = 0,
            Y = 0,
            Width = 30,
            Height = 30,
            ClipsChildren = true,
        };
        GumService.Default.Root.Children.Add(clipContainer);

        RectangleRuntime firstSibling = new()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            IsFilled = true,
            FillColor = SKColors.Blue,
        };
        clipContainer.Children.Add(firstSibling);

        RectangleRuntime secondSibling = new()
        {
            X = 0,
            Y = 0,
            Width = 60,
            Height = 60,
            IsFilled = true,
            FillColor = SKColors.Red,
        };
        clipContainer.Children.Add(secondSibling);

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);

        // Inside the clip container's bounds: the second sibling's red should show through.
        bitmap.GetPixel(15, 15).ShouldBe(SKColors.Red);

        // Outside the clip container's bounds, but inside the second sibling's own (unclipped)
        // extent: must stay background -- it must not bleed past the container.
        bitmap.GetPixel(50, 50).ShouldBe(SKColors.Black);
    }
}
