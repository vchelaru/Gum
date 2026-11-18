using Gum.DataTypes;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Parameters for a screenshot render operation.
/// </summary>
public class ScreenshotRequest
{
    /// <summary>
    /// Absolute path to the .gumx project file.
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// Name of the Screen or Component to render.
    /// </summary>
    public required string ElementName { get; init; }

    /// <summary>
    /// Absolute or relative path for the output PNG file.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Width of the output image in pixels. Defaults to the project canvas width.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Height of the output image in pixels. Defaults to the project canvas height.
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// Background color to clear to before rendering. Defaults to fully transparent when null.
    /// </summary>
    public ScreenshotColor? BackgroundColor { get; init; }

    /// <summary>
    /// Variable values set on the rendered element after it loads and before it is laid out and drawn. Nothing is
    /// saved to the project.
    /// </summary>
    public IReadOnlyList<ScreenshotVariableOverride> VariableOverrides { get; init; } = Array.Empty<ScreenshotVariableOverride>();

    /// <summary>
    /// Applies <see cref="VariableOverrides"/> to <paramref name="element"/>, built from <paramref name="elementSave"/>.
    /// Returns why one could not apply, or null when all applied.
    /// </summary>
    public string? ApplyVariableOverrides(ElementSave elementSave, GraphicalUiElement element) =>
        ScreenshotVariableOverride.ApplyAll(VariableOverrides, elementSave, element);

    /// <summary>
    /// The size to render at: <see cref="Width"/>/<see cref="Height"/> when set, otherwise the
    /// project's canvas size, otherwise 800x600.
    /// </summary>
    public (int Width, int Height) ResolveSize(GumProjectSave project)
    {
        int width = Width ?? (project.DefaultCanvasWidth > 0 ? project.DefaultCanvasWidth : 800);
        int height = Height ?? (project.DefaultCanvasHeight > 0 ? project.DefaultCanvasHeight : 600);
        return (width, height);
    }

    /// <summary>
    /// The project's Screen or Component named <see cref="ElementName"/>, or null when there is none.
    /// </summary>
    public ElementSave? FindElement(GumProjectSave project) =>
        project.AllElements.FirstOrDefault(e => e.Name == ElementName);

    /// <summary>
    /// Resolves <see cref="OutputPath"/> to a full path and creates its folder.
    /// </summary>
    public string PrepareOutputPath()
    {
        string outputPath = Path.GetFullPath(OutputPath);
        string? outputDir = Path.GetDirectoryName(outputPath);
        if (outputDir != null)
        {
            Directory.CreateDirectory(outputDir);
        }
        return outputPath;
    }
}
