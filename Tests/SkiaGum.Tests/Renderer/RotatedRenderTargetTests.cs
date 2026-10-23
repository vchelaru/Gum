using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;
using static SkiaGum.Tests.Renderer.PixelCoverage;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// A rotated render-target container must look like the same container drawn without a render
/// target: its children turn with it and nothing inside its own rectangle is clipped (#6588).
/// Text resamples differently when the baked image is turned, so scenes are compared by shape: how
/// many pixels each child covers and where their center is, not pixel for pixel. Shape centers
/// match within 0.1 pixel, so their tolerance catches a 1 pixel shift. Text drawn upright into the
/// bake and then turned rasterizes differently from text drawn turned, and glyph rasterization
/// differs by OS (about 1.5 pixels on macOS), so the text tolerance is 4 pixels: the shapes carry
/// the fine position check, and the text check still catches a double rotation, which moves the
/// text about 22 pixels.
/// </summary>
public class RotatedRenderTargetTests
{
    const int Size = 240;
    const float ShapeCenterTolerance = 0.25f;
    const float TextCenterTolerance = 4f;

    public RotatedRenderTargetTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(30f, 0f)]
    [InlineData(0f, 30f)]
    [InlineData(30f, -50f)]
    public void Draw_RenderTarget_MatchesSameContainerWithoutRenderTarget(float containerRotation, float parentRotation)
    {
        GraphicalUiElement Build(bool isRenderTarget)
        {
            ContainerRuntime parent = new() { X = 60, Y = 60, Width = 0, Height = 0, Rotation = parentRotation };
            ContainerRuntime container = new()
            {
                Width = 60,
                Height = 60,
                Rotation = containerRotation,
                IsRenderTarget = isRenderTarget,
            };
            container.Children.Add(Rect(40, 0, 20, 20, SKColors.Red));
            container.Children.Add(new TextRuntime { X = 0, Y = 20, Text = "WW", Color = SKColors.White });
            parent.Children.Add(container);
            return parent;
        }

        using SKBitmap plain = Render(() => Build(isRenderTarget: false));
        using SKBitmap viaRenderTarget = Render(() => Build(isRenderTarget: true));

        AssertSameCoverage(plain, viaRenderTarget, IsRed, minimumCount: 300, ShapeCenterTolerance, countTolerancePercent: 10);
        AssertSameCoverage(plain, viaRenderTarget, IsTextColored, minimumCount: 30, TextCenterTolerance, countTolerancePercent: 25);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Draw_NestedRotatedContainers_MatchWithoutRenderTargets(bool isOuterRenderTarget, bool isInnerRenderTarget)
    {
        GraphicalUiElement Build(bool useRenderTargets)
        {
            ContainerRuntime outer = new()
            {
                X = 100, Y = 100, Width = 90, Height = 90, Rotation = 30,
                IsRenderTarget = useRenderTargets && isOuterRenderTarget,
            };
            outer.Children.Add(Rect(0, 60, 20, 20, SKColors.Blue));
            ContainerRuntime inner = new()
            {
                X = 20, Y = 0, Width = 60, Height = 60, Rotation = 20,
                IsRenderTarget = useRenderTargets && isInnerRenderTarget,
            };
            inner.Children.Add(Rect(10, 20, 20, 20, SKColors.Red));
            outer.Children.Add(inner);
            return outer;
        }

        using SKBitmap plain = Render(() => Build(useRenderTargets: false));
        using SKBitmap viaRenderTarget = Render(() => Build(useRenderTargets: true));

        AssertSameCoverage(plain, viaRenderTarget, IsRed, minimumCount: 300, ShapeCenterTolerance, countTolerancePercent: 10);
        AssertSameCoverage(plain, viaRenderTarget, IsBlue, minimumCount: 300, ShapeCenterTolerance, countTolerancePercent: 10);
    }

    [Fact]
    public void Draw_RotatedRenderTargetWithCenterOrigin_MatchesWithoutRenderTarget()
    {
        GraphicalUiElement Build(bool isRenderTarget)
        {
            ContainerRuntime container = new()
            {
                X = 120, Y = 120, Width = 60, Height = 60, Rotation = 30, IsRenderTarget = isRenderTarget,
                XOrigin = HorizontalAlignment.Center,
                YOrigin = VerticalAlignment.Center,
            };
            container.Children.Add(Rect(40, 0, 20, 20, SKColors.Red));
            container.Children.Add(Rect(0, 40, 20, 20, SKColors.Blue));
            return container;
        }

        using SKBitmap plain = Render(() => Build(isRenderTarget: false));
        using SKBitmap viaRenderTarget = Render(() => Build(isRenderTarget: true));

        AssertSameCoverage(plain, viaRenderTarget, IsRed, minimumCount: 300, ShapeCenterTolerance, countTolerancePercent: 10);
        AssertSameCoverage(plain, viaRenderTarget, IsBlue, minimumCount: 300, ShapeCenterTolerance, countTolerancePercent: 10);
    }

    // A render target truncates a child that overflows it; turning the container keeps the same
    // truncated area (a 50x20 child at x 40 keeps only the part inside the 60x60 container).
    [Fact]
    public void Draw_RotatedRenderTarget_KeepsTheUnrotatedClippedArea()
    {
        GraphicalUiElement Build(float rotation)
        {
            ContainerRuntime container = new()
            {
                X = 100, Y = 100, Width = 60, Height = 60, Rotation = rotation, IsRenderTarget = true,
            };
            container.Children.Add(Rect(40, 10, 50, 20, SKColors.Red));
            return container;
        }

        using SKBitmap upright = Render(() => Build(rotation: 0));
        using SKBitmap turned = Render(() => Build(rotation: 30));

        // Untruncated, the child would cover about 1000 pixels.
        int uprightCount = Measure(upright, IsRed).Count;
        uprightCount.ShouldBeInRange(300, 450);
        Measure(turned, IsRed).Count.ShouldBeInRange(uprightCount * 9 / 10, uprightCount * 11 / 10);
    }

    // The corner rectangle of a container rotated 30 degrees counterclockwise lands above the
    // container's origin, outside its axis-aligned rectangle, with or without a render target.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Draw_RotatedContainer_PutsCornerChildAboveOrigin(bool isRenderTarget)
    {
        using SKBitmap bitmap = Render(() =>
        {
            ContainerRuntime container = new()
            {
                X = 60, Y = 60, Width = 60, Height = 60, Rotation = 30, IsRenderTarget = isRenderTarget,
            };
            container.Children.Add(Rect(40, 0, 20, 20, SKColors.Red));
            return container;
        });

        // Local (50, 10), the corner rectangle's center, turned 30 degrees counterclockwise about
        // the container origin (60, 60): (60 + 50cos30 + 10sin30, 60 - 50sin30 + 10cos30).
        bitmap.GetPixel(108, 44).ShouldBe(SKColors.Red);
    }

    static bool IsTextColored(SKColor c) => c.Red > 100 && c.Green > 100 && c.Blue > 100;

    static SKBitmap Render(System.Func<GraphicalUiElement> build) => PixelCoverage.Render(Size, build);
}
