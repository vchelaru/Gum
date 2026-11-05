using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaBlend = Microsoft.Xna.Framework.Graphics.Blend;
using XnaBlendState = Microsoft.Xna.Framework.Graphics.BlendState;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Pins the cache behind <see cref="XNAExtensions.ToXNA(Gum.BlendState?)"/>. SpriteBatchStack
/// converts the active blend on every SpriteBatch.Begin, so a custom Gum BlendState must come back
/// as the same XNA instance until it is edited (#7619).
/// </summary>
public class BlendStateXnaConversionTests : BaseTestClass
{
    [Fact]
    public void ToXNA_ReturnsTheSameInstance_WhenACustomStateIsConvertedTwice()
    {
        Gum.BlendState custom = Gum.BlendState.ReplaceAlpha;

        XnaBlendState first = custom.ToXNA();
        XnaBlendState second = custom.ToXNA();

        second.ShouldBeSameAs(first);
        first.AlphaSourceBlend.ShouldBe(custom.AlphaSourceBlend.ToXna());
    }

    [Fact]
    public void ToXNA_ReturnsTheXnaPreset_ForAPresetState()
    {
        Gum.BlendState.AlphaBlend.ToXNA().ShouldBeSameAs(XnaBlendState.AlphaBlend);
    }

    [Fact]
    public void ToXNA_RebuildsTheState_WhenTheGumStateWasEditedSinceTheLastConversion()
    {
        Gum.BlendState custom = new Gum.BlendState
        {
            ColorSourceBlend = Gum.Blend.One,
            ColorDestinationBlend = Gum.Blend.Zero,
        };
        XnaBlendState before = custom.ToXNA();
        before.ColorSourceBlend.ShouldBe(XnaBlend.One);

        custom.ColorSourceBlend = Gum.Blend.SourceAlpha;
        XnaBlendState after = custom.ToXNA();

        after.ColorSourceBlend.ShouldBe(XnaBlend.SourceAlpha);
        after.ShouldNotBeSameAs(before);
    }

    [Fact]
    public void ToXNA_ReturnsDifferentInstances_ForDifferentCustomStates()
    {
        Gum.BlendState.ReplaceAlpha.ToXNA().ShouldNotBeSameAs(Gum.BlendState.MinAlpha.ToXNA());
    }

    [Fact]
    public void ToXNA_RebuildsTheState_WhenTheCachedStateWasDisposed()
    {
        Gum.BlendState custom = new Gum.BlendState
        {
            ColorSourceBlend = Gum.Blend.One,
            ColorDestinationBlend = Gum.Blend.One,
        };
        XnaBlendState first = custom.ToXNA();
        first.Dispose();

        XnaBlendState second = custom.ToXNA();

        second.ShouldNotBeSameAs(first);
        second.IsDisposed.ShouldBeFalse();
    }
}
