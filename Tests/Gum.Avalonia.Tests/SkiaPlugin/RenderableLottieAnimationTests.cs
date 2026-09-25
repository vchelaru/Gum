using System;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using Xunit;

namespace Gum.Avalonia.Tests.SkiaPlugin;

public class RenderableLottieAnimationTests
{
    private sealed class AnimatingDrawable : RenderableSkiaObject, IAnimatingRenderable
    {
        public bool IsAnimating { get; set; }

        public override void DrawToSurface(SKSurface surface) { }
    }

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
    public void IsAnimating_IsFalse_WithoutALoadedAnimation()
    {
        RenderableLottieAnimation lottie = new RenderableLottieAnimation();

        lottie.IsAnimating.ShouldBeFalse();
    }

    [Fact]
    public void SkiaTexturedRenderable_IsAnimating_FollowsItsDrawable()
    {
        AnimatingDrawable drawable = new AnimatingDrawable { IsAnimating = true };
        SkiaTexturedRenderable renderable = new SkiaTexturedRenderable(drawable);

        renderable.IsAnimating.ShouldBeTrue();

        drawable.IsAnimating = false;
        renderable.IsAnimating.ShouldBeFalse();
    }
}
