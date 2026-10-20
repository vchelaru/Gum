using Microsoft.Xna.Framework;
using Gum.Renderables;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #6246: a bounds mask stays open across the fill slot and stroke slot of one shape, so
/// <see cref="ShapeRenderer"/> must not let it outlive its shapes. A mask left open (its stroke slot never drew)
/// is closed by the next shape's mask, a draw with another blend, or the end of the batch.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ShapeRendererAlphaBoundsMaskTests : BaseTestClass
{
    [Fact]
    public void BeginAlphaBoundsMask_ForAnotherOwner_ClosesTheMaskLeftOpen()
    {
        using ShapeGameLease game = new ShapeGameLease();
        ShapeRenderer renderer = ShapeRenderer.Self;
        Circle first = new() { Blend = Gum.RenderingLibrary.Blend.ReplaceAlpha };
        Circle second = new() { Blend = Gum.RenderingLibrary.Blend.ReplaceAlpha };
        renderer.BeginBatch(null, null, first, null, isBakingRenderTarget: true);

        renderer.BeginAlphaBoundsMask(first, Vector2.Zero, new Vector2(10, 10), 0f);
        renderer.IsAlphaBoundsMaskOpenFor(first).ShouldBeTrue();
        renderer.IsAlphaBoundsMaskOpenFor(second).ShouldBeFalse();

        renderer.BeginAlphaBoundsMask(second, Vector2.Zero, new Vector2(10, 10), 0f);
        renderer.IsAlphaBoundsMaskOpenFor(first).ShouldBeFalse();
        renderer.IsAlphaBoundsMaskOpenFor(second).ShouldBeTrue();

        renderer.EndAlphaBoundsMask();
        renderer.IsAlphaBoundsMaskOpenFor(second).ShouldBeFalse();
        renderer.EndBatch();
    }

    [Fact]
    public void EnsureBlend_ForAnotherBlend_ClosesTheMaskLeftOpen()
    {
        using ShapeGameLease game = new ShapeGameLease();
        ShapeRenderer renderer = ShapeRenderer.Self;
        Circle mask = new() { Blend = Gum.RenderingLibrary.Blend.ReplaceAlpha };
        Circle other = new() { Blend = Gum.RenderingLibrary.Blend.Normal };
        renderer.BeginBatch(null, null, mask, null, isBakingRenderTarget: true);
        renderer.BeginAlphaBoundsMask(mask, Vector2.Zero, new Vector2(10, 10), 0f);

        renderer.EnsureBlend(other);

        renderer.IsAlphaBoundsMaskOpenFor(mask).ShouldBeFalse();
        renderer.EndBatch();
    }

    [Fact]
    public void EndBatch_ClosesTheMaskLeftOpen()
    {
        using ShapeGameLease game = new ShapeGameLease();
        ShapeRenderer renderer = ShapeRenderer.Self;
        Circle mask = new() { Blend = Gum.RenderingLibrary.Blend.MinAlpha };
        renderer.BeginBatch(null, null, mask, null, isBakingRenderTarget: true);
        renderer.BeginAlphaBoundsMask(mask, Vector2.Zero, new Vector2(10, 10), 0f);

        renderer.EndBatch();

        renderer.IsAlphaBoundsMaskOpenFor(mask).ShouldBeFalse();
    }
}
