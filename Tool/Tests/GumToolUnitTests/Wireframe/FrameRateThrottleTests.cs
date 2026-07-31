using Shouldly;
using XnaAndWinforms;

namespace GumToolUnitTests.Wireframe;

/// <summary>
/// Pins <see cref="FrameRateThrottle"/>, which is what lets a WPF render surface driven by
/// <c>CompositionTarget.Rendering</c> (a fixed ~vsync cadence) honor a requested frame rate.
/// </summary>
public class FrameRateThrottleTests
{
    [Fact]
    public void ShouldRenderFrame_IsFalse_WhenTheRequestedIntervalHasNotElapsed()
    {
        FrameRateThrottle throttle = new FrameRateThrottle();

        throttle.ShouldRenderFrame(millisecondsSinceLastFrame: 16.7, desiredFramesPerSecond: 30)
            .ShouldBeFalse();
    }

    [Fact]
    public void ShouldRenderFrame_IsTrue_OnceTheRequestedIntervalHasElapsed()
    {
        FrameRateThrottle throttle = new FrameRateThrottle();

        throttle.ShouldRenderFrame(millisecondsSinceLastFrame: 33.4, desiredFramesPerSecond: 30)
            .ShouldBeTrue();
    }

    [Fact]
    public void ShouldRenderFrame_IsTrue_WhenTheRequestedRateIsNotPositive()
    {
        FrameRateThrottle throttle = new FrameRateThrottle();

        throttle.ShouldRenderFrame(millisecondsSinceLastFrame: 0, desiredFramesPerSecond: 0)
            .ShouldBeTrue();
    }
}
