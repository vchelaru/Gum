using Gum.Input;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Moq;
using RenderingLibrary;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="CameraController"/>'s pan/zoom/hotkey math after its relocation to
/// Gum.Presentation (ADR-0005, part of #3846) — it previously read/wrote the camera through the
/// XNALIKE-only <c>Renderer.Self</c>/<c>SystemManagers.Default</c> singletons instead of its own
/// injected <see cref="Camera"/>, which is what actually blocked the move.
/// </summary>
public class CameraControllerTests
{
    private static Mock<IHotkeyManager> CreateHotkeyManagerMock()
    {
        // Every combo the class reads must resolve to a non-null KeyCombination distinct from the
        // one under test, since IsPressed() is an extension method that dereferences a null "this"
        // instance rather than short-circuiting.
        var hotkeyManager = new Mock<IHotkeyManager>();
        hotkeyManager.SetupGet(h => h.MoveCameraLeft).Returns(KeyCombination.Pressed(GumKey.Left));
        hotkeyManager.SetupGet(h => h.MoveCameraRight).Returns(KeyCombination.Pressed(GumKey.Right));
        hotkeyManager.SetupGet(h => h.MoveCameraUp).Returns(KeyCombination.Pressed(GumKey.Up));
        hotkeyManager.SetupGet(h => h.MoveCameraDown).Returns(KeyCombination.Pressed(GumKey.Down));
        hotkeyManager.SetupGet(h => h.ZoomCameraIn).Returns(KeyCombination.Pressed(GumKey.Add));
        hotkeyManager.SetupGet(h => h.ZoomCameraInAlternative).Returns(KeyCombination.Pressed(GumKey.F2));
        hotkeyManager.SetupGet(h => h.ZoomCameraOut).Returns(KeyCombination.Pressed(GumKey.Subtract));
        hotkeyManager.SetupGet(h => h.ZoomCameraOutAlternative).Returns(KeyCombination.Pressed(GumKey.F12));
        return hotkeyManager;
    }

    private static (CameraController Controller, Camera Camera, Mock<IHotkeyManager> HotkeyManager, Mock<IZoomController> ZoomController)
        CreateSut()
    {
        var camera = new Camera { ClientWidth = 800, ClientHeight = 600 };
        var hotkeyManager = CreateHotkeyManagerMock();
        var zoomController = new Mock<IZoomController>();

        var controller = new CameraController();
        controller.Initialize(camera, zoomController.Object, defaultWidth: 800, defaultHeight: 600, hotkeyManager.Object);

        return (controller, camera, hotkeyManager, zoomController);
    }

    [Fact]
    public void HandleKeyPress_MoveCameraLeft_MovesCameraByStepDividedByZoomAndRaisesCameraChanged()
    {
        var (controller, camera, hotkeyManager, _) = CreateSut();
        camera.Zoom = 2f;
        var originalX = camera.X;
        var raised = 0;
        controller.CameraChanged += () => raised++;

        controller.HandleKeyPress(new GumKeyEventArgs { Key = GumKey.Left });

        camera.X.ShouldBe(originalX - 10 / 2f);
        raised.ShouldBe(1);
    }

    [Fact]
    public void HandleKeyPress_ZoomCameraIn_CallsZoomControllerZoomInAndRaisesCameraChanged()
    {
        var (controller, _, _, zoomController) = CreateSut();
        var raised = 0;
        controller.CameraChanged += () => raised++;

        controller.HandleKeyPress(new GumKeyEventArgs { Key = GumKey.Add });

        zoomController.Verify(z => z.ZoomIn(), Times.Once);
        zoomController.Verify(z => z.ZoomOut(), Times.Never);
        raised.ShouldBe(1);
    }

    [Fact]
    public void HandleKeyPress_UnrelatedKey_DoesNothing()
    {
        var (controller, camera, _, zoomController) = CreateSut();
        var originalX = camera.X;
        var originalY = camera.Y;
        var raised = 0;
        controller.CameraChanged += () => raised++;

        controller.HandleKeyPress(new GumKeyEventArgs { Key = GumKey.Z });

        camera.X.ShouldBe(originalX);
        camera.Y.ShouldBe(originalY);
        zoomController.Verify(z => z.ZoomIn(), Times.Never);
        zoomController.Verify(z => z.ZoomOut(), Times.Never);
        raised.ShouldBe(0);
    }

    [Fact]
    public void HandleMouseDown_ThenHandleMouseMove_WithMiddleButton_PansCameraOppositeToMouseDeltaAndRaisesCameraChanged()
    {
        var (controller, camera, _, _) = CreateSut();
        camera.Zoom = 2f;
        var originalX = camera.X;
        var originalY = camera.Y;
        var raised = 0;
        controller.CameraChanged += () => raised++;

        controller.HandleMouseDown(new GumMouseEventArgs { X = 100, Y = 100, Button = GumMouseButton.Middle });
        controller.HandleMouseMove(new GumMouseEventArgs { X = 130, Y = 90, Button = GumMouseButton.Middle });

        // Dragging the mouse right/up should move the world (camera position) left/down relative
        // to the drag, scaled by zoom - i.e. content appears to follow the cursor.
        camera.X.ShouldBe(originalX - (130 - 100) / 2f);
        camera.Y.ShouldBe(originalY - (90 - 100) / 2f);
        raised.ShouldBe(1);
    }

    [Fact]
    public void HandleMouseMove_WithLeftButton_DoesNotPanCameraOrRaiseCameraChanged()
    {
        var (controller, camera, _, _) = CreateSut();
        var originalX = camera.X;
        var originalY = camera.Y;
        var raised = 0;
        controller.CameraChanged += () => raised++;

        controller.HandleMouseDown(new GumMouseEventArgs { X = 100, Y = 100, Button = GumMouseButton.Left });
        controller.HandleMouseMove(new GumMouseEventArgs { X = 200, Y = 200, Button = GumMouseButton.Left });

        camera.X.ShouldBe(originalX);
        camera.Y.ShouldBe(originalY);
        raised.ShouldBe(0);
    }

    [Fact]
    public void HandleMouseWheel_ScrollUp_ZoomsInAndKeepsWorldPointUnderCursorFixed()
    {
        var (controller, camera, _, zoomController) = CreateSut();
        zoomController.Setup(z => z.ZoomIn()).Callback(() => camera.Zoom = 2f);
        var raised = 0;
        controller.CameraChanged += () => raised++;

        var mouseArgs = new GumMouseEventArgs { X = 300, Y = 200, Delta = 120 };
        camera.ScreenToWorld(mouseArgs.X, mouseArgs.Y, out var worldXBefore, out var worldYBefore);

        controller.HandleMouseWheel(mouseArgs);

        zoomController.Verify(z => z.ZoomIn(), Times.Once);
        camera.ScreenToWorld(mouseArgs.X, mouseArgs.Y, out var worldXAfter, out var worldYAfter);
        worldXAfter.ShouldBe(worldXBefore, tolerance: 0.001);
        worldYAfter.ShouldBe(worldYBefore, tolerance: 0.001);
        mouseArgs.Handled.ShouldBeTrue();
        raised.ShouldBe(1);
    }

    [Fact]
    public void HandleMouseWheel_ScrollDown_CallsZoomOut()
    {
        var (controller, camera, _, zoomController) = CreateSut();

        controller.HandleMouseWheel(new GumMouseEventArgs { X = 300, Y = 200, Delta = -120 });

        zoomController.Verify(z => z.ZoomOut(), Times.Once);
        zoomController.Verify(z => z.ZoomIn(), Times.Never);
    }
}
