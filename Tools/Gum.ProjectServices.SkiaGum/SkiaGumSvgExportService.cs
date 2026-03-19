using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.SvgExport;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using SkiaGum;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace Gum.ProjectServices.SkiaGum;

/// <summary>
/// Renders a Gum element to an SVG file using SkiaGum and <see cref="SKSvgCanvas"/>.
/// </summary>
/// <remarks>
/// Uses SkiaSharp's SVG canvas backend, which records drawing commands as SVG elements.
/// Bitmap content (sprites, textures) will be embedded as base64-encoded images in the SVG.
/// </remarks>
public class SkiaGumSvgExportService : ISvgExportService
{
    /// <inheritdoc/>
    public SvgExportResult ExportSvg(SvgExportRequest request)
    {
        try
        {
            string outputPath = Path.GetFullPath(request.OutputPath);
            string? outputDir = Path.GetDirectoryName(outputPath);
            if (outputDir != null)
            {
                Directory.CreateDirectory(outputDir);
            }

            // Initialize SkiaGum with a temporary bitmap canvas so we can load the
            // project, create the element, and run layout to measure its size.
            using (SKBitmap measureBitmap = new SKBitmap(1, 1))
            using (SKCanvas measureCanvas = new SKCanvas(measureBitmap))
            {
                GumService gumService = GumService.Default;
                gumService.Initialize(measureCanvas, request.ProjectPath);

                GumProjectSave? project = ObjectFinder.Self.GumProjectSave;

                if (project == null)
                {
                    return SvgExportResult.Failed($"Failed to load project: {request.ProjectPath}");
                }

                ElementSave? elementSave = project.AllElements
                    .FirstOrDefault(e => e.Name == request.ElementName);

                if (elementSave == null)
                {
                    return SvgExportResult.Failed(
                        $"Element '{request.ElementName}' not found in project.");
                }

                GraphicalUiElement element = elementSave.ToGraphicalUiElement(
                    SystemManagers.Default, addToManagers: false);
                element.AddToManagers(SystemManagers.Default);
                element.UpdateLayout();

                int width;
                int height;

                if (request.Width.HasValue || request.Height.HasValue)
                {
                    // Explicit overrides — use them (falling back to project canvas size).
                    width = request.Width ?? project.DefaultCanvasWidth;
                    height = request.Height ?? project.DefaultCanvasHeight;
                }
                else if (elementSave is ScreenSave)
                {
                    // Screens fill the canvas.
                    width = project.DefaultCanvasWidth;
                    height = project.DefaultCanvasHeight;
                }
                else
                {
                    // Components — use the element's actual laid-out size.
                    width = (int)Math.Ceiling(element.Width);
                    height = (int)Math.Ceiling(element.Height);
                }

                SKRect bounds = SKRect.Create(width, height);

                using (FileStream fileStream = File.Create(outputPath))
                {
                    using (SKCanvas svgCanvas = SKSvgCanvas.Create(bounds, fileStream))
                    {
                        // Swap in the SVG canvas for rendering.
                        SystemManagers.Default.Canvas = svgCanvas;

                        // For components, translate so the element renders at the SVG origin.
                        if (elementSave is not ScreenSave)
                        {
                            float offsetX = element.GetAbsoluteX();
                            float offsetY = element.GetAbsoluteY();
                            if (offsetX != 0 || offsetY != 0)
                            {
                                svgCanvas.Translate(-offsetX, -offsetY);
                            }
                        }

                        gumService.Draw();
                    }
                    // SVG document is finalized when svgCanvas is disposed
                }
            }

            return SvgExportResult.Succeeded(outputPath);
        }
        catch (Exception ex)
        {
            return SvgExportResult.Failed(ex.ToString());
        }
    }
}
