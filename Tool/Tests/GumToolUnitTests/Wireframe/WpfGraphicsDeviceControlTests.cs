using Shouldly;
using XnaAndWinforms;
using Xunit;

namespace GumToolUnitTests.Wireframe;

public class WpfGraphicsDeviceControlTests
{
    // #4681: the render target must be sized in physical pixels (DIU size * DPI scale), not raw
    // WPF device-independent units, or the bitmap ends up with fewer pixels than the monitor has
    // and WPF's own DPI compositing stretches it, blurring the canvas ("1 pixel becomes 2 pixels").
    [Theory]
    [InlineData(800.0, 1.0, 800)]
    [InlineData(800.0, 2.0, 1600)]
    [InlineData(801.4, 1.0, 801)]
    [InlineData(0.0, 2.0, 1)]
    [InlineData(-5.0, 2.0, 1)]
    public void ToPhysicalPixelSize_ConvertsDiuSizeByDpiScale(double diuSize, double dpiScale, int expected)
    {
        WpfGraphicsDeviceControl.ToPhysicalPixelSize(diuSize, dpiScale).ShouldBe(expected);
    }
}
