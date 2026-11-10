using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Pins the cache behind ColorTextureAlpha's fog pass (#7837): a blend whose color source is
/// SourceAlpha comes back as one cached copy with One in its place, until the source is edited.
/// </summary>
public class PremultipliedColorBlendStateTests : BaseTestClass
{
    [Fact]
    public void GetBlendStateForPremultipliedColor_ReturnsTheSameCopy_WhenCalledTwice()
    {
        SpriteRenderer renderer = new();
        Gum.BlendState source = Gum.BlendState.NonPremultiplied.Clone();

        Gum.BlendState? first = renderer.GetBlendStateForPremultipliedColor(source);
        Gum.BlendState? second = renderer.GetBlendStateForPremultipliedColor(source);

        second.ShouldBeSameAs(first);
        first.ShouldNotBeSameAs(source);
        first!.ColorSourceBlend.ShouldBe(Gum.Blend.One);
        first.ColorDestinationBlend.ShouldBe(source.ColorDestinationBlend);
        source.ColorSourceBlend.ShouldBe(Gum.Blend.SourceAlpha);
    }

    [Fact]
    public void GetBlendStateForPremultipliedColor_RebuildsTheCopy_WhenTheSourceWasEdited()
    {
        SpriteRenderer renderer = new();
        Gum.BlendState source = Gum.BlendState.NonPremultiplied.Clone();
        Gum.BlendState? before = renderer.GetBlendStateForPremultipliedColor(source);

        source.ColorDestinationBlend = Gum.Blend.One;
        Gum.BlendState? afterDestination = renderer.GetBlendStateForPremultipliedColor(source);

        afterDestination.ShouldNotBeSameAs(before);
        afterDestination!.ColorDestinationBlend.ShouldBe(Gum.Blend.One);
        afterDestination.ColorSourceBlend.ShouldBe(Gum.Blend.One);

        source.BlendFactor = System.Drawing.Color.Red;
        Gum.BlendState? afterBlendFactor = renderer.GetBlendStateForPremultipliedColor(source);

        afterBlendFactor.ShouldNotBeSameAs(afterDestination);
        afterBlendFactor!.BlendFactor.ShouldBe(System.Drawing.Color.Red);
    }

    [Fact]
    public void GetBlendStateForPremultipliedColor_ReturnsTheSource_WhenItsColorSourceIsNotSourceAlpha()
    {
        SpriteRenderer renderer = new();

        renderer.GetBlendStateForPremultipliedColor(Gum.BlendState.AlphaBlend).ShouldBeSameAs(Gum.BlendState.AlphaBlend);

        Gum.BlendState source = Gum.BlendState.NonPremultiplied.Clone();
        renderer.GetBlendStateForPremultipliedColor(source);
        source.ColorSourceBlend = Gum.Blend.One;
        renderer.GetBlendStateForPremultipliedColor(source).ShouldBeSameAs(source);
    }
}
