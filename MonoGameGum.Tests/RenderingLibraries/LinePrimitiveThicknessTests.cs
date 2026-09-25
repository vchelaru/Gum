using RenderingLibrary.Math.Geometry;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

public class LinePrimitiveThicknessTests
{
    [Fact]
    public void GetThicknessScale_ShouldHonorLineWidthZoomAndSourceHeight()
    {
        float linePixelWidth = 3;
        float cameraZoom = 2;
        int sourceHeight = 4;

        float scale = LinePrimitive.GetThicknessScale(linePixelWidth, cameraZoom, sourceHeight);

        // 3 screen pixels at 2x zoom is 1.5 world units; a 4-texel-tall source must scale to that.
        (scale * sourceHeight).ShouldBe(1.5f);
    }
}
