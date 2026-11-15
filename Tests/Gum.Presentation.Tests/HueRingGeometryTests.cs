using Gum.ColorPicking;
using Shouldly;

namespace Gum.Presentation.Tests;

public class HueRingGeometryTests
{
    // A 200 wide control centered at (100, 100).
    private const double Center = 100;

    [Theory]
    [InlineData(100, 0, 0)]
    [InlineData(200, 100, 90)]
    [InlineData(100, 200, 180)]
    [InlineData(0, 100, 270)]
    public void HueAt_ZeroIsAtTheTop_AndGrowsClockwise(double x, double y, double expected)
    {
        HueRingGeometry.HueAt(Center, Center, x, y).ShouldBe(expected, tolerance: 0.001);
    }

    [Fact]
    public void HueAt_JustEitherSideOfTheTop_IsEitherSideOfTheSeam()
    {
        double left = HueRingGeometry.HueAt(Center, Center, 99, 0);
        double right = HueRingGeometry.HueAt(Center, Center, 101, 0);

        left.ShouldBeGreaterThan(359);
        right.ShouldBeLessThan(1);
    }

    [Fact]
    public void HueAt_AtTheCenter_IsZeroRatherThanNaN()
    {
        HueRingGeometry.HueAt(Center, Center, Center, Center).ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(280)]
    [InlineData(359)]
    public void PointAt_ThenHueAt_RoundTrips(double hue)
    {
        (double X, double Y) point = HueRingGeometry.PointAt(Center, Center, 90, hue);

        HueRingGeometry.HueAt(Center, Center, point.X, point.Y).ShouldBe(hue, tolerance: 0.001);
    }

    [Fact]
    public void PointAt_Zero_IsTheTopOfTheRing()
    {
        (double X, double Y) point = HueRingGeometry.PointAt(Center, Center, 90, 0);

        point.X.ShouldBe(100, tolerance: 0.001);
        point.Y.ShouldBe(10, tolerance: 0.001);
    }

    [Theory]
    [InlineData(100, 5, true)]
    [InlineData(100, 15, true)]
    [InlineData(100, 50, false)]
    [InlineData(100, 100, false)]
    [InlineData(5, 5, false)]
    [InlineData(100, -3, false)]
    public void IsOnRing_IsTrueOnlyInsideTheBand(double x, double y, bool expected)
    {
        HueRingGeometry.IsOnRing(Center, Center, x, y, outerRadius: 100, innerRadius: 80, tolerance: 0).ShouldBe(expected);
    }

    [Fact]
    public void IsOnRing_Tolerance_WidensTheBandOnBothSides()
    {
        HueRingGeometry.IsOnRing(Center, Center, 100, 22, outerRadius: 100, innerRadius: 80, tolerance: 0).ShouldBeFalse();
        HueRingGeometry.IsOnRing(Center, Center, 100, 22, outerRadius: 100, innerRadius: 80, tolerance: 3).ShouldBeTrue();
    }

    [Fact]
    public void SquareSide_FitsInsideTheInnerCircle_WithAGap()
    {
        double side = HueRingGeometry.SquareSide(innerRadius: 80, gap: 4);

        double corner = side / 2 * System.Math.Sqrt(2);
        corner.ShouldBeLessThanOrEqualTo(76 + 0.001);
        side.ShouldBeGreaterThan(100);
    }

    [Theory]
    [InlineData(10, 5, 15)]
    [InlineData(359, 2, 1)]
    [InlineData(1, -2, 359)]
    [InlineData(0, -1, 359)]
    [InlineData(280, 10, 290)]
    public void NudgeHue_WrapsAroundTheSeam(double hue, double delta, double expected)
    {
        HueRingGeometry.NudgeHue(hue, delta).ShouldBe(expected, tolerance: 0.001);
    }

    [Fact]
    public void DraggingThePointerAroundTheSeam_ChangesTheHueContinuously_NeverJumpingAcrossTheWheel()
    {
        // Purple (280) to orange (30) through red, in 1 degree pointer steps.
        double previous = 280;
        for (double angle = 280; angle <= 390; angle += 1)
        {
            (double X, double Y) point = HueRingGeometry.PointAt(Center, Center, 90, angle);
            double hue = HueRingGeometry.HueAt(Center, Center, point.X, point.Y);
            double step = System.Math.Abs(hue - previous);
            (step < 2 || step > 358).ShouldBeTrue($"hue jumped from {previous} to {hue}");
            previous = hue;
        }
        previous.ShouldBe(30, tolerance: 0.001);
    }
}
