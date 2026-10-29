using RenderingLibrary;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// The baked texture of a render-target container is whole pixels (the world size rounded), so its
/// composite blit must be that same size in world units; a fractional world size would squeeze the
/// texture and drop the pixel row a .5 edge cuts in half on some GL drivers (#7073).
/// </summary>
public class RenderTargetCompositeDestinationTests
{
    [Theory]
    [InlineData(42.5f, 29.5f, 43, 30, 1f)]
    [InlineData(40.4f, 27.6f, 40, 28, 1f)]
    [InlineData(21.25f, 14.75f, 43, 30, 2f)]
    public void GetCompositeDestination_IsTheBakedTextureSizeInWorldUnits(
        float worldWidth, float worldHeight, int textureWidth, int textureHeight, float zoom)
    {
        RenderTargetBounds bounds = new(10.25f, 20.75f, 10.25f + worldWidth, 20.75f + worldHeight, textureWidth, textureHeight);

        Raylib_cs.Rectangle destination = global::RenderingLibrary.Graphics.Renderer.GetCompositeDestination(bounds, zoom);

        destination.X.ShouldBe(10.25f);
        destination.Y.ShouldBe(20.75f);
        destination.Width.ShouldBe(textureWidth / zoom);
        destination.Height.ShouldBe(textureHeight / zoom);
    }

    // Regression pin for unchanged behavior: a whole-pixel container already blitted 1:1.
    [Fact]
    public void GetCompositeDestination_WholeSizeContainer_IsUnchanged()
    {
        RenderTargetBounds bounds = new(10f, 20f, 50f, 48f, 40, 28);

        Raylib_cs.Rectangle destination = global::RenderingLibrary.Graphics.Renderer.GetCompositeDestination(bounds, 1f);

        destination.Width.ShouldBe(40f);
        destination.Height.ShouldBe(28f);
    }
}
