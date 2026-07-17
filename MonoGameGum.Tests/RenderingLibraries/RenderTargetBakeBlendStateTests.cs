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
}
