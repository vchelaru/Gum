using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

public class CameraTests : BaseTestClass
{
    [Fact]
    public void NoSetFromMatrixCall_ShouldPreserveExplicitlySetCameraValues()
    {
        Camera _sut = new Camera();
        _sut.ClientWidth = 800;
        _sut.ClientHeight = 600;

        _sut.X = 123;
        _sut.Y = 456;
        _sut.Zoom = 2;

        // Sanity guard for the GumService.Draw() no-arg path:
        // nothing in the default render flow should mutate the camera.
        _sut.X.ShouldBe(123);
        _sut.Y.ShouldBe(456);
        _sut.Zoom.ShouldBe(2);
    }

    [Fact]
    public void SetFromMatrix_ShouldOverwritePreviouslySetCameraValues()
    {
        Camera _sut = new Camera();
        _sut.ClientWidth = 800;
        _sut.ClientHeight = 600;

        _sut.X = 999;
        _sut.Y = 888;
        _sut.Zoom = 5;

        // Use a matrix Gum itself would produce for (X=0, Y=0, Zoom=1) so we don't
        // hand-roll the formula and can assert the values were overwritten.
        Camera _source = new Camera();
        _source.ClientWidth = 800;
        _source.ClientHeight = 600;
        Matrix4x4 matrix = _source.GetTransformationMatrix();

        _sut.SetFromMatrix(matrix);

        _sut.X.ShouldBe(0);
        _sut.Y.ShouldBe(0);
        _sut.Zoom.ShouldBe(1);
    }

    [Fact]
    public void SetFromMatrix_ShouldRoundTripThroughGetTransformationMatrix_InDefaultMode()
    {
        Camera _sut = new Camera();
        _sut.ClientWidth = 800;
        _sut.ClientHeight = 600;
        _sut.X = 100;
        _sut.Y = 50;
        _sut.Zoom = 2;

        Matrix4x4 matrix = _sut.GetTransformationMatrix();

        Camera _roundTripped = new Camera();
        _roundTripped.ClientWidth = 800;
        _roundTripped.ClientHeight = 600;
        _roundTripped.SetFromMatrix(matrix);

        _roundTripped.X.ShouldBe(_sut.X);
        _roundTripped.Y.ShouldBe(_sut.Y);
        _roundTripped.Zoom.ShouldBe(_sut.Zoom);
    }

    [Fact]
    public void SetFromMatrix_ShouldRoundTripThroughGetTransformationMatrix_InCenterModeWithoutEffect()
    {
        // Center+!UsingEffect is the branch that bakes T(w/2, h/2) into the matrix.
        // UseBasicEffectRendering is static state, so restore it after the test.
        bool _previousUseBasicEffect = RenderingLibrary.Graphics.RendererSettings.UseBasicEffectRendering;
        try
        {
            RenderingLibrary.Graphics.RendererSettings.UseBasicEffectRendering = false;

            Camera _sut = new Camera();
            _sut.ClientWidth = 800;
            _sut.ClientHeight = 600;
            _sut.CameraCenterOnScreen = CameraCenterOnScreen.Center;
            _sut.X = 100;
            _sut.Y = 50;
            _sut.Zoom = 2;

            Matrix4x4 matrix = _sut.GetTransformationMatrix();

            Camera _roundTripped = new Camera();
            _roundTripped.ClientWidth = 800;
            _roundTripped.ClientHeight = 600;
            _roundTripped.CameraCenterOnScreen = CameraCenterOnScreen.Center;
            _roundTripped.SetFromMatrix(matrix);

            _roundTripped.X.ShouldBe(_sut.X);
            _roundTripped.Y.ShouldBe(_sut.Y);
            _roundTripped.Zoom.ShouldBe(_sut.Zoom);
        }
        finally
        {
            RenderingLibrary.Graphics.RendererSettings.UseBasicEffectRendering = _previousUseBasicEffect;
        }
    }

    [Fact]
    public void ScreenToWorld_ShouldRoundTripBackCorrectly()
    {
        Camera _sut = new Camera();
        _sut.ClientWidth = 800;
        _sut.ClientHeight = 600;

        _sut.ScreenToWorld(100, 150, out float worldX, out float worldY);

        _sut.WorldToScreen(worldX, worldY, out float screenX, out float screenY);

        screenX.ShouldBe(100);
        screenY.ShouldBe(150);
    }

    [Fact]
    public void ScreenToWorld_WithZoom_ShouldRoundTripBackCorrectly()
    {
        Camera _sut = new Camera();
        _sut.ClientWidth = 800;
        _sut.ClientHeight = 600;
        _sut.Zoom = 2;

        _sut.ScreenToWorld(100, 150, out float worldX, out float worldY);

        _sut.WorldToScreen(worldX, worldY, out float screenX, out float screenY);

        screenX.ShouldBe(100);
        screenY.ShouldBe(150);
    }

    [Fact]
    public void ScreenToWorld_WithClientLeftTop_ShouldRoundTripBackCorrectly()
    {
        Camera _sut = new Camera();
        _sut.ClientWidth = 800;
        _sut.ClientHeight = 600;
        _sut.ClientLeft = 50;
        _sut.ClientTop = 75;

        _sut.ScreenToWorld(100, 150, out float worldX, out float worldY);

        _sut.WorldToScreen(worldX, worldY, out float screenX, out float screenY);

        screenX.ShouldBe(100);
        screenY.ShouldBe(150);
    }
}
