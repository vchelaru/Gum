using Gum.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Input;

/// <summary>
/// <see cref="Cursor.XRespectingGumZoomAndBounds"/> and the Y version without a GraphicsDevice
/// (headless tests and scripted runs): there is no viewport offset, so only the camera zoom and
/// position apply. The case with a real viewport is pinned in MonoGameGum.IntegrationTests
/// (CursorViewportTests).
/// </summary>
public class CursorZoomAndBoundsTests : BaseTestClass
{
    private readonly Camera _camera = SystemManagers.Default.Renderer.Camera;
    private readonly float _originalZoom;
    private readonly float _originalX;
    private readonly float _originalY;

    public CursorZoomAndBoundsTests()
    {
        _originalZoom = _camera.Zoom;
        _originalX = _camera.X;
        _originalY = _camera.Y;
    }

    public override void Dispose()
    {
        _camera.Zoom = _originalZoom;
        _camera.X = _originalX;
        _camera.Y = _originalY;
        base.Dispose();
    }

    [Fact]
    public void RespectingGumZoomAndBounds_WithoutGraphicsDevice_AppliesZoomAndCameraWithNoViewportOffset()
    {
        SystemManagers.Default.Renderer.GraphicsDevice.ShouldBeNull();
        _camera.Zoom = 2;
        _camera.X = 7;
        _camera.Y = 3;
        Cursor cursor = new((GameWindow?)null);

        cursor.Activity(0, new MouseState(50, 30, 0, ButtonState.Released, ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released), new TouchCollection());

        // 50 / 2 + 7 and 30 / 2 + 3
        cursor.XRespectingGumZoomAndBounds().ShouldBe(32);
        cursor.YRespectingGumZoomAndBounds().ShouldBe(18);
    }
}
