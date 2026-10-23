using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;
using static SkiaGum.Tests.Renderer.PixelCoverage;

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

    // A ClipsChildren container clips to its own rectangle as it is drawn, turned with the container
    // and its parents (#6604). Each scene is compared against the same scene without clipping whose
    // overflowing children are cut to the part inside the container's local rectangle by hand.
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(30f, 0f)]
    [InlineData(0f, 30f)]
    [InlineData(30f, -50f)]
    public void ClipsChildren_RotatedContainerOrParent_ClipsToTurnedRectangle(float containerRotation, float parentRotation)
    {
        GraphicalUiElement Build(bool clips)
        {
            ContainerRuntime parent = new() { X = 120, Y = 120, Width = 0, Height = 0, Rotation = parentRotation };
            ContainerRuntime container = new()
            {
                Width = 60, Height = 60, Rotation = containerRotation, ClipsChildren = clips,
            };
            // 50x20 at x 40 overflows the 60-wide container; only its first 20 pixels are inside.
            container.Children.Add(clips ? Rect(40, 10, 50, 20, SKColors.Red) : Rect(40, 10, 20, 20, SKColors.Red));
            parent.Children.Add(container);
            return parent;
        }

        using SKBitmap clipped = Render(ClipSceneSize, () => Build(clips: true));
        using SKBitmap expected = Render(ClipSceneSize, () => Build(clips: false));

        AssertSameCoverage(expected, clipped, IsRed, minimumCount: 300, ClipCenterTolerance, countTolerancePercent: 10);
    }

    [Fact]
    public void ClipsChildren_NestedRotatedContainers_EachClipsToItsTurnedRectangle()
    {
        GraphicalUiElement Build(bool clips)
        {
            ContainerRuntime outer = new()
            {
                X = 100, Y = 100, Width = 90, Height = 90, Rotation = 30, ClipsChildren = clips,
            };
            // 40 wide at x 70 overflows the 90-wide outer container by 20.
            outer.Children.Add(clips ? Rect(70, 60, 40, 20, SKColors.Blue) : Rect(70, 60, 20, 20, SKColors.Blue));
            ContainerRuntime inner = new()
            {
                X = 10, Y = 0, Width = 50, Height = 50, Rotation = 20, ClipsChildren = clips,
            };
            // 40 tall at y 30 overflows the 50-tall inner container by 20.
            inner.Children.Add(clips ? Rect(10, 30, 20, 40, SKColors.Red) : Rect(10, 30, 20, 20, SKColors.Red));
            outer.Children.Add(inner);
            return outer;
        }

        using SKBitmap clipped = Render(ClipSceneSize, () => Build(clips: true));
        using SKBitmap expected = Render(ClipSceneSize, () => Build(clips: false));

        AssertSameCoverage(expected, clipped, IsRed, minimumCount: 300, ClipCenterTolerance, countTolerancePercent: 10);
        AssertSameCoverage(expected, clipped, IsBlue, minimumCount: 300, ClipCenterTolerance, countTolerancePercent: 10);
    }

    // Text applies its clipping ancestor's clip itself; under a rotated container that clip must
    // turn too, so text wholly inside the container draws exactly as with no clipping.
    [Theory]
    [InlineData(0f)]
    [InlineData(30f)]
    public void ClipsChildren_RotatedContainer_DoesNotCutTextInsideIt(float rotation)
    {
        GraphicalUiElement Build(bool clips)
        {
            ContainerRuntime container = new()
            {
                X = 100, Y = 120, Width = 80, Height = 60, Rotation = rotation, ClipsChildren = clips,
            };
            container.Children.Add(new TextRuntime { X = 30, Y = 0, Text = "WW", Color = SKColors.White });
            return container;
        }

        using SKBitmap clipped = Render(ClipSceneSize, () => Build(clips: true));
        using SKBitmap expected = Render(ClipSceneSize, () => Build(clips: false));

        AssertSameCoverage(expected, clipped, IsWhite, minimumCount: 30, centerTolerance: 0.25f, countTolerancePercent: 3);
    }

    // Turned 90 degrees, a container placed below the screen swings up onto it; it is drawn, not
    // culled by where its unturned rectangle would be.
    [Fact]
    public void ClipsChildren_RotatedOntoScreen_IsNotCulled()
    {
        GraphicalUiElement Build(bool clips)
        {
            ContainerRuntime container = new()
            {
                X = 10, Y = ClipSceneSize + 10, Width = 60, Height = 60, Rotation = 90, ClipsChildren = clips,
            };
            container.Children.Add(Rect(0, 0, 60, 60, SKColors.Red));
            return container;
        }

        using SKBitmap clipped = Render(ClipSceneSize, () => Build(clips: true));
        using SKBitmap expected = Render(ClipSceneSize, () => Build(clips: false));

        AssertSameCoverage(expected, clipped, IsRed, minimumCount: 1500, ClipCenterTolerance, countTolerancePercent: 3);
    }

    static bool IsWhite(SKColor c) => c.Red > 100 && c.Green > 100 && c.Blue > 100;

    const int ClipSceneSize = 240;
    const float ClipCenterTolerance = 0.5f;
}
