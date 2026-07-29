using System.Numerics;
using Gum.GueDeriving;
using Gum.Wireframe;
using MonoGameGum.Input;
using Moq;
using Shouldly;

namespace MonoGameGum.Tests.V2.Runtimes;

/// <summary>
/// Regression tests for <see cref="GraphicalUiElement.HitTestTransformMatrix"/> /
/// <see cref="GraphicalUiElement.EffectiveHitTestTransformMatrix"/> (issue #4096).
///
/// The transform lets a subtree drawn into an externally-scaled render target stay
/// cursor-interactive: the caller sets the matrix that maps a raw window pixel back into
/// the coordinate space the subtree was drawn in (the inverse of the caller's RT-to-screen
/// blit). It is resolved by climbing to the nearest ancestor that set it, mirroring
/// <c>EffectiveManagers</c>, and is consumed ONLY in hit-testing.
///
/// The invariant these tests guard: setting the transform changes hit-test results but
/// never render/layout output — the same draw-vs-hit-test split <c>MatrixRoutingTests</c>
/// guards for <c>Layer</c>.
/// </summary>
public class HitTestTransformTests : BaseTestClass
{
    [Fact]
    public void HasCursorOver_UsesEffectiveHitTestTransform_ClimbedFromAncestor()
    {
        // Game container drawn into a half-scale render target: a click at window pixel
        // (150, 50) lands over a child that only occupies world [0,100] once the pixel is
        // mapped back through a 0.5x transform to (75, 25).
        ContainerRuntime gameContainer = new() { X = 0, Y = 0, Width = 200, Height = 200 };
        ContainerRuntime button = new() { X = 0, Y = 0, Width = 100, Height = 100 };
        gameContainer.Children.Add(button);
        GumService.Default.Root.Children.Add(gameContainer);
        GumService.Default.Root.UpdateLayout();

        Mock<ICursor> cursor = new();
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.X).Returns(150);
        cursor.Setup(c => c.Y).Returns(50);

        // Without a transform the raw window pixel (150, 50) is outside the button.
        button.HasCursorOver(cursor.Object).ShouldBeFalse();

        // Set the transform on the ANCESTOR only; the child must resolve it by climbing.
        gameContainer.HitTestTransformMatrix = Matrix3x2.CreateScale(0.5f);

        // (150, 50) maps to (75, 25), which is inside the button.
        button.HasCursorOver(cursor.Object).ShouldBeTrue();
    }

    [Fact]
    public void HitTestTransformMatrix_DoesNotAffectRenderCoordinates()
    {
        ContainerRuntime gameContainer = new() { X = 30, Y = 40, Width = 200, Height = 200 };
        ContainerRuntime button = new() { X = 10, Y = 20, Width = 100, Height = 100 };
        gameContainer.Children.Add(button);
        GumService.Default.Root.Children.Add(gameContainer);
        GumService.Default.Root.UpdateLayout();

        float baselineLeft = button.AbsoluteLeft;
        float baselineTop = button.AbsoluteTop;

        // A hit-test transform must never feed layout/rendering, so absolute positions
        // stay put — this is the "changes hit-testing, not render output" half of #4096.
        gameContainer.HitTestTransformMatrix = Matrix3x2.CreateScale(0.5f);
        GumService.Default.Root.UpdateLayout();

        button.AbsoluteLeft.ShouldBe(baselineLeft);
        button.AbsoluteTop.ShouldBe(baselineTop);
    }
}
