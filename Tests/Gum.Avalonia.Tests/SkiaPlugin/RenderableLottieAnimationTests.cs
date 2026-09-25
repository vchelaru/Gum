using System;
using Shouldly;
using SkiaGum.Renderables;
using Xunit;

namespace Gum.Avalonia.Tests.SkiaPlugin;

public class RenderableLottieAnimationTests
{
    [Fact]
    public void PreRender_FlagsUpdate_OnlyAfterThrottleInterval()
    {
        DateTime now = new DateTime(2026, 1, 1, 12, 0, 0);
        RenderableLottieAnimation lottie = new RenderableLottieAnimation(() => now);

        lottie.PreRender();
        lottie.NeedsUpdate.ShouldBeTrue();
        lottie.NeedsUpdate = false;

        now = now.AddSeconds(0.05);
        lottie.PreRender();
        lottie.NeedsUpdate.ShouldBeFalse();

        now = now.AddSeconds(0.05);
        lottie.PreRender();
        lottie.NeedsUpdate.ShouldBeTrue();
    }

    [Fact]
    public void PreRender_DoesNotFlagUpdate_WhenNotAnimating()
    {
        DateTime now = new DateTime(2026, 1, 1, 12, 0, 0);
        RenderableLottieAnimation lottie = new RenderableLottieAnimation(() => now);
        lottie.IsAnimating = false;
        lottie.NeedsUpdate = false;

        now = now.AddSeconds(1);
        lottie.PreRender();

        lottie.NeedsUpdate.ShouldBeFalse();
    }
}
