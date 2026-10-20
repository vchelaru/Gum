using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Gum.Wireframe;
using Shouldly;
using SkiaGum;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies that <see cref="Gum.RenderingLibrary.Blend"/> set on a Skia renderable is honored
/// at paint construction time as <see cref="SKBlendMode"/>. Before this wiring, the property
/// was stored on the renderable but never read, so e.g. setting <c>Blend = Additive</c>
/// silently produced normal alpha blending.
/// </summary>
public class BlendTests
{
    [Fact]
    public void RenderableShapeBase_Blend_DefaultsToNull()
    {
        TestableSprite sut = new();

        sut.Blend.ShouldBeNull();
    }

    [Fact]
    public void Sprite_GetPaint_NullBlend_LeavesDefaultSrcOver()
    {
        TestableSprite sut = new();
        sut.Blend = null;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.SrcOver);
    }

    [Fact]
    public void Sprite_GetPaint_Normal_MapsToSrcOver()
    {
        TestableSprite sut = new();
        sut.Blend = Blend.Normal;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.SrcOver);
    }

    [Fact]
    public void Sprite_GetPaint_Additive_MapsToPlus()
    {
        TestableSprite sut = new();
        sut.Blend = Blend.Additive;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.Plus);
    }

    [Fact]
    public void Sprite_GetPaint_Replace_MapsToSrc()
    {
        TestableSprite sut = new();
        sut.Blend = Blend.Replace;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.Src);
    }

    [Fact]
    public void Sprite_GetPaint_SubtractAlpha_MapsToDstOut()
    {
        TestableSprite sut = new();
        sut.Blend = Blend.SubtractAlpha;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.DstOut);
    }

    // ReplaceAlpha has no single SKBlendMode (alpha becomes the source alpha and the destination
    // color is scaled by it), so the paint carries a custom blender instead of a blend mode.
    [Fact]
    public void ApplyBlend_ReplaceAlpha_SetsCustomBlender()
    {
        using SKPaint paint = new();

        paint.ApplyBlend(Blend.ReplaceAlpha);

        paint.Blender.ShouldNotBeNull();
    }

    [Fact]
    public void ApplyBlend_MinAlpha_SetsDstIn()
    {
        using SKPaint paint = new();

        paint.ApplyBlend(Blend.MinAlpha);

        paint.BlendMode.ShouldBe(SKBlendMode.DstIn);
    }

    [Fact]
    public void Sprite_GetPaint_ReplaceAlpha_UsesCustomBlender()
    {
        TestableSprite sut = new();
        sut.Blend = Blend.ReplaceAlpha;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.Blender.ShouldNotBeNull();
    }

    // A circle applies ReplaceAlpha to its whole bounds, which Render does with a layer, so the
    // circle's own paint stays a plain draw.
    [Fact]
    public void Circle_GetPaint_ReplaceAlpha_LeavesPaintUnblended()
    {
        TestableCircle sut = new();
        sut.Blend = Blend.ReplaceAlpha;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.Blender.ShouldBeNull();
        paint.BlendMode.ShouldBe(SKBlendMode.SrcOver);
    }

    // The mask use: inside a render-target container, a MinAlpha shape erases what is under it
    // where the shape is transparent and leaves it where the shape is opaque (GameUiSamples' mana orb).
    [Fact]
    public void MinAlphaShape_InRenderTargetContainer_MasksPixelsBelowIt()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(40, 20));
        Gum.GumService.Default.Initialize(surface.Canvas, 40, 20);
        ContainerRuntime renderTarget = new() { Width = 40, Height = 20, IsRenderTarget = true };
        renderTarget.Children.Add(new RectangleRuntime
        {
            Width = 40, Height = 20, IsFilled = true, FillColor = SKColors.Red,
        });
        RectangleRuntime transparentMask = new()
        {
            X = 0, Width = 20, Height = 20, IsFilled = true, FillColor = new SKColor(255, 255, 255, 0),
        };
        transparentMask.SetProperty(nameof(RenderableShapeBase.Blend), Blend.MinAlpha);
        renderTarget.Children.Add(transparentMask);
        RectangleRuntime opaqueMask = new()
        {
            X = 20, Width = 20, Height = 20, IsFilled = true, FillColor = SKColors.White,
        };
        opaqueMask.SetProperty(nameof(RenderableShapeBase.Blend), Blend.MinAlpha);
        renderTarget.Children.Add(opaqueMask);
        Gum.GumService.Default.Root.Children.Add(renderTarget);

        Gum.GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        bitmap.GetPixel(10, 10).Alpha.ShouldBe((byte)0);
        bitmap.GetPixel(30, 10).ShouldBe(SKColors.Red);
    }

    // ReplaceAlpha inside a render-target container writes the source alpha over what is below and
    // scales the destination color by it (MonoGame: BlendState.ReplaceAlphaOnPremultipliedTarget).
    // A 50% white mask over opaque red leaves red at 50% alpha, not a pink opaque pixel.
    [Fact]
    public void ReplaceAlphaShape_InRenderTargetContainer_OverOpaque_KeepsColorAtSourceAlpha()
    {
        using SKBitmap bitmap = RenderReplaceAlphaMask(fillBelow: true);

        SKColor masked = bitmap.GetPixel(10, 10);
        ((int)masked.Red).ShouldBeInRange(255 - 2, 255 + 2);
        ((int)masked.Green).ShouldBeInRange(0 - 2, 0 + 2);
        ((int)masked.Blue).ShouldBeInRange(0 - 2, 0 + 2);
        ((int)masked.Alpha).ShouldBeInRange(128 - 2, 128 + 2);
        bitmap.GetPixel(30, 10).ShouldBe(SKColors.Red);
    }

    // Over empty pixels the alpha is still replaced (the destination color is zero, so the pixel is
    // black at the source alpha). DstIn would leave these pixels transparent.
    [Fact]
    public void ReplaceAlphaShape_InRenderTargetContainer_OverTransparent_WritesSourceAlpha()
    {
        using SKBitmap bitmap = RenderReplaceAlphaMask(fillBelow: false);

        SKColor masked = bitmap.GetPixel(10, 10);
        ((int)masked.Red).ShouldBeInRange(0 - 2, 0 + 2);
        ((int)masked.Green).ShouldBeInRange(0 - 2, 0 + 2);
        ((int)masked.Blue).ShouldBeInRange(0 - 2, 0 + 2);
        ((int)masked.Alpha).ShouldBeInRange(128 - 2, 128 + 2);
        bitmap.GetPixel(30, 10).Alpha.ShouldBe((byte)0);
    }

    // Text composites through a layer; it must not reach past the text, or ReplaceAlpha would erase
    // everything else in the clip.
    [Fact]
    public void ReplaceAlphaText_InRenderTargetContainer_LeavesPixelsOutsideTheTextAlone()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(120, 60));
        Gum.GumService.Default.Initialize(surface.Canvas, 120, 60);
        ContainerRuntime renderTarget = new() { Width = 120, Height = 60, IsRenderTarget = true };
        renderTarget.Children.Add(new RectangleRuntime
        {
            Width = 120, Height = 60, IsFilled = true, FillColor = SKColors.Red,
        });
        TextRuntime text = new() { X = 0, Y = 0, Text = "Hi", Blend = Blend.ReplaceAlpha };
        renderTarget.Children.Add(text);
        Gum.GumService.Default.Root.Children.Add(renderTarget);

        Gum.GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        bitmap.GetPixel(110, 50).ShouldBe(SKColors.Red);
    }

    // The rule MonoGame text follows too (MonoGameGum.IntegrationTests' TextAlphaBoundsMaskTests): the
    // blend changes the measured text block plus 2 pixels on every side, and nothing outside it.
    [Theory]
    [InlineData(Blend.ReplaceAlpha)]
    [InlineData(Blend.MinAlpha)]
    public void AlphaOnlyText_InRenderTargetContainer_ChangesItsMeasuredBlockPlusTwoPixels(Blend blend)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 80));
        Gum.GumService.Default.Initialize(surface.Canvas, 200, 80);
        ContainerRuntime renderTarget = new() { Width = 200, Height = 80, IsRenderTarget = true };
        renderTarget.Children.Add(new RectangleRuntime
        {
            Width = 200, Height = 80, IsFilled = true, StrokeWidth = 0, FillColor = SKColors.Red,
        });
        TextRuntime text = new()
        {
            X = 20, Y = 20, Text = "HOUZ 0123\nNLM", Blend = blend,
            WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren,
            HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren,
        };
        renderTarget.Children.Add(text);
        Gum.GumService.Default.Root.Children.Add(renderTarget);

        Gum.GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) != SKColors.Red)
                {
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x + 1);
                    bottom = Math.Max(bottom, y + 1);
                }
            }
        }

        float width = text.GetAbsoluteWidth();
        float height = text.GetAbsoluteHeight();
        ((float)left).ShouldBe(18, 1, "left");
        ((float)top).ShouldBe(18, 1, "top");
        ((float)(right - left)).ShouldBe(width + 4, 1, $"width, text measured {width}");
        ((float)(bottom - top)).ShouldBe(height + 4, 1, $"height, text measured {height}");
    }

    // The same scene and expected pixels as MonoGameGum.IntegrationTests' AlphaBlendSceneTests, so the
    // two runtimes stay in step. Alpha-only blends apply to a shape's whole bounds: the circle's
    // corner (40, 52) is erased as well as the circle itself.
    [Fact]
    public void ReplaceAlpha_Scene_MatchesMonoGamePixels()
    {
        using SKBitmap bitmap = RenderBlendScene(Blend.ReplaceAlpha);

        AssertPixel(bitmap, 20, 20, 135, 50, 60);
        AssertPixel(bitmap, 50, 60, 135, 50, 60);
        AssertPixel(bitmap, 70, 70, 35, 35, 45);
        AssertPixel(bitmap, 40, 52, 70, 70, 90);
        AssertPixel(bitmap, 5, 5, 200, 30, 30);
        AssertPixel(bitmap, 90, 10, 70, 70, 90);
    }

    [Fact]
    public void MinAlpha_Scene_MatchesMonoGamePixels()
    {
        using SKBitmap bitmap = RenderBlendScene(Blend.MinAlpha);

        AssertPixel(bitmap, 20, 20, 135, 50, 60);
        AssertPixel(bitmap, 50, 60, 135, 50, 60);
        AssertPixel(bitmap, 70, 70, 70, 70, 90);
        AssertPixel(bitmap, 40, 52, 70, 70, 90);
        AssertPixel(bitmap, 5, 5, 200, 30, 30);
        AssertPixel(bitmap, 90, 10, 70, 70, 90);
    }

    // MonoGame's bounds mask covers circles, rounded rectangles and arcs only. A line replaces alpha
    // only where it draws, so the rest of its bounds is left alone.
    [Fact]
    public void ReplaceAlphaLine_InRenderTargetContainer_OnlyChangesPixelsItDraws()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(40, 40));
        Gum.GumService.Default.Initialize(surface.Canvas, 40, 40);
        ContainerRuntime renderTarget = new() { Width = 40, Height = 40, IsRenderTarget = true };
        renderTarget.Children.Add(new RectangleRuntime
        {
            Width = 40, Height = 40, IsFilled = true, StrokeWidth = 0, FillColor = SKColors.Red,
        });
        LineRuntime line = new() { X = 0, Y = 0, Width = 40, Height = 40, StrokeWidth = 4 };
#pragma warning disable CS0618 // Color is obsolete; the line has no FillColor yet.
        line.Color = new SKColor(255, 255, 255, 128);
#pragma warning restore CS0618
        line.SetProperty(nameof(RenderableShapeBase.Blend), Blend.ReplaceAlpha);
        renderTarget.Children.Add(line);
        Gum.GumService.Default.Root.Children.Add(renderTarget);

        Gum.GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        bitmap.GetPixel(30, 5).ShouldBe(SKColors.Red);
        bitmap.GetPixel(20, 20).Alpha.ShouldBeInRange((byte)120, (byte)136);
    }

    private static void AssertPixel(SKBitmap bitmap, int x, int y, int red, int green, int blue)
    {
        SKColor actual = bitmap.GetPixel(x, y);
        ((int)actual.Red).ShouldBeInRange(red - 2, red + 2, $"red at ({x}, {y})");
        ((int)actual.Green).ShouldBeInRange(green - 2, green + 2, $"green at ({x}, {y})");
        ((int)actual.Blue).ShouldBeInRange(blue - 2, blue + 2, $"blue at ({x}, {y})");
    }

    private static SKBitmap RenderBlendScene(Blend blend)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(100, 100));
        Gum.GumService.Default.Initialize(surface.Canvas, 100, 100);
        ContainerRuntime root = new() { Width = 100, Height = 100 };
        root.Children.Add(new RectangleRuntime
        {
            Width = 100, Height = 100, IsFilled = true, StrokeWidth = 0, FillColor = new SKColor(70, 70, 90),
        });
        ContainerRuntime renderTarget = new() { Width = 100, Height = 100, IsRenderTarget = true };
        renderTarget.Children.Add(new RectangleRuntime
        {
            Width = 55, Height = 100, IsFilled = true, StrokeWidth = 0, FillColor = new SKColor(200, 30, 30),
        });
        RectangleRuntime square = new()
        {
            X = 8, Y = 8, Width = 35, Height = 35, IsFilled = true, StrokeWidth = 0, FillColor = new SKColor(255, 255, 255, 128),
        };
        square.SetProperty(nameof(RenderableShapeBase.Blend), blend);
        renderTarget.Children.Add(square);
        CircleRuntime circle = new()
        {
            X = 38, Y = 50, Width = 45, Height = 45, IsFilled = true, StrokeWidth = 0, FillColor = new SKColor(255, 255, 255, 128),
        };
        circle.SetProperty(nameof(RenderableShapeBase.Blend), blend);
        renderTarget.Children.Add(circle);
        root.Children.Add(renderTarget);
        Gum.GumService.Default.Root.Children.Add(root);

        Gum.GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    private static SKBitmap RenderReplaceAlphaMask(bool fillBelow)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(40, 20));
        Gum.GumService.Default.Initialize(surface.Canvas, 40, 20);
        ContainerRuntime renderTarget = new() { Width = 40, Height = 20, IsRenderTarget = true };
        if (fillBelow)
        {
            renderTarget.Children.Add(new RectangleRuntime
            {
                Width = 40, Height = 20, IsFilled = true, FillColor = SKColors.Red,
            });
        }

        RectangleRuntime mask = new()
        {
            X = 0, Width = 20, Height = 20, IsFilled = true, FillColor = new SKColor(255, 255, 255, 128),
        };
        mask.SetProperty(nameof(RenderableShapeBase.Blend), Blend.ReplaceAlpha);
        renderTarget.Children.Add(mask);
        Gum.GumService.Default.Root.Children.Add(renderTarget);

        Gum.GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    // Blend lives on RenderableShapeBase so every shape derivative gets it, not just Sprite/NineSlice.
    // Spot-check NineSlice and a non-texture shape (Circle) to lock that in.
    [Fact]
    public void NineSlice_GetPaint_Additive_MapsToPlus()
    {
        TestableNineSlice sut = new();
        sut.Blend = Blend.Additive;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.Plus);
    }

    [Fact]
    public void Circle_GetPaint_Additive_MapsToPlus()
    {
        TestableCircle sut = new();
        sut.Blend = Blend.Additive;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.Plus);
    }

    // Regression tests for the .gumx loading path that bit on first sample run:
    // a Standards/RoundedRectangle.gutx (and friends) include a "Blend" default variable
    // typed as the Gum.RenderingLibrary.Blend enum. The .gumx loader calls SetProperty with
    // the unboxed non-nullable enum value, which falls through to CustomSetPropertyOnRenderable.
    // Before this branch, that path landed on GraphicalUiElement.SetPropertyThroughReflection,
    // which calls Convert.ChangeType(enumValue, typeof(Blend?)) and throws
    // InvalidCastException because Convert.ChangeType doesn't support Nullable<T>.
    // Direct-property unit tests didn't cover this because they bypass the reflection path.
    [Fact]
    public void SetPropertyOnRenderable_BlendOnSprite_AssignsAdditive()
    {
        Sprite renderable = new();
        GraphicalUiElement gue = new(renderable);

        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
            renderable, gue, nameof(RenderableShapeBase.Blend), Blend.Additive);

        renderable.Blend.ShouldBe(Blend.Additive);
    }

    [Fact]
    public void SetPropertyOnRenderable_BlendOnNineSlice_AssignsAdditive()
    {
        NineSlice renderable = new();
        GraphicalUiElement gue = new(renderable);

        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
            renderable, gue, nameof(RenderableShapeBase.Blend), Blend.Additive);

        renderable.Blend.ShouldBe(Blend.Additive);
    }

    [Fact]
    public void SetPropertyOnRenderable_BlendOnRoundedRectangle_AssignsNormal()
    {
        // Exact reproducer for the stack trace in #2922 follow-up: Standards/RoundedRectangle.gutx
        // sets Blend = Normal as a default variable and crashed the SilkNetGum sample on load.
        RoundedRectangle renderable = new();
        GraphicalUiElement gue = new(renderable);

        Should.NotThrow(() => CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
            renderable, gue, nameof(RenderableShapeBase.Blend), Blend.Normal));

        renderable.Blend.ShouldBe(Blend.Normal);
    }

    // SolidRectangle (internal), LineRectangle, and LineGrid have no per-type branch in
    // CustomSetPropertyOnRenderable.SetPropertyOnRenderableFunc — they fell straight to the
    // reflection fallback and hit the same enum -> Nullable<enum> InvalidCastException as
    // the named-branch types. The user-reported crash was on SolidRectangle (the renderable
    // for SolidRectangleRuntime / the "ColoredRectangle" standard mapping in SystemManagers).
    // LineRectangle stands in for SolidRectangle in tests since SolidRectangle is internal;
    // both exercise the same RenderableShapeBase catch-all path.
    [Fact]
    public void SetPropertyOnRenderable_BlendOnLineRectangle_AssignsNormal()
    {
        LineRectangle renderable = new();
        GraphicalUiElement gue = new(renderable);

        Should.NotThrow(() => CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
            renderable, gue, nameof(RenderableShapeBase.Blend), Blend.Normal));

        renderable.Blend.ShouldBe(Blend.Normal);
    }

    [Fact]
    public void SetPropertyOnRenderable_BlendOnLineGrid_AssignsAdditive()
    {
        LineGrid renderable = new();
        GraphicalUiElement gue = new(renderable);

        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
            renderable, gue, nameof(RenderableShapeBase.Blend), Blend.Additive);

        renderable.Blend.ShouldBe(Blend.Additive);
    }

    [Fact]
    public void SetPropertyOnRenderable_BlendOnPolygon_AssignsAdditive()
    {
        Polygon renderable = new();
        GraphicalUiElement gue = new(renderable);

        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
            renderable, gue, nameof(RenderableShapeBase.Blend), Blend.Additive);

        renderable.Blend.ShouldBe(Blend.Additive);
    }

    private sealed class TestableSprite : Sprite
    {
        public SKPaint InvokeGetPaint(SKRect boundingRect, float absoluteRotation)
            => GetPaint(boundingRect, absoluteRotation);
    }

    private sealed class TestableNineSlice : NineSlice
    {
        public SKPaint InvokeGetPaint(SKRect boundingRect, float absoluteRotation)
            => GetPaint(boundingRect, absoluteRotation);
    }

    private sealed class TestableCircle : Circle
    {
        public SKPaint InvokeGetPaint(SKRect boundingRect, float absoluteRotation)
            => GetPaint(boundingRect, absoluteRotation);
    }
}
