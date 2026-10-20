using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using BlendState = Gum.BlendState;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Issue #6237: text drawn with ReplaceAlpha or MinAlpha masks its whole bounds through the stencil
/// buffer of the render target it bakes into, so only those blends ask for a stencil-capable target.
/// The pixels are covered by MonoGameGum.IntegrationTests' TextAlphaBoundsMaskTests.
/// </summary>
public class TextRequiresStencilBufferTests : BaseTestClass
{
    [Fact]
    public void RequiresStencilBuffer_IsTrue_ForReplaceAlphaAndMinAlpha_OnTheStraightPipeline()
    {
        Text text = new Text();

        text.BlendState = BlendState.ReplaceAlpha;
        text.RequiresStencilBuffer.ShouldBeTrue();

        text.BlendState = BlendState.MinAlpha;
        text.RequiresStencilBuffer.ShouldBeTrue();
    }

    [Fact]
    public void RequiresStencilBuffer_IsFalse_ForOtherBlends()
    {
        Text text = new Text();

        text.RequiresStencilBuffer.ShouldBeFalse();
        foreach (BlendState blendState in new[] { BlendState.NonPremultiplied, BlendState.AlphaBlend, BlendState.Additive, BlendState.Opaque, BlendState.SubtractAlpha })
        {
            text.BlendState = blendState;
            text.RequiresStencilBuffer.ShouldBeFalse(blendState.ToString());
        }
    }

    [Fact]
    public void RequiresStencilBuffer_UsesThePremultipliedStates_OnThePremultipliedPipeline()
    {
        BlendState previous = Renderer.NormalBlendState;
        try
        {
            Renderer.NormalBlendState = BlendState.AlphaBlend;
            Text text = new Text();

            text.BlendState = BlendState.ReplaceAlphaPremultiplied;
            text.RequiresStencilBuffer.ShouldBeTrue();

            text.BlendState = BlendState.MinAlphaPremultiplied;
            text.RequiresStencilBuffer.ShouldBeTrue();

            text.BlendState = BlendState.ReplaceAlpha;
            text.RequiresStencilBuffer.ShouldBeFalse();
        }
        finally
        {
            Renderer.NormalBlendState = previous;
        }
    }

    [Fact]
    public void RequiresStencilBuffer_IsFalse_WhenTextRendersThroughATexture()
    {
        TextRenderingMode previous = Text.TextRenderingMode;
        try
        {
            Text.TextRenderingMode = TextRenderingMode.RenderTarget;
            Text text = new Text();
            text.BlendState = BlendState.ReplaceAlpha;

            text.RequiresStencilBuffer.ShouldBeFalse();
        }
        finally
        {
            Text.TextRenderingMode = previous;
        }
    }
}
