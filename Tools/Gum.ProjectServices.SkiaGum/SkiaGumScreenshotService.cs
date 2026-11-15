using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.Screenshot;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using SkiaSharp;
using System;
using System.IO;
using ToolsUtilities;

namespace Gum.ProjectServices.SkiaGum;

/// <summary>
/// Renders a Gum element to a PNG using SkiaGum on a CPU raster surface.
/// </summary>
/// <remarks>
/// For games that draw Gum with Skia (#7965). Follows the same steps as the MonoGame and raylib
/// services: load the project, instantiate the element, run layout at the requested canvas size,
/// draw once.
/// </remarks>
public class SkiaGumScreenshotService : IScreenshotService
{
    /// <inheritdoc/>
    public ScreenshotResult TakeScreenshot(ScreenshotRequest request)
    {
        // Initializing SkiaGum defaults MissingFileBehavior to ThrowException (#6889). Like the SVG export,
        // a screenshot still draws the rest of an element whose image is missing.
        // The raw value: restoring the resolved one would leave it assigned, and a later Initialize in this
        // process would no longer apply its ThrowException default (#7963).
        MissingFileBehavior? previousMissingFileBehavior = GraphicalUiElement.AssignedMissingFileBehavior;
        GumService gumService = GumService.Default;
        try
        {
            // The project's canvas size is only known after loading, so initialize at 1x1 and resize.
            using SKBitmap measureBitmap = new SKBitmap(1, 1);
            using SKCanvas measureCanvas = new SKCanvas(measureBitmap);
            gumService.Initialize(measureCanvas, request.ProjectPath);
            GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;

            GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
            if (project == null)
            {
                return ScreenshotResult.Failed($"Failed to load project: {request.ProjectPath}");
            }

            ElementSave? elementSave = request.FindElement(project);
            if (elementSave == null)
            {
                return ScreenshotResult.ElementNotFound(request.ElementName);
            }

            (int width, int height) = request.ResolveSize(project);
            gumService.HandleResize(width, height);

            GraphicalUiElement element = elementSave.ToGraphicalUiElement(
                SystemManagers.Default, addToManagers: false);
            element.AddToManagers(SystemManagers.Default);
            element.UpdateLayout();

            SKImageInfo info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using SKBitmap bitmap = new SKBitmap(info);
            using (SKCanvas canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(request.BackgroundColor is { } background
                    ? new SKColor(background.R, background.G, background.B, background.A)
                    : SKColors.Transparent);

                gumService.Draw(canvas);
            }

            if (request.BackgroundColor.HasValue)
            {
                ScreenshotAlphaFlattener.FlattenToOpaque(bitmap.GetPixelSpan());
            }

            string outputPath = request.PrepareOutputPath();
            using SKData png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            using FileStream stream = File.Create(outputPath);
            png.SaveTo(stream);

            return ScreenshotResult.Succeeded(outputPath);
        }
        catch (Exception ex)
        {
            return ScreenshotResult.Failed(ex.ToString());
        }
        finally
        {
            try
            {
                gumService.Uninitialize();
            }
            catch
            {
                // best-effort cleanup
            }

            GraphicalUiElement.AssignedMissingFileBehavior = previousMissingFileBehavior;
        }
    }
}
