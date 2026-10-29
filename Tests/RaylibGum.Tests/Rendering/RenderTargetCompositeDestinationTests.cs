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
    [InlineData(42.5f, 29.5f, 85, 59, 2f)]
    [InlineData(40f, 28f, 40, 28, 1f)]
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
}
