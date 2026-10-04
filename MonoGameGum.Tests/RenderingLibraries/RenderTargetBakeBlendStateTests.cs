using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Blend = Gum.Blend;
using BlendState = Gum.BlendState;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Pins <see cref="Renderer.AdjustBlendStateForRenderTargetBake"/>, the seam behind the #1696
/// render-target premultiplied-alpha fix. When baking a render target's children over a transparent
/// clear, an unconfigured child (whose blend resolves to <see cref="Renderer.NormalBlendState"/>)
/// gets a "premultiply on bake" blend so straight-alpha color composites correctly. But when the
/// whole pipeline is already premultiplied (<see cref="Renderer.NormalBlendState"/> ==
/// <c>AlphaBlend</c>, as FRB's GumIdb sets it), that substitution would premultiply an
/// already-premultiplied color a second time and darken it, so the ambient blend must be kept.
/// The pixel-level end-to-end result can't be unit-tested here because FRB premultiplies in a
/// custom shader that this harness doesn't run — so this pins the blend decision directly.
/// </summary>
public class RenderTargetBakeBlendStateTests : BaseTestClass
{
    [Fact]
    public void AdjustBlendStateForRenderTargetBake_KeepsAmbientBlend_WhenPremultipliedPipeline()
    {
        var previous = Renderer.NormalBlendState;
        try
        {
            Renderer.NormalBlendState = BlendState.AlphaBlend;

            var result = Renderer.AdjustBlendStateForRenderTargetBake(
                BlendState.AlphaBlend, isBakingRenderTarget: true);

            // No substitution: the premultiplied ambient blend already accumulates premultiplied
            // children correctly over the transparent clear.
            result.ShouldBeSameAs(BlendState.AlphaBlend);
        }
        finally
        {
            Renderer.NormalBlendState = previous;
        }
    }

    [Fact]
    public void AdjustBlendStateForRenderTargetBake_SubstitutesBakeBlend_WhenStraightAlphaPipeline()
    {
        var previous = Renderer.NormalBlendState;
        try
        {
            Renderer.NormalBlendState = BlendState.NonPremultiplied;

            var result = Renderer.AdjustBlendStateForRenderTargetBake(
                BlendState.NonPremultiplied, isBakingRenderTarget: true);

            // Straight-alpha content bakes with the premultiply-on-bake blend: color uses
            // SourceAlpha (premultiply), alpha uses One (so alpha isn't squared over transparent).
            result.ShouldNotBeSameAs(BlendState.NonPremultiplied);
            result.ColorSourceBlend.ShouldBe(Blend.SourceAlpha);
            result.AlphaSourceBlend.ShouldBe(Blend.One);
        }
        finally
        {
            Renderer.NormalBlendState = previous;
        }
    }

    // Pins the #4091 fix, whose swap target #5682 replaced: MinAlphaPremultiplied's color Min was a
    // no-op against a white mask, so partially transparent masks still leaked color. MinAlpha (e.g. GameUiSamples' ManaOrb wave mask) deliberately leaves
    // color untouched (ColorSourceBlend=Zero, ColorDestinationBlend=One) and only clips alpha via
    // Min. Baked under _bakeToRenderTargetBlendState's premultiply-on-bake convention, that leaves
    // a masked-out pixel's color premultiplied against its OLD (pre-mask) alpha instead of its new
    // (zero) alpha — DrawRenderTargetToScreen's premultiplied composite-back blit then bleeds that
    // leftover color through instead of treating the pixel as transparent. MinAlphaPremultiplied's
    // Min-for-color-too formula drops the leftover color to whatever the mask's own
    // (premultiplied-authored) texture color contributes instead.
    //
    // A round-trip through SpriteRuntime.Blend (Gum enum -> XNA BlendState -> back to Gum
    // BlendState) does NOT preserve reference identity for anything but the four core presets
    // (Opaque/AlphaBlend/Additive/NonPremultiplied) — MinAlpha loses its singleton reference and
    // arrives here as a field-identical but distinct instance. So this constructs a fresh
    // Gum.BlendState with MinAlpha's exact field values rather than referencing the static
    // BlendState.MinAlpha, to pin the fix against structural (not reference) comparison.
    [Fact]
    public void AdjustBlendStateForRenderTargetBake_SubstitutesScaledDestinationColor_ForFieldEquivalentMinAlpha_WhenStraightAlphaPipeline()
    {
        var previous = Renderer.NormalBlendState;
        try
        {
            Renderer.NormalBlendState = BlendState.NonPremultiplied;

            var fieldEquivalentMinAlpha = new BlendState
            {
                ColorSourceBlend = BlendState.MinAlpha.ColorSourceBlend,
                ColorBlendFunction = BlendState.MinAlpha.ColorBlendFunction,
                ColorDestinationBlend = BlendState.MinAlpha.ColorDestinationBlend,
                AlphaSourceBlend = BlendState.MinAlpha.AlphaSourceBlend,
                AlphaBlendFunction = BlendState.MinAlpha.AlphaBlendFunction,
                AlphaDestinationBlend = BlendState.MinAlpha.AlphaDestinationBlend,
            };
            fieldEquivalentMinAlpha.ShouldNotBeSameAs(BlendState.MinAlpha);

            var result = Renderer.AdjustBlendStateForRenderTargetBake(
                fieldEquivalentMinAlpha, isBakingRenderTarget: true);

            result.ShouldBeSameAs(BlendState.MinAlphaOnPremultipliedTarget);
        }
        finally
        {
            Renderer.NormalBlendState = previous;
        }
    }

    // #5671: SubtractAlpha keeps destination color while lowering alpha, so the baked texture's
    // premultiplied color stays too bright against its new alpha and the composite-back blit shows
    // it instead of a hole. Same structural-match rule as MinAlpha above (Sprite.Blend round-trips
    // through XNA and loses the singleton reference).
    [Fact]
    public void AdjustBlendStateForRenderTargetBake_SubstitutesDestinationOut_ForFieldEquivalentSubtractAlpha_WhenStraightAlphaPipeline()
    {
        var previous = Renderer.NormalBlendState;
        try
        {
            Renderer.NormalBlendState = BlendState.NonPremultiplied;

            var fieldEquivalentSubtractAlpha = new BlendState
            {
                ColorSourceBlend = BlendState.SubtractAlpha.ColorSourceBlend,
                ColorBlendFunction = BlendState.SubtractAlpha.ColorBlendFunction,
                ColorDestinationBlend = BlendState.SubtractAlpha.ColorDestinationBlend,
                AlphaSourceBlend = BlendState.SubtractAlpha.AlphaSourceBlend,
                AlphaBlendFunction = BlendState.SubtractAlpha.AlphaBlendFunction,
                AlphaDestinationBlend = BlendState.SubtractAlpha.AlphaDestinationBlend,
            };
            fieldEquivalentSubtractAlpha.ShouldNotBeSameAs(BlendState.SubtractAlpha);

            var result = Renderer.AdjustBlendStateForRenderTargetBake(
                fieldEquivalentSubtractAlpha, isBakingRenderTarget: true);

            result.ShouldBeSameAs(BlendState.SubtractAlphaFromPremultipliedTarget);
            result.ColorSourceBlend.ShouldBe(Blend.Zero);
            result.ColorDestinationBlend.ShouldBe(Blend.InverseSourceAlpha);
            result.AlphaSourceBlend.ShouldBe(Blend.Zero);
            result.AlphaDestinationBlend.ShouldBe(Blend.InverseSourceAlpha);
        }
        finally
        {
            Renderer.NormalBlendState = previous;
        }
    }

    [Fact]
    public void AdjustBlendStateForRenderTargetBake_KeepsSubtractAlpha_WhenNotBakingOrPremultipliedPipeline()
    {
        var previous = Renderer.NormalBlendState;
        try
        {
            Renderer.NormalBlendState = BlendState.NonPremultiplied;
            Renderer.AdjustBlendStateForRenderTargetBake(BlendState.SubtractAlpha, isBakingRenderTarget: false)
                .ShouldBeSameAs(BlendState.SubtractAlpha);

            Renderer.NormalBlendState = BlendState.AlphaBlend;
            Renderer.AdjustBlendStateForRenderTargetBake(BlendState.SubtractAlpha, isBakingRenderTarget: true)
                .ShouldBeSameAs(BlendState.SubtractAlpha);
        }
        finally
        {
            Renderer.NormalBlendState = previous;
        }
    }

    // #5673/#5682: ReplaceAlpha and MinAlpha keep destination color while rewriting alpha, so the
    // baked premultiplied color goes inconsistent with its new alpha. While baking they scale
    // destination color by source alpha instead. Field-equivalent copies pin the structural match.
    [Fact]
    public void AdjustBlendStateForRenderTargetBake_SubstitutesScaledDestinationColor_ForFieldEquivalentReplaceAndMinAlpha()
    {
        var previous = Renderer.NormalBlendState;
        try
        {
            Renderer.NormalBlendState = BlendState.NonPremultiplied;

            Renderer.AdjustBlendStateForRenderTargetBake(CopyOf(BlendState.ReplaceAlpha), isBakingRenderTarget: true)
                .ShouldBeSameAs(BlendState.ReplaceAlphaOnPremultipliedTarget);
            Renderer.AdjustBlendStateForRenderTargetBake(CopyOf(BlendState.MinAlpha), isBakingRenderTarget: true)
                .ShouldBeSameAs(BlendState.MinAlphaOnPremultipliedTarget);

            BlendState.ReplaceAlphaOnPremultipliedTarget.ColorSourceBlend.ShouldBe(Blend.Zero);
            BlendState.ReplaceAlphaOnPremultipliedTarget.ColorDestinationBlend.ShouldBe(Blend.SourceAlpha);
            BlendState.ReplaceAlphaOnPremultipliedTarget.AlphaDestinationBlend.ShouldBe(Blend.Zero);
            BlendState.MinAlphaOnPremultipliedTarget.ColorDestinationBlend.ShouldBe(Blend.SourceAlpha);
            BlendState.MinAlphaOnPremultipliedTarget.AlphaBlendFunction.ShouldBe(Gum.BlendFunction.Min);
        }
        finally
        {
            Renderer.NormalBlendState = previous;
        }
    }

    [Fact]
    public void AdjustBlendStateForRenderTargetBake_KeepsReplaceAndMinAlpha_WhenNotBakingOrPremultipliedPipeline()
    {
        var previous = Renderer.NormalBlendState;
        try
        {
            Renderer.NormalBlendState = BlendState.NonPremultiplied;
            Renderer.AdjustBlendStateForRenderTargetBake(BlendState.ReplaceAlpha, isBakingRenderTarget: false)
                .ShouldBeSameAs(BlendState.ReplaceAlpha);
            Renderer.AdjustBlendStateForRenderTargetBake(BlendState.MinAlpha, isBakingRenderTarget: false)
                .ShouldBeSameAs(BlendState.MinAlpha);

            Renderer.NormalBlendState = BlendState.AlphaBlend;
            Renderer.AdjustBlendStateForRenderTargetBake(BlendState.ReplaceAlpha, isBakingRenderTarget: true)
                .ShouldBeSameAs(BlendState.ReplaceAlpha);
            Renderer.AdjustBlendStateForRenderTargetBake(BlendState.MinAlpha, isBakingRenderTarget: true)
                .ShouldBeSameAs(BlendState.MinAlpha);
        }
        finally
        {
            Renderer.NormalBlendState = previous;
        }
    }

    private static BlendState CopyOf(BlendState source) => new BlendState
    {
        ColorSourceBlend = source.ColorSourceBlend,
        ColorBlendFunction = source.ColorBlendFunction,
        ColorDestinationBlend = source.ColorDestinationBlend,
        AlphaSourceBlend = source.AlphaSourceBlend,
        AlphaBlendFunction = source.AlphaBlendFunction,
        AlphaDestinationBlend = source.AlphaDestinationBlend,
    };
}
