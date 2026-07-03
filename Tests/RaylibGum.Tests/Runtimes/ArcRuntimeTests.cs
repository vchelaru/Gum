using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// Issue #3454 — pins the gradient surface added to the raylib branch of the shared
/// <c>ArcRuntime</c>. The arc is stroke-only, so (per the #3009 model shared with Circle/Rectangle)
/// the gradient START is the arc's primary <c>Color</c>, synced into the renderable's <c>Color1</c>
/// each frame in <c>PreRender</c>; <c>Color2</c> is the standalone second stop.
/// </summary>
public class ArcRuntimeTests : BaseTestClass
{
    [Fact]
    public void Gradient_Surface_ForwardsToContainedRenderable()
    {
        ArcRuntime sut = new();
        Color c2 = new Color(0, 0, 255, 255);

        sut.UseGradient = true;
        sut.GradientType = GradientType.Radial;
        sut.Color2 = c2;
        sut.GradientX1 = 4f;
        sut.GradientY1 = 8f;
        sut.GradientX2 = 56f;
        sut.GradientY2 = 28f;
        sut.GradientInnerRadius = 4f;
        sut.GradientOuterRadius = 28f;

        LineArc inner = (LineArc)sut.RenderableComponent!;
        inner.UseGradient.ShouldBeTrue();
        inner.GradientType.ShouldBe(GradientType.Radial);
        inner.Color2.B.ShouldBe((byte)255);
        inner.GradientX1.ShouldBe(4f);
        inner.GradientY1.ShouldBe(8f);
        inner.GradientX2.ShouldBe(56f);
        inner.GradientY2.ShouldBe(28f);
        inner.GradientInnerRadius.ShouldBe(4f);
        inner.GradientOuterRadius.ShouldBe(28f);
    }

    // Issue #3454 / #3009 — the arc's gradient start follows its primary body Color rather than a
    // standalone Color1 slot. PreRender mirrors Color into the renderable's Color1 so the start
    // stop tracks the body regardless of how Color was set (state change, animation, .gumx load).
    [Fact]
    public void PreRender_SyncsGradientStartToBodyColor()
    {
        ArcRuntime sut = new();
        Color body = new Color(255, 0, 0, 255);

        sut.Color = body;
        sut.PreRender();

        LineArc inner = (LineArc)sut.RenderableComponent!;
        inner.Color1.R.ShouldBe((byte)255);
        inner.Color1.G.ShouldBe((byte)0);
        inner.Color1.B.ShouldBe((byte)0);
        inner.Color1.A.ShouldBe((byte)255);
    }

    [Fact]
    public void Color2_Channels_RoundTrip()
    {
        ArcRuntime sut = new();

        sut.Red2 = 12;
        sut.Green2 = 34;
        sut.Blue2 = 56;
        sut.Alpha2 = 78;

        sut.Red2.ShouldBe(12);
        sut.Green2.ShouldBe(34);
        sut.Blue2.ShouldBe(56);
        sut.Alpha2.ShouldBe(78);
        sut.Color2.R.ShouldBe((byte)12);
        sut.Color2.A.ShouldBe((byte)78);
    }
}
