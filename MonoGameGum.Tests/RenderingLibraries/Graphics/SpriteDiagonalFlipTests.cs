using System;
using System.Numerics;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics;

// Pins the pure geometry Sprite.GetDiagonalFlipComposite uses to synthesize a diagonal (transpose)
// flip out of MonoGame's rotation + FlipHorizontally primitives, since SpriteEffects has no
// diagonal option. Simulates the exact vertex formula MonoGame's SpriteBatch uses internally
// (screenPos = position + Rotate(rotation) * ((localCorner - origin) * scale)) so the resulting
// corner permutation can be checked against FlatRedBall's FlipDiagonal (fixes top-left/bottom-right,
// swaps top-right/bottom-left) without needing a real GraphicsDevice.
public class SpriteDiagonalFlipTests : BaseTestClass
{
    private static Vector2 Rotate(Vector2 v, float radians)
    {
        var cos = (float)System.Math.Cos(radians);
        var sin = (float)System.Math.Sin(radians);
        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }

    private static Vector2 ScreenCornerFor(Vector2 localCorner, Vector2 position, Vector2 origin, Vector2 scale, float rotation) =>
        position + Rotate((localCorner - origin) * scale, rotation);

    private static void ShouldBeApproximately(Vector2 actual, Vector2 expected)
    {
        actual.X.ShouldBe(expected.X, tolerance: 0.001);
        actual.Y.ShouldBe(expected.Y, tolerance: 0.001);
    }

    [Fact]
    public void GetDiagonalFlipComposite_SwapsTopRightAndBottomLeft_FixesTopLeftAndBottomRight()
    {
        const float sourceSize = 10f;
        var scale = new Vector2(2f, 2f);
        const float baseRotation = 0f;
        var position = new Vector2(100f, 50f);

        var localTL = new Vector2(0, 0);
        var localTR = new Vector2(sourceSize, 0);
        var localBL = new Vector2(0, sourceSize);
        var localBR = new Vector2(sourceSize, sourceSize);

        // Baseline (non-diagonal) screen corners: origin = Vector2.Zero, matching Sprite.Render's
        // non-diagonal draw call.
        var baseTL = ScreenCornerFor(localTL, position, Vector2.Zero, scale, baseRotation);
        var baseTR = ScreenCornerFor(localTR, position, Vector2.Zero, scale, baseRotation);
        var baseBL = ScreenCornerFor(localBL, position, Vector2.Zero, scale, baseRotation);
        var baseBR = ScreenCornerFor(localBR, position, Vector2.Zero, scale, baseRotation);

        var composite = Sprite.GetDiagonalFlipComposite(sourceSize, sourceSize, scale, baseRotation);
        var diagonalPosition = position + composite.PositionOffset;

        // SpriteEffects.FlipHorizontally swaps which local corner carries which UV before
        // rotation: the vertex geometrically at local-TL now shows the TR texel and vice versa,
        // same for BL/BR. Drive the local corners through that same (flip-then-rotate) pipeline.
        var flippedTL = localTR;
        var flippedTR = localTL;
        var flippedBL = localBR;
        var flippedBR = localBL;

        var diagTL = ScreenCornerFor(flippedTL, diagonalPosition, composite.Origin, scale, composite.RotationRadians);
        var diagTR = ScreenCornerFor(flippedTR, diagonalPosition, composite.Origin, scale, composite.RotationRadians);
        var diagBL = ScreenCornerFor(flippedBL, diagonalPosition, composite.Origin, scale, composite.RotationRadians);
        var diagBR = ScreenCornerFor(flippedBR, diagonalPosition, composite.Origin, scale, composite.RotationRadians);

        // Main-diagonal transpose: the screen corners that showed UV_TL/UV_BR before still show
        // them, while UV_TR/UV_BL swap screen corners.
        ShouldBeApproximately(diagTL, baseTL);
        ShouldBeApproximately(diagBR, baseBR);
        ShouldBeApproximately(diagTR, baseBL);
        ShouldBeApproximately(diagBL, baseTR);
    }

    [Fact]
    public void GetDiagonalFlipComposite_PreservesFootprintCenter_WhenObjectItselfIsRotated()
    {
        const float sourceSize = 8f;
        var scale = Vector2.One;
        var baseRotation = MathF.PI / 6; // 30 degrees, an arbitrary non-zero object rotation
        var position = new Vector2(10f, 20f);

        var baselineCenter = position + Rotate(new Vector2(sourceSize / 2f, sourceSize / 2f) * scale, baseRotation);

        var composite = Sprite.GetDiagonalFlipComposite(sourceSize, sourceSize, scale, baseRotation);
        var diagonalPosition = position + composite.PositionOffset;

        // With origin re-pivoted to the source rect's center, "position" IS the on-screen center,
        // so this must match the baseline (non-diagonal) rectangle's center exactly.
        ShouldBeApproximately(diagonalPosition, baselineCenter);
    }

    [Fact]
    public void GetDiagonalFlipComposite_SquareSourceInSquareFootprint_KeepsTheScaleAndCenterPivot()
    {
        var scale = new Vector2(2f, 2f);

        var composite = Sprite.GetDiagonalFlipComposite(10f, 10f, scale, 0f);

        composite.Scale.ShouldBe(scale);
        composite.Origin.ShouldBe(new Vector2(5f, 5f));
        composite.PositionOffset.ShouldBe(new Vector2(10f, 10f));
        composite.RotationRadians.ShouldBe(-MathF.PI / 2, tolerance: 0.0001f);
    }

    [Fact]
    public void GetDiagonalFlipComposite_NonSquareSource_LandsTransposedOnTheOriginalFootprint()
    {
        // A 4x2 source drawn into a 20x40 footprint is a scale of (5, 20).
        const float sourceWidth = 4f;
        const float sourceHeight = 2f;
        var scale = new Vector2(5f, 20f);
        var position = new Vector2(100f, 50f);

        var composite = Sprite.GetDiagonalFlipComposite(sourceWidth, sourceHeight, scale, 0f);
        var diagonalPosition = position + composite.PositionOffset;

        // SpriteEffects.FlipHorizontally makes the vertex at each local corner carry the texel of
        // the corner mirrored across the vertical axis, before the rotation.
        var flippedTL = new Vector2(sourceWidth, 0);
        var flippedTR = new Vector2(0, 0);
        var flippedBL = new Vector2(sourceWidth, sourceHeight);
        var flippedBR = new Vector2(0, sourceHeight);

        var diagTL = ScreenCornerFor(flippedTL, diagonalPosition, composite.Origin, composite.Scale, composite.RotationRadians);
        var diagTR = ScreenCornerFor(flippedTR, diagonalPosition, composite.Origin, composite.Scale, composite.RotationRadians);
        var diagBL = ScreenCornerFor(flippedBL, diagonalPosition, composite.Origin, composite.Scale, composite.RotationRadians);
        var diagBR = ScreenCornerFor(flippedBR, diagonalPosition, composite.Origin, composite.Scale, composite.RotationRadians);

        // The texel for the source's top-left and bottom-right stay at the footprint's top-left and
        // bottom-right; the top-right and bottom-left texels swap places. The footprint is
        // x 100..120, y 50..90.
        ShouldBeApproximately(diagTL, new Vector2(100f, 50f));
        ShouldBeApproximately(diagBR, new Vector2(120f, 90f));
        ShouldBeApproximately(diagTR, new Vector2(100f, 90f));
        ShouldBeApproximately(diagBL, new Vector2(120f, 50f));
    }
}
