using Gum.Input;
using Shouldly;
using System.Numerics;

namespace UnityGum.Tests;

public class ScreenToCanvasMapperTests
{
    [Fact]
    public void Map_SameSize_FlipsYFromBottomLeftToTopLeft()
    {
        ScreenToCanvasMapper mapper = new ScreenToCanvasMapper();
        mapper.SetSizes(screenWidth: 800, screenHeight: 600, canvasPixelWidth: 800, canvasPixelHeight: 600);

        mapper.Map(new Vector2(10, 590)).ShouldBe(new Vector2(10, 10));
        mapper.Map(new Vector2(0, 0)).ShouldBe(new Vector2(0, 600));
    }

    [Fact]
    public void Map_SmallerCanvas_ScalesToCanvasPixels()
    {
        ScreenToCanvasMapper mapper = new ScreenToCanvasMapper();
        mapper.SetSizes(screenWidth: 1600, screenHeight: 1200, canvasPixelWidth: 800, canvasPixelHeight: 600);

        mapper.Map(new Vector2(400, 1000)).ShouldBe(new Vector2(200, 100));
    }

    [Fact]
    public void Map_ZeroScreenSize_ReturnsOrigin()
    {
        ScreenToCanvasMapper mapper = new ScreenToCanvasMapper();

        mapper.Map(new Vector2(10, 10)).ShouldBe(Vector2.Zero);
    }
}
