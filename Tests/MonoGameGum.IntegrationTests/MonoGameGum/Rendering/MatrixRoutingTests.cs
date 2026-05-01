using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using System;
using Xunit;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Regression tests for the matrix routing in <c>SpriteRenderer.BeginSpriteBatch</c>.
///
/// This area has historically been broken multiple times. Each break manifests the same
/// way: sprites and shapes don't render at the same screen position when
/// <c>Camera.Zoom</c> or <c>ForcedMatrix</c> is set. The shared invariant under test:
///
/// <code>
///   SpriteRenderer.CurrentTransformMatrix
///     == ForcedMatrix.HasValue ? ForcedMatrix * GetZoomAndMatrix(layer, camera)
///                              : GetZoomAndMatrix(layer, camera)
/// </code>
///
/// CurrentTransformMatrix is what shapes use as their <c>ShapeBatch.Begin(view:)</c>.
/// The sprite-side <c>basicEffect.View</c> composes the same way in the same method —
/// kept in sync by construction. If you change one composition line, change the other,
/// and these tests will tell you whether the two paths still agree.
///
/// See <c>.claude/skills/gum-monogame-rendering/SKILL.md</c> for the architectural
/// rationale and the past wrong turns these tests guard against.
/// </summary>
public class MatrixRoutingTests : BaseTestClass
{
    [Fact]
    public void CurrentTransformMatrix_IsIdentity_WhenZoomIsOneAndNoForcedMatrix()
    {
        using var game = new MinimalGame();
        game.RunOneFrame();

        var view = CaptureSpriteBatchTransform(zoom: 1f, cameraX: 0, cameraY: 0, forcedMatrix: null);

        AssertMatricesEqual(view, XnaMatrix.Identity);
    }

    [Fact]
    public void CurrentTransformMatrix_IncludesCameraZoom_WhenZoomIsNonOneAndNoForcedMatrix()
    {
        // Past regression: under UsingEffect=true, the SpriteBatch transformMatrix used to be
        // sourced from a layer-zoom-only matrix that ignored Camera.Zoom entirely. Sprites
        // (which read camera.Zoom via basicEffect.View) scaled, shapes (which read this
        // matrix via CurrentTransformMatrix) didn't. Visible drift at any non-1 Camera.Zoom.
        using var game = new MinimalGame();
        game.RunOneFrame();

        var view = CaptureSpriteBatchTransform(zoom: 2f, cameraX: 0, cameraY: 0, forcedMatrix: null);

        AssertMatricesEqual(view, XnaMatrix.CreateScale(2f, 2f, 1f));
    }

    [Fact]
    public void CurrentTransformMatrix_AppliesForcedMatrix_WhenZoomIsOne()
    {
        using var game = new MinimalGame();
        game.RunOneFrame();

        var forced = XnaMatrix.CreateScale(3f, 3f, 1f);
        var view = CaptureSpriteBatchTransform(zoom: 1f, cameraX: 0, cameraY: 0, forcedMatrix: forced);

        AssertMatricesEqual(view, forced);
    }

    [Fact]
    public void CurrentTransformMatrix_ComposesForcedMatrixWithCameraZoom_WhenBothSet()
    {
        // Compose semantics: ForcedMatrix layers ON TOP of the camera view, not as a
        // replacement. A consumer that sets both Camera.Zoom AND a scale-bearing ForcedMatrix
        // gets scale² applied — that's the consumer's bug to fix (pick one source). The
        // engine's job is to compose predictably so sprites and shapes stay aligned.
        //
        // Past regression #2589 ("replace" semantics): set basicEffect.View = ForcedMatrix
        // and dropped GetZoomAndMatrix entirely. Camera position/zoom disappeared for any
        // GumBatch consumer with ForcedMatrix set, breaking rendering wholesale.
        using var game = new MinimalGame();
        game.RunOneFrame();

        var forced = XnaMatrix.CreateScale(3f, 3f, 1f);
        var view = CaptureSpriteBatchTransform(zoom: 2f, cameraX: 0, cameraY: 0, forcedMatrix: forced);

        AssertMatricesEqual(view, forced * XnaMatrix.CreateScale(2f, 2f, 1f));
    }

    [Fact]
    public void CurrentTransformMatrix_IncludesCameraTranslation_WhenCameraIsScrolled()
    {
        // GetZoomAndMatrix (UsingEffect path) returns Translate(-x,-y) * Scale(zoom).
        // Belt-and-suspenders: ensure camera position contributes, not just zoom.
        using var game = new MinimalGame();
        game.RunOneFrame();

        var view = CaptureSpriteBatchTransform(zoom: 1f, cameraX: 100, cameraY: 50, forcedMatrix: null);

        AssertMatricesEqual(view, XnaMatrix.CreateTranslation(-100, -50, 0));
    }

    /// <summary>
    /// Begins a sprite batch with the given camera state and ForcedMatrix, captures the
    /// resulting <see cref="Graphics.SpriteRenderer.CurrentTransformMatrix"/>, and ends
    /// the batch. Returns the captured matrix (non-null — null would itself be a failure).
    /// </summary>
    private static XnaMatrix CaptureSpriteBatchTransform(float zoom, float cameraX, float cameraY, XnaMatrix? forcedMatrix)
    {
        var renderer = SystemManagers.Default.Renderer;
        renderer.Camera.Zoom = zoom;
        renderer.Camera.X = cameraX;
        renderer.Camera.Y = cameraY;

        renderer.Begin(spriteBatchMatrix: forcedMatrix);
        var captured = renderer.SpriteRenderer.CurrentTransformMatrix;
        renderer.End();

        captured.ShouldNotBeNull();
        return captured!.Value;
    }

    private static void AssertMatricesEqual(XnaMatrix actual, XnaMatrix expected, float tolerance = 1e-4f)
    {
        var a = ToArray(actual);
        var e = ToArray(expected);
        for (int i = 0; i < 16; i++)
        {
            Math.Abs(a[i] - e[i]).ShouldBeLessThan(tolerance,
                $"Matrix mismatch at element {i}.\nActual:\n{actual}\nExpected:\n{expected}");
        }
    }

    private static float[] ToArray(XnaMatrix m) => new[]
    {
        m.M11, m.M12, m.M13, m.M14,
        m.M21, m.M22, m.M23, m.M24,
        m.M31, m.M32, m.M33, m.M34,
        m.M41, m.M42, m.M43, m.M44,
    };

    /// <summary>
    /// Minimal Game host that initializes a fresh <see cref="GumService"/> per test
    /// (singleton <c>GumService.Default</c> can only be initialized once per process).
    /// We don't draw anything — we just need <c>BeginSpriteBatch</c> to be callable.
    /// </summary>
    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        public GumService GumService { get; }

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            GumService = new GumService();
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumService.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (GumService.IsInitialized)
                GumService.Uninitialize();
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
