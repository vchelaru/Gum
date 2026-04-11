using System.Numerics;
using RenderingLibrary.Math;
using Shouldly;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;

namespace GumToolUnitTests.Math;

public class MathFunctionsTests
{
    [Fact]
    public void RotateVector_WithZeroRadians_ShouldNotChangeVector()
    {
        // Simulates resizing from bottom with center Y origin:
        // reposition = (0, someValue), rotation = 0
        Vector2 vector = new Vector2(0, 5.0f);

        MathFunctions.RotateVector(ref vector, radians: 0);

        vector.X.ShouldBe(0);
        vector.Y.ShouldBe(5.0f);
    }

    [Fact]
    public void RotateVector_WithZeroRadians_XOnlyShouldNotChangeVector()
    {
        Vector2 vector = new Vector2(3.0f, 0);

        MathFunctions.RotateVector(ref vector, radians: 0);

        vector.X.ShouldBe(3.0f);
        vector.Y.ShouldBe(0);
    }

    [Fact]
    public void RotateVector_With90Degrees_ShouldNotIntroduceDrift()
    {
        // A vector along the Y axis rotated by 90 degrees should land exactly on the X axis
        Vector2 vector = new Vector2(0, 5.0f);
        float radians = MathHelper.ToRadians(90);

        MathFunctions.RotateVector(ref vector, radians);

        // After 90-degree rotation, (0, 5) should become (-5, 0) or (5, 0)
        // depending on rotation direction. The key assertion is Y should be exactly 0.
        vector.Y.ShouldBe(0, customMessage: "Y should be exactly 0 after 90-degree rotation, not a tiny floating-point residual");
    }

    [Fact]
    public void RotateVector_With180Degrees_ShouldNotIntroduceDrift()
    {
        Vector2 vector = new Vector2(0, 5.0f);
        float radians = MathHelper.ToRadians(180);

        MathFunctions.RotateVector(ref vector, radians);

        // (0, 5) rotated 180 degrees should be (0, -5) — X should remain exactly 0
        vector.X.ShouldBe(0, customMessage: "X should be exactly 0 after 180-degree rotation");
    }

    [Fact]
    public void RotateVector_With270Degrees_ShouldNotIntroduceDrift()
    {
        Vector2 vector = new Vector2(0, 5.0f);
        float radians = MathHelper.ToRadians(270);

        MathFunctions.RotateVector(ref vector, radians);

        vector.Y.ShouldBe(0, customMessage: "Y should be exactly 0 after 270-degree rotation");
    }

    [Fact]
    public void RotateVector_WithZeroVector_ShouldRemainZero()
    {
        Vector2 vector = new Vector2(0, 0);

        MathFunctions.RotateVector(ref vector, radians: 1.5f);

        vector.X.ShouldBe(0);
        vector.Y.ShouldBe(0);
    }
}
