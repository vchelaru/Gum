using Gum.DataTypes;
using Gum.ProjectServices.Screenshot;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;
using MonoGameGum;
using RenderingLibrary;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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

                // Circle/rounded-rectangle rendering is Apos.Shapes-backed. Without this, those
                // shapes silently fall back to the renderable's construction defaults (transparent
                // fill, 1px white stroke) instead of the element's actual state - see #4403.
                if (!ShapeRenderer.Self.IsInitialized)
                {
                    ShapeRenderer.Self.Initialize();
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

                Color clearColor = _request.BackgroundColor is { } background
                    ? new Color(background.R, background.G, background.B, background.A)
                    : Color.Transparent;

                using var renderTarget = new RenderTarget2D(GraphicsDevice, width, height);
                GraphicsDevice.SetRenderTarget(renderTarget);
                GraphicsDevice.Clear(clearColor);

                gumService.Draw();

                GraphicsDevice.SetRenderTarget(null);

                string outputPath = Path.GetFullPath(_request.OutputPath);
                string? outputDir = Path.GetDirectoryName(outputPath);
                if (outputDir != null)
                {
                    Directory.CreateDirectory(outputDir);
                }

                using var stream = File.OpenWrite(outputPath);

                if (_request.BackgroundColor.HasValue)
                {
                    // Translucent content blended onto the opaque clear color above does not
                    // itself flatten the render target's own alpha channel back to 255 - force it
                    // so the exported PNG is genuinely opaque, not just visually opaque against
                    // the background it happened to be rendered with.
                    Color[] pixels = new Color[width * height];
                    renderTarget.GetData(pixels);
                    ScreenshotAlphaFlattener.FlattenToOpaque(MemoryMarshal.AsBytes(pixels.AsSpan()));

                    using var flattenedTexture = new Texture2D(GraphicsDevice, width, height);
                    flattenedTexture.SetData(pixels);
                    flattenedTexture.SaveAsPng(stream, width, height);
                }
                else
                {
                    renderTarget.SaveAsPng(stream, width, height);
                }

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
