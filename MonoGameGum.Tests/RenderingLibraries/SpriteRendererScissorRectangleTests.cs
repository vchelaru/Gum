using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Rectangle = System.Drawing.Rectangle;

namespace MonoGameGum.Tests.RenderingLibraries;

// #5947: MonoGame 3.8.6 DesktopGL's Clear can leave the GL scissor test enabled with the stale rect
// from the last applied state, which blanks the frame when that rect is 0x0. Gum used to hand
// SpriteBatch a 0x0 rect for every unclipped draw (scissor test disabled, so the rect was ignored);
// a viewport-sized rect makes the leak harmless.
public class SpriteRendererScissorRectangleTests
{
    [Fact]
    public void GetEffectiveScissorRectangle_WithoutClip_ReturnsCameraClientRect()
    {
        Camera camera = new Camera();
        camera.ClientLeft = 100;
        camera.ClientTop = 50;
        camera.ClientWidth = 800;
        camera.ClientHeight = 600;

        Rectangle result = SpriteRenderer.GetEffectiveScissorRectangle(clipRectangle: null, camera);

        result.ShouldBe(new Rectangle(100, 50, 800, 600));
    }

    [Fact]
    public void GetEffectiveScissorRectangle_WithClip_ReturnsClip()
    {
        Camera camera = new Camera();
        camera.ClientWidth = 800;
        camera.ClientHeight = 600;

        Rectangle result = SpriteRenderer.GetEffectiveScissorRectangle(new Rectangle(10, 20, 30, 40), camera);

        result.ShouldBe(new Rectangle(10, 20, 30, 40));
    }

    [Fact]
    public void GetEffectiveScissorRectangle_WithNegativeSizedClip_ClampsSizeToZero()
    {
        Camera camera = new Camera();
        camera.ClientWidth = 800;
        camera.ClientHeight = 600;

        Rectangle result = SpriteRenderer.GetEffectiveScissorRectangle(new Rectangle(10, 20, -5, -7), camera);

        result.ShouldBe(new Rectangle(10, 20, 0, 0));
    }
}
