using System.Linq;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
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

    [Fact]
    public void ToXNA_DoesNotThrow_WhenSeveralThreadsConvertTheSameStaleStateAtOnce()
    {
        // Probabilistic: the race is only possible when threads overlap, so many rounds run.
        for (int round = 0; round < 300; round++)
        {
            Gum.BlendState custom = new Gum.BlendState
            {
                ColorSourceBlend = Gum.Blend.One,
                ColorDestinationBlend = Gum.Blend.One,
            };
            custom.ToXNA().Dispose();

            using System.Threading.Barrier start = new System.Threading.Barrier(8);
            System.Threading.Tasks.Task[] tasks = Enumerable.Range(0, 8).Select(_ => System.Threading.Tasks.Task.Run(() =>
            {
                start.SignalAndWait();
                custom.ToXNA();
            })).ToArray();

            Should.NotThrow(() => System.Threading.Tasks.Task.WaitAll(tasks));
        }
    }

    [Fact]
    public void ToGum_ReturnsTheSourceState_ForACustomStateConvertedByToXNA()
    {
        Gum.BlendState custom = CreateStraightAlphaOver();

        custom.ToXNA().ToGum().ShouldBeSameAs(custom);
    }

    [Fact]
    public void ToGum_ReturnsANewState_WhenTheSourceWasEditedAfterConversion()
    {
        Gum.BlendState custom = new Gum.BlendState
        {
            ColorSourceBlend = Gum.Blend.One,
            ColorDestinationBlend = Gum.Blend.Zero,
        };
        XnaBlendState xna = custom.ToXNA();

        custom.ColorSourceBlend = Gum.Blend.SourceAlpha;
        Gum.BlendState roundTripped = xna.ToGum();

        roundTripped.ShouldNotBeSameAs(custom);
        roundTripped.ColorSourceBlend.ShouldBe(Gum.Blend.One);
    }

    // Blend.Normal maps to Renderer.NormalBlendState, and the runtimes' Blend setters round-trip it
    // through XNA. Every element must hold NormalBlendState itself: the renderer compares blend
    // states by reference and restarts the SpriteBatch on any difference (#8253).
    [Theory]
    [InlineData(nameof(ContainerRuntime))]
    [InlineData(nameof(SpriteRuntime))]
    [InlineData(nameof(NineSliceRuntime))]
    [InlineData(nameof(ColoredRectangleRuntime))]
    [InlineData(nameof(TextRuntime))]
    public void BlendNormal_HoldsTheNormalBlendStateInstance_WhenNormalBlendStateIsCustom(string runtimeType)
    {
        Gum.BlendState previous = Renderer.NormalBlendState;
        Gum.BlendState custom = CreateStraightAlphaOver();
        try
        {
            Renderer.NormalBlendState = custom;
            GraphicalUiElement element = runtimeType switch
            {
                nameof(ContainerRuntime) => new ContainerRuntime { Blend = Gum.RenderingLibrary.Blend.Normal },
                nameof(SpriteRuntime) => new SpriteRuntime { Blend = Gum.RenderingLibrary.Blend.Normal },
                nameof(NineSliceRuntime) => new NineSliceRuntime { Blend = Gum.RenderingLibrary.Blend.Normal },
                nameof(ColoredRectangleRuntime) => new ColoredRectangleRuntime { Blend = Gum.RenderingLibrary.Blend.Normal },
                _ => new TextRuntime { Blend = Gum.RenderingLibrary.Blend.Normal },
            };

            ((IRenderableIpso)element.RenderableComponent).BlendState.ShouldBeSameAs(custom);
        }
        finally
        {
            Renderer.NormalBlendState = previous;
        }
    }

    private static Gum.BlendState CreateStraightAlphaOver() => new Gum.BlendState
    {
        ColorSourceBlend = Gum.Blend.SourceAlpha,
        ColorDestinationBlend = Gum.Blend.InverseSourceAlpha,
        AlphaSourceBlend = Gum.Blend.One,
        AlphaDestinationBlend = Gum.Blend.InverseSourceAlpha,
    };
}
