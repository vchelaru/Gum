using Gum.DataTypes;
using Gum.ProjectServices.Screenshot;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using RenderingLibrary;
using System;
using System.IO;
using System.Linq;

namespace Gum.ProjectServices.MonoGame;

/// <summary>
/// Renders a Gum element to a PNG using MonoGame (DesktopGL / OpenGL).
/// </summary>
/// <remarks>
/// Runs a minimal <see cref="Game"/> instance with a 1×1 off-screen window so that MonoGame's
/// SDL2 layer initializes correctly, then initializes <see cref="GumService"/> exactly the same
/// way a real MonoGame game would. This ensures font rendering, texture loading, and layout
/// produce pixel-accurate output matching a live game.
/// </remarks>
public class MonoGameScreenshotService : IScreenshotService
{
    /// <inheritdoc/>
    public ScreenshotResult TakeScreenshot(ScreenshotRequest request)
    {
        using var game = new ScreenshotGame(request);
        game.Run();
        try
        {
            GumService.Default.Uninitialize();
        }
        catch
        {
            // best-effort cleanup
        }
        return game.Result;
    }

    private sealed class ScreenshotGame : Game
    {
        private readonly ScreenshotRequest _request;
        private readonly GraphicsDeviceManager _graphics;

        public ScreenshotResult Result { get; private set; } =
            ScreenshotResult.Failed("Screenshot did not complete.");

        public ScreenshotGame(ScreenshotRequest request)
        {
            _request = request;
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1,
                PreferredBackBufferHeight = 1,
            };
        }

        protected override void Initialize()
        {
            // Move window off-screen to avoid a visible flash
            Window.Position = new Point(-10000, -10000);

            base.Initialize();

            try
            {
                int width = _request.Width ?? 800;
                int height = _request.Height ?? 600;

                var gumService = GumService.Default;
                var project = gumService.Initialize(this, _request.ProjectPath);

                if (project == null)
                {
                    Result = ScreenshotResult.Failed($"Failed to load project: {_request.ProjectPath}");
                    return;
                }

                gumService.CanvasWidth = width;
                gumService.CanvasHeight = height;

                var elementSave = project.AllElements
                    .FirstOrDefault(e => e.Name == _request.ElementName);

                if (elementSave == null)
                {
                    Result = ScreenshotResult.Failed(
                        $"Element '{_request.ElementName}' not found in project.");
                    return;
                }

                var element = elementSave.ToGraphicalUiElement(SystemManagers.Default);
                element.AddToManagers(SystemManagers.Default);
                element.UpdateLayout();

                using var renderTarget = new RenderTarget2D(GraphicsDevice, width, height);
                GraphicsDevice.SetRenderTarget(renderTarget);
                GraphicsDevice.Clear(Color.Transparent);

                gumService.Draw();

                GraphicsDevice.SetRenderTarget(null);

                string outputPath = Path.GetFullPath(_request.OutputPath);
                string? outputDir = Path.GetDirectoryName(outputPath);
                if (outputDir != null)
                {
                    Directory.CreateDirectory(outputDir);
                }

                using var stream = File.OpenWrite(outputPath);
                renderTarget.SaveAsPng(stream, width, height);

                Result = ScreenshotResult.Succeeded(outputPath);
            }
            catch (Exception ex)
            {
                Result = ScreenshotResult.Failed(ex.ToString());
            }
            finally
            {
                Exit();
            }
        }
    }
}
