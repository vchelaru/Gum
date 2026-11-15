using Gum;
using Shouldly;

namespace SilkNetGum.Tests;

public class FramebufferResizeTrackerTests
{
    private (int Width, int Height) _framebuffer = (800, 450);

    private FramebufferResizeTracker CreateSut() => new FramebufferResizeTracker(() => _framebuffer, 800, 450);

    [Fact]
    public void TryConsume_WithoutARequest_ShouldReturnFalse()
    {
        _framebuffer = (1000, 600);

        CreateSut().TryConsume(out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryConsume_AfterRequestWithNewSize_ShouldReturnTheFramebufferSizeReadAtConsumeTime()
    {
        FramebufferResizeTracker sut = CreateSut();
        sut.Request();
        // The window can resize again between the event and the next frame; the later size is the one to apply.
        _framebuffer = (1024, 768);

        sut.TryConsume(out int width, out int height).ShouldBeTrue();

        (width, height).ShouldBe((1024, 768));
    }

    [Fact]
    public void TryConsume_CalledTwiceForOneRequest_ShouldReturnFalseTheSecondTime()
    {
        FramebufferResizeTracker sut = CreateSut();
        _framebuffer = (1024, 768);
        sut.Request();
        sut.TryConsume(out _, out _);

        sut.TryConsume(out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryConsume_WhenSizeIsUnchanged_ShouldReturnFalse()
    {
        FramebufferResizeTracker sut = CreateSut();
        sut.Request();

        sut.TryConsume(out _, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 450)]
    [InlineData(800, 0)]
    public void TryConsume_WhenWindowIsMinimized_ShouldReturnFalseAndKeepTheLastSize(int width, int height)
    {
        // A zero-size render target is invalid, so a minimize must not recreate the surface.
        FramebufferResizeTracker sut = CreateSut();
        _framebuffer = (width, height);
        sut.Request();
        sut.TryConsume(out _, out _).ShouldBeFalse();

        _framebuffer = (1000, 600);
        sut.Request();
        sut.TryConsume(out int newWidth, out int newHeight).ShouldBeTrue();
        (newWidth, newHeight).ShouldBe((1000, 600));
    }

    [Fact]
    public void TryConsume_WhenRestoredToTheAppliedSizeAfterMinimize_ShouldReturnFalse()
    {
        FramebufferResizeTracker sut = CreateSut();
        _framebuffer = (0, 0);
        sut.Request();
        sut.TryConsume(out _, out _);

        _framebuffer = (800, 450);
        sut.Request();

        sut.TryConsume(out _, out _).ShouldBeFalse();
    }
}
