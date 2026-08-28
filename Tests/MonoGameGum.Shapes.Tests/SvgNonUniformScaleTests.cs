using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #4509 — Apos.Shapes' DrawSvg takes a scalar em size, so a Width that disagrees with the
// file's aspect ratio is otherwise ignored. Svg.Render corrects it by re-opening the ShapeBatch
// with a stretched view matrix, which costs two batch flushes, so it does that only when the
// mismatch is real. The decision and the matrix are pure math and are pinned here; the draw itself
// is covered by SvgNonUniformScaleRenderTests in MonoGameGum.IntegrationTests.
public class SvgNonUniformScaleTests
{
    [Theory]
    // Width already follows the file's ratio - the MaintainFileAspectRatio default, and the case
    // that must never pay for a batch break.
    [InlineData(200f, 100f, 2f, false)]
    // A square box on a 2:1 drawing: Apos would draw 200 wide inside a 100 wide element.
    [InlineData(100f, 100f, 2f, true)]
    // Sub-pixel disagreement isn't visible and isn't worth two flushes.
    [InlineData(200.2f, 100f, 2f, false)]
    // Degenerate dimensions divide by zero or scale to nothing.
    [InlineData(0f, 100f, 2f, false)]
    [InlineData(100f, 0f, 2f, false)]
    [InlineData(100f, 100f, 0f, false)]
    public void RequiresNonUniformScale_IsTrueOnlyForAVisibleAspectMismatch(
        float width, float height, float documentAspectRatio, bool expected)
    {
        Svg.RequiresNonUniformScale(width, height, documentAspectRatio).ShouldBe(expected);
    }

    [Fact]
    public void CreateNonUniformScaleMatrix_Unrotated_PullsTheDrawnRightEdgeOntoTheBoxRightEdge()
    {
        Vector2 topLeft = new Vector2(10, 20);
        float boxWidth = 100;
        float boxHeight = 100;
        float documentAspectRatio = 2;

        Matrix matrix = Svg.CreateNonUniformScaleMatrix(
            boxWidth, boxHeight, documentAspectRatio, topLeft, rotationRadians: 0);

        // Apos draws the 2:1 document 200 wide off the height, so its right edge lands at x = 210.
        Vector2 drawnRightEdge = new Vector2(topLeft.X + (boxHeight * documentAspectRatio), topLeft.Y);
        Vector2 transformed = Vector2.Transform(drawnRightEdge, matrix);

        transformed.X.ShouldBe(topLeft.X + boxWidth, 0.01f);
        transformed.Y.ShouldBe(topLeft.Y, 0.01f);

        // The top left corner is the pivot and must not move.
        Vector2 pivot = Vector2.Transform(topLeft, matrix);
        pivot.X.ShouldBe(topLeft.X, 0.01f);
        pivot.Y.ShouldBe(topLeft.Y, 0.01f);
    }

    [Fact]
    public void CreateNonUniformScaleMatrix_Rotated_StretchesAlongTheElementsLocalAxis()
    {
        // Skia's VectorSprite composes translate * rotate * scale, so the stretch happens in the
        // element's own frame and rotates with it. A world space x scale would shear a rotated
        // drawing instead.
        Vector2 topLeft = new Vector2(10, 20);
        float boxWidth = 100;
        float boxHeight = 100;
        float documentAspectRatio = 2;
        float rotationRadians = MathHelper.PiOver2;

        Matrix matrix = Svg.CreateNonUniformScaleMatrix(
            boxWidth, boxHeight, documentAspectRatio, topLeft, rotationRadians);

        // Quarter turn: the drawing's local +x runs along world +y, so its far corner sits 200
        // below the pivot and the stretch must halve that to 100.
        Vector2 drawnFarCorner = new Vector2(topLeft.X, topLeft.Y + (boxHeight * documentAspectRatio));
        Vector2 transformed = Vector2.Transform(drawnFarCorner, matrix);

        transformed.X.ShouldBe(topLeft.X, 0.01f);
        transformed.Y.ShouldBe(topLeft.Y + boxWidth, 0.01f);
    }
}
