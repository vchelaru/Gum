using Gum;
using Gum.Input;
using Shouldly;

namespace BrutalGum.Tests;

public class BrutalMathTests
{
    [Theory]
    [InlineData(100.0, 800, 1600, 200.0)]
    [InlineData(100.0, 800, 800, 100.0)]
    [InlineData(100.0, 0, 1600, 100.0)]
    [InlineData(100.0, 800, 0, 100.0)]
    public void WindowToFramebuffer_ScalesByTheDpiRatio_AndPassesThroughWhenASizeIsUnknown(double value, int window, int framebuffer, double expected)
    {
        BrutalScale.WindowToFramebuffer(value, window, framebuffer).ShouldBe(expected);
    }
}
