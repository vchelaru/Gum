using System;

namespace Gum.ColorPicking;

/// <summary>
/// The hue ring's geometry: hue 0 (red) is at the top and grows clockwise, so purple and orange sit
/// either side of the seam at the top and one drag passes through red.
/// </summary>
public static class HueRingGeometry
{
    /// <summary>The hue, 0 to under 360, of the direction from the center to a point; 0 at the center.</summary>
    public static double HueAt(double centerX, double centerY, double x, double y)
    {
        double dx = x - centerX;
        double dy = y - centerY;
        if (dx == 0 && dy == 0)
        {
            return 0;
        }
        return NudgeHue(System.Math.Atan2(dx, -dy) * 180 / System.Math.PI, 0);
    }

    /// <summary>The point at a hue on the circle of the given radius.</summary>
    public static (double X, double Y) PointAt(double centerX, double centerY, double radius, double hue)
    {
        double radians = hue * System.Math.PI / 180;
        return (centerX + radius * System.Math.Sin(radians), centerY - radius * System.Math.Cos(radians));
    }

    /// <summary>True when the point lies in the band between the inner and outer radius, widened by the tolerance on both sides.</summary>
    public static bool IsOnRing(double centerX, double centerY, double x, double y, double outerRadius, double innerRadius, double tolerance)
    {
        double distance = System.Math.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
        return distance >= innerRadius - tolerance && distance <= outerRadius + tolerance;
    }

    /// <summary>The side of the largest square whose corners stay a gap inside the inner circle.</summary>
    public static double SquareSide(double innerRadius, double gap) => (innerRadius - gap) * System.Math.Sqrt(2);

    /// <summary>Adds a delta to a hue, wrapping through the seam into 0 to under 360.</summary>
    public static double NudgeHue(double hue, double delta)
    {
        double wrapped = ((hue + delta) % 360 + 360) % 360;
        return wrapped >= 360 ? 0 : wrapped;
    }
}
