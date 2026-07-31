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
/// same project — the whole point of adding this backend (#4174). Reuses the current process'
/// hidden raylib window if one is already open (e.g. from a previous call in the same process)
/// rather than closing/reopening it, since raylib window re-creation is unreliable — see
/// RaylibGum.Tests' TestAssemblyInitialize.
/// </remarks>
public class RaylibScreenshotService : IScreenshotService
{
    /// <inheritdoc/>
    public ScreenshotResult TakeScreenshot(ScreenshotRequest request)
    {
        try
        {
            if (!Raylib_cs.Raylib.IsWindowReady())
            {
                Raylib_cs.Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
                Raylib_cs.Raylib.InitWindow(1, 1, "Gum Screenshot");
            }

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

            var element = elementSave.ToGraphicalUiElement(SystemManagers.Default);
            element.AddToManagers(SystemManagers.Default);
            element.UpdateLayout();

            RenderTexture2D renderTexture = Raylib_cs.Raylib.LoadRenderTexture(width, height);
            try
            {
                Raylib_cs.Raylib.BeginTextureMode(renderTexture);
                Raylib_cs.Raylib.ClearBackground(new Color((byte)0, (byte)0, (byte)0, (byte)0));
                gumService.Draw();
                Raylib_cs.Raylib.EndTextureMode();

                Image image = Raylib_cs.Raylib.LoadImageFromTexture(renderTexture.Texture);
                try
                {
                    // Render textures are stored bottom-up in GL; flip so the exported PNG reads
                    // right-side-up like any other screenshot.
                    Raylib_cs.Raylib.ImageFlipVertical(ref image);

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
        }
    }
}
