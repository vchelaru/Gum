using Gum.Wireframe;
using BlendState = Gum.BlendState;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Covers issue #6154: <see cref="Renderer.UsePremultipliedAlpha"/> is the single switch that picks
/// the blend state and is pushed to a font creator that opts in via
/// <see cref="IPremultipliableFontCreator"/>.
/// </summary>
public class PremultipliedAlphaSwitchTests : BaseTestClass
{
    private readonly IInMemoryFontCreator? _savedCreator;

    public PremultipliedAlphaSwitchTests()
    {
        _savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        Renderer.UsePremultipliedAlpha = false;
    }

    public override void Dispose()
    {
        Renderer.UsePremultipliedAlpha = false;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = _savedCreator;
        base.Dispose();
    }

    [Fact]
    public void UsePremultipliedAlpha_ShouldSwitchNormalBlendState_BothWays()
    {
        Renderer.UsePremultipliedAlpha = true;
        Renderer.NormalBlendState.ShouldBe(BlendState.AlphaBlend);

        Renderer.UsePremultipliedAlpha = false;
        Renderer.NormalBlendState.ShouldBe(BlendState.NonPremultiplied);
    }

    [Fact]
    public void UsePremultipliedAlpha_ShouldLeaveCustomBlendStateAlone_WhenSetFalseRedundantly()
    {
        BlendState custom = BlendState.Additive;
        Renderer.NormalBlendState = custom;

        Renderer.UsePremultipliedAlpha = false;

        Renderer.NormalBlendState.ShouldBeSameAs(custom);
        Renderer.NormalBlendState = BlendState.NonPremultiplied;
    }

    [Fact]
    public void FontCreator_ShouldReceiveFlag_WhenSwitchChangesAfterCreatorIsAssigned()
    {
        PremultipliableFontCreator creator = new();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        creator.PremultiplyAlpha.ShouldBeFalse();

        Renderer.UsePremultipliedAlpha = true;
        creator.PremultiplyAlpha.ShouldBeTrue();

        Renderer.UsePremultipliedAlpha = false;
        creator.PremultiplyAlpha.ShouldBeFalse();
    }

    [Fact]
    public void FontCreator_ShouldReceiveFlag_WhenCreatorIsAssignedAfterSwitchIsOn()
    {
        Renderer.UsePremultipliedAlpha = true;

        PremultipliableFontCreator creator = new();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

        creator.PremultiplyAlpha.ShouldBeTrue();
    }

    [Fact]
    public void FontCreator_ShouldNotBeRequiredToOptIn()
    {
        Renderer.UsePremultipliedAlpha = true;

        CustomSetPropertyOnRenderable.InMemoryFontCreator = new PlainFontCreator();
        Renderer.UsePremultipliedAlpha = false;

        CustomSetPropertyOnRenderable.InMemoryFontCreator.ShouldBeOfType<PlainFontCreator>();
    }

    private class PlainFontCreator : IInMemoryFontCreator
    {
        public BitmapFont? TryCreateFont(BmfcSave bmfcSave) => null;
    }

    private sealed class PremultipliableFontCreator : PlainFontCreator, IPremultipliableFontCreator
    {
        public bool PremultiplyAlpha { get; set; }
    }
}
