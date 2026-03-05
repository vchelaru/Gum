using Gum.RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

public class BlendExtensionsTests : BaseTestClass
{
    [Fact]
    public void ToBlend_ReturnsAdditive_WhenBlendStateIsAdditive()
    {
        Gum.BlendState.Additive.ToBlend().ShouldBe(Blend.Additive);
    }

    [Fact]
    public void ToBlend_ReturnsMinAlpha_WhenBlendStateIsMinAlpha()
    {
        Gum.BlendState.MinAlpha.ToBlend().ShouldBe(Blend.MinAlpha);
    }

    [Fact]
    public void ToBlend_ReturnsNormal_WhenBlendStateIsNonPremultiplied()
    {
        Gum.BlendState.NonPremultiplied.ToBlend().ShouldBe(Blend.Normal);
    }

    [Fact]
    public void ToBlend_ReturnsNull_WhenBlendStateIsUnknown()
    {
        new Gum.BlendState().ToBlend().ShouldBeNull();
    }

    [Fact]
    public void ToBlend_ReturnsReplace_WhenBlendStateIsOpaque()
    {
        Gum.BlendState.Opaque.ToBlend().ShouldBe(Blend.Replace);
    }

    [Fact]
    public void ToBlend_ReturnsReplaceAlpha_WhenBlendStateIsReplaceAlpha()
    {
        Gum.BlendState.ReplaceAlpha.ToBlend().ShouldBe(Blend.ReplaceAlpha);
    }

    [Fact]
    public void ToBlend_ReturnsSubtractAlpha_WhenBlendStateIsSubtractAlpha()
    {
        Gum.BlendState.SubtractAlpha.ToBlend().ShouldBe(Blend.SubtractAlpha);
    }
}
