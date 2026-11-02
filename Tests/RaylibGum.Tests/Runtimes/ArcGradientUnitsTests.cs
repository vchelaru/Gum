using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Renderables;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// #7306 - the Arc standard's gradient *Units variables must convert the authored gradient
/// numbers to element pixels, so a percent-authored gradient matches the pixel-authored one
/// (as on MonoGame and Skia) instead of being read as raw pixels.
/// </summary>
public class ArcGradientUnitsTests : BaseTestClass
{
    private static ArcRuntime CreateArc()
    {
        ArcRuntime arc = new();
        arc.Width = 300;
        arc.Height = 200;
        arc.SetProperty("UseGradient", true);
        return arc;
    }

    [Fact]
    public void SetProperty_PercentEndpointUnits_ResolvesEndpointsToPixels()
    {
        ArcRuntime arc = CreateArc();
        arc.SetProperty("GradientX1", 10f);
        arc.SetProperty("GradientY1", 20f);
        arc.SetProperty("GradientX2", 100f);
        arc.SetProperty("GradientY2", 50f);
        arc.SetProperty("GradientX1Units", PositionUnitType.PercentageWidth);
        arc.SetProperty("GradientY1Units", PositionUnitType.PercentageHeight);
        arc.SetProperty("GradientX2Units", PositionUnitType.PercentageWidth);
        arc.SetProperty("GradientY2Units", PositionUnitType.PercentageHeight);
        arc.PreRender();

        LineArc renderable = (LineArc)arc.RenderableComponent!;
        renderable.EffectiveGradientX1.ShouldBe(30f);
        renderable.EffectiveGradientY1.ShouldBe(40f);
        renderable.EffectiveGradientX2.ShouldBe(300f);
        renderable.EffectiveGradientY2.ShouldBe(100f);
    }

    [Fact]
    public void SetProperty_PercentRadiusUnits_ResolvesRadiiToPixels()
    {
        ArcRuntime arc = CreateArc();
        arc.SetProperty("GradientInnerRadius", 10f);
        arc.SetProperty("GradientOuterRadius", 50f);
        arc.SetProperty("GradientInnerRadiusUnits", DimensionUnitType.PercentageOfParent);
        arc.SetProperty("GradientOuterRadiusUnits", DimensionUnitType.PercentageOfParent);
        arc.PreRender();

        LineArc renderable = (LineArc)arc.RenderableComponent!;
        renderable.EffectiveGradientInnerRadius.ShouldBe(30f);
        renderable.EffectiveGradientOuterRadius.ShouldBe(150f);
    }

    [Fact]
    public void SetProperty_PixelsFromMiddleAndLarge_OffsetsFromAxisSize()
    {
        ArcRuntime arc = CreateArc();
        arc.SetProperty("GradientX1", -10f);
        arc.SetProperty("GradientX2", -20f);
        arc.SetProperty("GradientX1Units", PositionUnitType.PixelsFromCenterX);
        arc.SetProperty("GradientX2Units", PositionUnitType.PixelsFromRight);
        arc.PreRender();

        LineArc renderable = (LineArc)arc.RenderableComponent!;
        renderable.EffectiveGradientX1.ShouldBe(140f);
        renderable.EffectiveGradientX2.ShouldBe(280f);
    }

    [Fact]
    public void RotatedEndpoints_PercentUnits_MatchPixelAuthoredEndpoints()
    {
        LineArc percent = new() { Width = 300, Height = 200, GradientX2 = 100, GradientY2 = 100 };
        percent.GradientX2Units = Gum.Converters.GeneralUnitType.Percentage;
        percent.GradientY2Units = Gum.Converters.GeneralUnitType.Percentage;
        LineArc pixels = new() { Width = 300, Height = 200, GradientX2 = 300, GradientY2 = 200 };

        percent.GetRotatedGradientEndpoints(30f).ShouldBe(pixels.GetRotatedGradientEndpoints(30f));
    }
}
