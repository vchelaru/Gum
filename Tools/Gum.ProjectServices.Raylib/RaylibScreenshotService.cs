using Gum.DataTypes;
using Gum.ProjectServices.Screenshot;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary;
using System;
using System.IO;
using System.Linq;

namespace Gum.ProjectServices.Raylib;

/// <summary>
/// Renders a Gum element to a PNG using raylib (OpenGL).
/// </summary>
/// <remarks>
/// Mirrors <c>MonoGameScreenshotService</c>'s render pipeline (load project, instantiate the
/// element, run layout, draw once) so the two backends can be diffed pixel-for-pixel against the
/// same project — the whole point of adding this backend (#4174).
///
/// <para>Creates and closes its own hidden window on every call rather than reusing one already
/// open in the process. An earlier version reused an existing window (matching RaylibGum.Tests'
/// persistent-window design), but <c>gumcli diff-screenshots</c> (#4174) calls this and
/// <c>MonoGameScreenshotService</c> back-to-back for every element — MonoGame's DesktopGL
/// teardown between calls left the reused raylib window's OpenGL context no longer valid, so a
/// second raylib render in the same process silently produced a corrupt image
/// (<c>ExportImage</c> failing). Owning a fresh window per call sidesteps that: it matches what a
/// real one-shot <c>gumcli screenshot --backend raylib</c> process already does (open, render,
/// process exits) with no added cost there, and gives every render its own valid GL context
/// regardless of what other graphics backends ran earlier in the process.</para>
/// </remarks>
public class RaylibScreenshotService : IScreenshotService
{
    /// <inheritdoc/>
    public ScreenshotResult TakeScreenshot(ScreenshotRequest request)
    {
        Raylib_cs.Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
        Raylib_cs.Raylib.InitWindow(1, 1, "Gum Screenshot");
        try
        {
            var gumService = GumService.Default;
            GumProjectSave? project = gumService.Initialize(request.ProjectPath);

            if (project == null)
            {
                return ScreenshotResult.Failed($"Failed to load project: {request.ProjectPath}");
            }

            ElementSave? elementSave = project.AllElements
                .FirstOrDefault(e => e.Name == request.ElementName);

            if (elementSave == null)
            {
                return ScreenshotResult.Failed(
                    $"Element '{request.ElementName}' not found in project.");
            }

            // Matches MonoGameScreenshotService's fallback exactly (800x600) rather than the
            // project's own canvas size, so the two backends stay directly diffable when Width/
            // Height are omitted. See #4174 for the follow-up to make both honor canvas size.
            int width = request.Width ?? 800;
            int height = request.Height ?? 600;

            // Must be set before UpdateLayout: a parentless element's PixelsFromMiddle/Percentage
            // positioning resolves against these, not against the render texture's actual size.
            // MonoGameScreenshotService gets this for free because its GraphicsDeviceManager backs
            // straight onto the real backbuffer; raylib always renders into an explicitly-sized
            // off-screen RenderTexture2D below, so nothing else tells the layout engine that size.
            gumService.CanvasWidth = width;
            gumService.CanvasHeight = height;

            var element = elementSave.ToGraphicalUiElement(SystemManagers.Default);
            element.AddToManagers(SystemManagers.Default);
            element.UpdateLayout();

            Color clearColor = request.BackgroundColor is { } background
                ? new Color(background.R, background.G, background.B, background.A)
                : new Color((byte)0, (byte)0, (byte)0, (byte)0);

            RenderTexture2D renderTexture = Raylib_cs.Raylib.LoadRenderTexture(width, height);
            try
            {
                Raylib_cs.Raylib.BeginTextureMode(renderTexture);
                Raylib_cs.Raylib.ClearBackground(clearColor);
                gumService.Draw();
                Raylib_cs.Raylib.EndTextureMode();

                Image image = Raylib_cs.Raylib.LoadImageFromTexture(renderTexture.Texture);
                try
                {
                    // Render textures are stored bottom-up in GL; flip so the exported PNG reads
                    // right-side-up like any other screenshot.
                    Raylib_cs.Raylib.ImageFlipVertical(ref image);

                    if (request.BackgroundColor.HasValue)
                    {
                        // Translucent content blended onto the opaque clear color above does not
                        // itself flatten the render target's own alpha channel back to 255 - force
                        // it so the exported PNG is genuinely opaque, not just visually opaque
                        // against the background it happened to be rendered with.
                        unsafe
                        {
                            var pixelBytes = new Span<byte>((void*)image.Data, image.Width * image.Height * 4);
                            ScreenshotAlphaFlattener.FlattenToOpaque(pixelBytes);
                        }
                    }

                    string outputPath = Path.GetFullPath(request.OutputPath);
                    string? outputDir = Path.GetDirectoryName(outputPath);
                    if (outputDir != null)
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    bool exported = Raylib_cs.Raylib.ExportImage(image, outputPath);
                    if (!exported)
                    {
                        return ScreenshotResult.Failed($"Failed to export image to '{outputPath}'.");
                    }

                    return ScreenshotResult.Succeeded(outputPath);
                }
                finally
                {
                    Raylib_cs.Raylib.UnloadImage(image);
                }
            }
            finally
            {
                Raylib_cs.Raylib.UnloadRenderTexture(renderTexture);
            }
        }
        catch (Exception ex)
        {
            return ScreenshotResult.Failed(ex.ToString());
        }
        finally
        {
            try
            {
                GumService.Default.Uninitialize();
            }
            catch
            {
                // best-effort cleanup
            }

            Raylib_cs.Raylib.CloseWindow();
        }
    }
}
