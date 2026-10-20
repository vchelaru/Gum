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

    // ReplaceAlpha has no clean SkiaSharp equivalent — SKBlendMode has no separate per-channel
    // blend-factor/equation override the way raylib's rlgl does (see
    // Runtimes/RaylibGum/Renderables/BlendModeExtensions.cs, issue #3470), so it falls through to the
    // default SrcOver rather than picking a Skia mode that would silently change visuals.
    [Fact]
    public void Sprite_GetPaint_ReplaceAlpha_FallsThroughToSrcOver()
    {
        TestableSprite sut = new();
        sut.Blend = Blend.ReplaceAlpha;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.SrcOver);
    }

    [Fact]
    public void Sprite_GetPaint_MinAlpha_MapsToDstIn()
    {
        TestableSprite sut = new();
        sut.Blend = Blend.MinAlpha;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.DstIn);
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
