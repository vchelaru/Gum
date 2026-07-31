using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.ImageDiff;
using SkiaSharp;

namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Renders every Screen and Component in a project through two <see cref="IScreenshotService"/>
/// backends and diffs each pair via <see cref="PixelComparer"/>.
/// </summary>
/// <remarks>
/// Backend-agnostic: takes both <see cref="IScreenshotService"/> instances via
/// <see cref="ScreenshotDiffRequest"/> rather than referencing MonoGame/raylib concretely, so this
/// project needs no dependency on either runtime. <c>gumcli diff-screenshots</c> supplies the
/// concrete backends. Comparison decodes each backend's PNG through SkiaSharp purely for pixel
/// access — no SkiaGum rendering is involved.
/// </remarks>
public class ScreenshotDiffService : IScreenshotDiffService
{
    /// <inheritdoc/>
    public ScreenshotDiffResult Diff(ScreenshotDiffRequest request)
    {
        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(request.ProjectPath);

        if (!loadResult.Success)
        {
            throw new InvalidOperationException(loadResult.ErrorMessage ?? $"Failed to load project: {request.ProjectPath}");
        }

        GumProjectSave project = loadResult.Project!;
        string outputDirectory = request.OutputDirectory
            ?? Path.Combine(Path.GetTempPath(), "GumScreenshotDiff_" + Guid.NewGuid().ToString("N"));

        IEnumerable<ElementSave> elements = project.Screens.Cast<ElementSave>().Concat(project.Components);

        List<ElementScreenshotDiff> diffs = elements
            .Select(element => DiffElement(request, element.Name, outputDirectory))
            .ToList();

        return new ScreenshotDiffResult { ElementDiffs = diffs, OutputDirectory = outputDirectory };
    }

    private static ElementScreenshotDiff DiffElement(ScreenshotDiffRequest request, string elementName, string outputDirectory)
    {
        string pathA = Path.Combine(outputDirectory, "A", $"{elementName}.png");
        string pathB = Path.Combine(outputDirectory, "B", $"{elementName}.png");

        ScreenshotResult resultA = request.BackendA.TakeScreenshot(new ScreenshotRequest
        {
            ProjectPath = request.ProjectPath,
            ElementName = elementName,
            OutputPath = pathA,
        });

        ScreenshotResult resultB = request.BackendB.TakeScreenshot(new ScreenshotRequest
        {
            ProjectPath = request.ProjectPath,
            ElementName = elementName,
            OutputPath = pathB,
        });

        if (!resultA.Success || !resultB.Success)
        {
            string errorMessage = !resultA.Success ? resultA.ErrorMessage! : resultB.ErrorMessage!;
            return new ElementScreenshotDiff { ElementName = elementName, Matches = false, ErrorMessage = errorMessage };
        }

        using SKBitmap bitmapA = SKBitmap.Decode(resultA.OutputPath)
            ?? throw new InvalidOperationException($"Failed to decode '{resultA.OutputPath}'.");
        using SKBitmap bitmapB = SKBitmap.Decode(resultB.OutputPath)
            ?? throw new InvalidOperationException($"Failed to decode '{resultB.OutputPath}'.");

        ImageDiffResult diff = PixelComparer.CompareApproximate(bitmapA, bitmapB, request.Tolerance, request.ProximityRadius);

        return new ElementScreenshotDiff
        {
            ElementName = elementName,
            Matches = diff.Matches,
            BackendAPath = resultA.OutputPath,
            BackendBPath = resultB.OutputPath,
            MismatchedPixelCount = diff.MismatchedPixelCount,
            TotalPixelCount = diff.TotalPixelCount,
            MismatchPercentage = diff.MismatchPercentage,
            BoundingBoxMinX = diff.BoundingBoxMinX,
            BoundingBoxMinY = diff.BoundingBoxMinY,
            BoundingBoxMaxX = diff.BoundingBoxMaxX,
            BoundingBoxMaxY = diff.BoundingBoxMaxY,
            DimensionMismatchDescription = diff.DimensionMismatchDescription,
        };
    }
}
