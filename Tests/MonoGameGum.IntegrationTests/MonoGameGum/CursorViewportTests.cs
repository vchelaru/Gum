using Gum.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Pins <see cref="Cursor.XRespectingGumZoomAndBounds"/> and the Y version when a real
/// GraphicsDevice exists: the viewport's top-left is subtracted before the camera zoom and position
/// are applied. The headless case (no GraphicsDevice) is in MonoGameGum.Tests.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class CursorViewportTests : BaseTestClass
{
    [Fact]
    public void RespectingGumZoomAndBounds_WithOffsetViewport_SubtractsViewportThenAppliesZoomAndCamera()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Viewport originalViewport = game.GraphicsDevice.Viewport;
        Camera camera = SystemManagers.Default.Renderer.Camera;
        float originalZoom = camera.Zoom;
        float originalX = camera.X;
        float originalY = camera.Y;
        try
        {
            game.GraphicsDevice.Viewport = new Viewport(10, 6, 300, 200);
            camera.Zoom = 2;
            camera.X = 7;
            camera.Y = 3;
            Cursor cursor = new((GameWindow?)null);

            cursor.Activity(0, new MouseState(50, 30, 0, ButtonState.Released, ButtonState.Released,
                ButtonState.Released, ButtonState.Released, ButtonState.Released), new TouchCollection());

            // (50 - 10) / 2 + 7 and (30 - 6) / 2 + 3
            cursor.XRespectingGumZoomAndBounds().ShouldBe(27);
            cursor.YRespectingGumZoomAndBounds().ShouldBe(15);
        }
        finally
        {
            game.GraphicsDevice.Viewport = originalViewport;
            camera.Zoom = originalZoom;
            camera.X = originalX;
            camera.Y = originalY;
        }
    }

    private sealed class MinimalGame : SharedGameLease
    {
        public Gum.GumService GumService { get; } = new Gum.GumService();

        protected override void OnInitialize() => GumService.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }
    }
}
