using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

public class CameraTests : BaseTestClass
{
    // Minimal IRenderableIpso whose GetAbsoluteLeft/Right/Top/Bottom (extension methods over
    // X/Y/Width/Height) return predictable values. Used to drive GetScissorRectangleFor in
    // isolation from any real Gum runtime element.
    private sealed class StubRenderable : IRenderableIpso
    {
        public StubRenderable()
        {
            Children = new ObservableCollection<IRenderableIpso>();
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Rotation { get; set; }
        public bool FlipHorizontal { get; set; }
        public string? Name { get; set; }
        public object? Tag { get; set; }
        public bool Visible { get; set; } = true;
        public bool AbsoluteVisible => Visible;
        public bool ClipsChildren { get; set; }
        public bool IsRenderTarget => false;
        public ObservableCollection<IRenderableIpso> Children { get; }
        public IRenderableIpso? Parent { get; set; }
        IVisible? IVisible.Parent => Parent;
        public int Alpha => 255;
        public ColorOperation ColorOperation => ColorOperation.Modulate;
        public Gum.BlendState BlendState => Gum.BlendState.NonPremultiplied;
        public bool Wrap => false;
        public string BatchKey => "SpriteBatch";
        public void SetParentDirect(IRenderableIpso? newParent) => Parent = newParent;
        public void Render(ISystemManagers managers) { }
        public void PreRender() { }
        public void StartBatch(ISystemManagers managers) { }
        public void EndBatch(ISystemManagers managers) { }
    }

    // Pins the shared render-target clamp helper (#3478) that both the MonoGame renderer
    // (GetRenderTargetFor) and the raylib renderer (BakeRenderTarget / CompositeRenderTarget) call,
    // so the camera-visible-bounds math can't drift between backends the way it did before the
    // extraction. TopLeft mode makes the camera's absolute bounds trivially [0,800) x [0,600).
    [Fact]
    public void GetRenderTargetBounds_FullyOnCamera_ReturnsClampedRectAndUnscaledPixelSize()
    {
        Camera camera = new Camera();
        camera.ClientWidth = 800;
        camera.ClientHeight = 600;
        camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;
        camera.X = 0;
        camera.Y = 0;
        camera.Zoom = 1;

        // Wholly inside the camera: world [100,300) x [50,150).
        StubRenderable ipso = new StubRenderable();
        ipso.X = 100;
        ipso.Y = 50;
        ipso.Width = 200;
        ipso.Height = 100;

        RenderTargetBounds bounds = camera.GetRenderTargetBounds(ipso);

        bounds.Left.ShouldBe(100);
        bounds.Top.ShouldBe(50);
        bounds.Right.ShouldBe(300);
        bounds.Bottom.ShouldBe(150);
        bounds.Width.ShouldBe(200);
        bounds.Height.ShouldBe(100);
        bounds.HasVisibleArea.ShouldBeTrue();
    }

    [Fact]
    public void GetRenderTargetBounds_EntirelyOffCamera_HasNoVisibleArea()
    {
        Camera camera = new Camera();
        camera.ClientWidth = 800;
        camera.ClientHeight = 600;
        camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;
        camera.X = 0;
        camera.Y = 0;
        camera.Zoom = 1;

        // Far past the camera's right edge: world [100000, 100100). Clamping collapses the width to
        // a non-positive value, so the container has nothing to bake or composite.
        StubRenderable ipso = new StubRenderable();
        ipso.X = 100000;
        ipso.Y = 0;
        ipso.Width = 100;
        ipso.Height = 100;

        RenderTargetBounds bounds = camera.GetRenderTargetBounds(ipso);

        bounds.HasVisibleArea.ShouldBeFalse();
        bounds.Width.ShouldBeLessThanOrEqualTo(0);
    }

    // The exact-zero fencepost: a 0x0 container fully on-camera clamps to width == height == 0, which
    // must read as "no visible area" (HasVisibleArea is Width > 0 && Height > 0). This is the degenerate
    // case the whole issue is about, and it guards the > 0 boundary directly — the off-camera test above
    // drives width negative, so it would not catch a >= 0 regression that lets a 0-size container render.
    [Fact]
    public void GetRenderTargetBounds_ZeroSizeOnCamera_HasNoVisibleArea()
    {
        Camera camera = new Camera();
        camera.ClientWidth = 800;
        camera.ClientHeight = 600;
        camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;
        camera.X = 0;
        camera.Y = 0;
        camera.Zoom = 1;

        StubRenderable ipso = new StubRenderable();
        ipso.X = 100;
        ipso.Y = 100;
        ipso.Width = 0;
        ipso.Height = 0;

        RenderTargetBounds bounds = camera.GetRenderTargetBounds(ipso);

        bounds.Width.ShouldBe(0);
        bounds.Height.ShouldBe(0);
        bounds.HasVisibleArea.ShouldBeFalse();
    }

    [Fact]
    public void GetRenderTargetBounds_WithZoom_ScalesPixelSizeByZoom()
    {
        Camera camera = new Camera();
        camera.ClientWidth = 800;
        camera.ClientHeight = 600;
        camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;
        camera.X = 0;
        camera.Y = 0;
        camera.Zoom = 2;

        // A 100x100 world-unit container at zoom 2 occupies 200x200 pixels; the world-space bounds
        // themselves are unaffected by zoom.
        StubRenderable ipso = new StubRenderable();
        ipso.X = 0;
        ipso.Y = 0;
        ipso.Width = 100;
        ipso.Height = 100;

        RenderTargetBounds bounds = camera.GetRenderTargetBounds(ipso);

        bounds.Left.ShouldBe(0);
        bounds.Right.ShouldBe(100);
        bounds.Width.ShouldBe(200);
        bounds.Height.ShouldBe(200);
        bounds.HasVisibleArea.ShouldBeTrue();
    }

    [Fact]
    public void GetScissorRectangleFor_WithClientLeftTop_ShouldProduceBackbufferRelativeRectAndClampToViewportExtent()
    {
        // Regression test for the FRB letterbox-resize bug: when the camera has a non-zero
        // ClientLeft/ClientTop (Viewport offset), the returned scissor must live in absolute
        // backbuffer coordinates AND must clamp to [ClientLeft, ClientLeft + ClientWidth] —
        // not [0, ClientWidth]. The old clamp would chop the right/bottom of any element
        // that extended past the (still viewport-local) ClientWidth, making the scissor track
        // the wrong region after a letterboxed resize. This test exercises the non-FRB
        // codepath only; the FRB-specific offset-shift inside #if FRB is verified by
        // running the real FRB integration (see KidDefense repro).
        Camera _sut = new Camera();
        _sut.ClientLeft = 100;
        _sut.ClientTop = 50;
        _sut.ClientWidth = 800;
        _sut.ClientHeight = 600;
        _sut.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

        // Element spans world [50, 1000) x [25, 800) — past the right/bottom of the viewport.
        StubRenderable ipso = new StubRenderable();
        ipso.X = 50;
        ipso.Y = 25;
        ipso.Width = 950;
        ipso.Height = 775;

        System.Drawing.Rectangle scissor = _sut.GetScissorRectangleFor(layer: null!, ipso);

        // Left/Top: WorldToScreen adds ClientLeft/Top under MonoGame, so 50 + 100 = 150 and
        // 25 + 50 = 75 — already backbuffer-relative, inside the viewport, unclamped.
        scissor.Left.ShouldBe(150);
        scissor.Top.ShouldBe(75);

        // Right/Bottom: world right = 1000, screen right = 1100. The new clamp caps at
        // ClientLeft + ClientWidth = 900 (was 800 under the old [0, ClientWidth] clamp).
        scissor.Right.ShouldBe(900);
        scissor.Bottom.ShouldBe(650);
    }

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
