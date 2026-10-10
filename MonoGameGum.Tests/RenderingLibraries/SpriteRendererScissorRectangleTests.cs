using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Rectangle = System.Drawing.Rectangle;

namespace MonoGameGum.Tests.RenderingLibraries;

// #5947/#5950: MonoGame 3.8.6 DesktopGL's Clear can leave the GL scissor test enabled with the stale
// rect from the last applied state, which clips the frame to that rect. Gum used to hand SpriteBatch
// a 0x0 rect for every unclipped draw (scissor test disabled, so the rect was ignored). A rect
// covering the whole render target makes the leak harmless; anything smaller still clips the rest.
public class SpriteRendererScissorRectangleTests
{
    [Fact]
    public void GetEffectiveScissorRectangle_WithoutClip_ReturnsWholeTarget()
    {
        Rectangle result = SpriteRenderer.GetEffectiveScissorRectangle(
            clipRectangle: null, targetWidth: 800, targetHeight: 480);

        result.ShouldBe(new Rectangle(0, 0, 800, 480));
    }

    [Fact]
    public void GetEffectiveScissorRectangle_WithClip_ReturnsClip()
    {
        Rectangle result = SpriteRenderer.GetEffectiveScissorRectangle(
            new Rectangle(10, 20, 30, 40), targetWidth: 800, targetHeight: 480);

        result.ShouldBe(new Rectangle(10, 20, 30, 40));
    }

    [Fact]
    public void GetEffectiveScissorRectangle_WithNegativeSizedClip_ClampsSizeToZero()
    {
        Rectangle result = SpriteRenderer.GetEffectiveScissorRectangle(
            new Rectangle(10, 20, -5, -7), targetWidth: 800, targetHeight: 480);

        result.ShouldBe(new Rectangle(10, 20, 0, 0));
    }
}
