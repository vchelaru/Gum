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

    // Forward direction (Blend -> BlendState). Previously untested even though ToBlendState is what
    // every runtime (SpriteRuntime, ContainerRuntime, NineSliceRuntime, TextRuntime, ...) actually
    // calls to apply a Blend value — RaylibGum's BlendModeExtensions (issue #3470) now also depends
    // on it to derive raylib's low-level blend factors, so pin the exact BlendState each Blend value
    // resolves to.
    [Fact]
    public void ToBlendState_ReturnsMinAlpha_WhenBlendIsMinAlpha()
    {
        Blend.MinAlpha.ToBlendState().ShouldBeSameAs(Gum.BlendState.MinAlpha);
    }

    [Fact]
    public void ToBlendState_ReturnsOpaque_WhenBlendIsReplace()
    {
        Blend.Replace.ToBlendState().ShouldBeSameAs(Gum.BlendState.Opaque);
    }

    [Fact]
    public void ToBlendState_ReturnsReplaceAlpha_WhenBlendIsReplaceAlpha()
    {
        Blend.ReplaceAlpha.ToBlendState().ShouldBeSameAs(Gum.BlendState.ReplaceAlpha);
    }

    [Fact]
    public void ToBlendState_ReturnsSubtractAlpha_WhenBlendIsSubtractAlpha()
    {
        Blend.SubtractAlpha.ToBlendState().ShouldBeSameAs(Gum.BlendState.SubtractAlpha);
    }
}
