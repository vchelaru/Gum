using System.Numerics;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Issue #3460: a blurred dropshadow paints via <see cref="ShadowBlurRenderer"/>'s offscreen
/// <c>BeginTextureMode</c>/<c>EndTextureMode</c> passes. raylib's <c>EndTextureMode</c> resets the
/// modelview to identity and does NOT restore a previously-active <c>BeginMode2D</c> camera, so
/// before the fix the outer camera transform (zoom/pan) was clobbered for the shadow's own
/// composite and every draw after it in the same frame. The fix re-establishes the active camera
/// after the offscreen passes. Asserted by reading the rlgl modelview matrix directly (deterministic,
/// no flaky screen readback).
/// </summary>
public class ShadowBlurCameraRestoreTests : BaseTestClass
{
    [Fact]
    public void Draw_BlurredShadow_RestoresActiveCameraTransform()
    {
        Camera2D camera = new Camera2D
        {
            Zoom = 2f,
            Target = new Vector2(15f, 25f),
            Offset = Vector2.Zero,
            Rotation = 0f,
        };

        LineRectangle owner = new LineRectangle();
        Color tint = new Color((byte)0, (byte)0, (byte)0, (byte)255);
        Color silhouette = new Color((byte)255, (byte)255, (byte)255, (byte)255);

        BeginDrawing();
        BeginMode2D(camera);

        // Sanity: BeginMode2D must actually be observable in the rlgl modelview (zoom shows as the
        // x-scale). If this fails the observation point is wrong and the rest of the test is moot.
        Matrix4x4 beforeShadow = Rlgl.GetMatrixModelview();
        beforeShadow.M11.ShouldBe(2f);

        Renderer.Self.ShadowBlur.Draw(
            owner,
            0f, 0f, 40f, 40f,
            4f,
            tint,
            camera,
            (px, py) => DrawRectangleV(new Vector2(px, py), new Vector2(40f, 40f), silhouette));

        Matrix4x4 afterShadow = Rlgl.GetMatrixModelview();

        EndMode2D();
        EndDrawing();

        // The zoom-2 camera transform must survive the offscreen blur passes. Before the fix
        // EndTextureMode leaves the modelview at identity (M11 == 1) and this equality fails.
        afterShadow.ShouldBe(beforeShadow);
    }
}
