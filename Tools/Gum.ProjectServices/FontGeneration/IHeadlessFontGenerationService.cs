using Gum.DataTypes;
using Gum.DataTypes.Variables;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Drawing;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.ProjectServices.FontGeneration;

/// <summary>
/// Headless font generation service that creates bitmap font files using bmfont.exe.
/// Windows-only; throws <see cref="PlatformNotSupportedException"/> on non-Windows platforms.
/// </summary>
public interface IHeadlessFontGenerationService
{
    /// <summary>
    /// Creates all missing font files referenced by the project and returns how many were generated.
    /// </summary>
    Task<int> CreateAllMissingFontFiles(GumProjectSave project, string projectDirectory, bool forceRecreate = false);

    /// <summary>
    /// Generates missing font files for all elements that recursively reference the element
    /// containing the given state. Called when a font property changes in the property grid.
    /// </summary>
    void GenerateMissingFontsForReferencingElements(GumProjectSave gumProject,
        StateSave stateSave, string projectDirectory);

    /// <summary>
    /// Synchronously creates a single font file if it does not already exist in the project directory.
    /// Intended for use from synchronous code paths such as property setting. Reports
    /// <see cref="FontFileStatus.Generating"/> instead of waiting when a bulk pass is already
    /// producing that file.
    /// </summary>
    FontFileStatus CreateFontIfNecessary(BmfcSave bmfcSave, string projectDirectory, bool autoSizeFontOutputs);

    /// <summary>
    /// Determines the smallest texture size that keeps the font on a single page.
    /// </summary>
    Task<GeneralResponse<Point>> GetOptimizedSizeFor(BmfcSave bmfcSave, bool forceMonoSpacedNumber, Action<string>? callback);

    /// <summary>
    /// Generates the font to a temp directory and returns the number of texture pages it requires.
    /// </summary>
    Task<GeneralResponse<int>> GetPageCountFor(BmfcSave bmfcSave, bool forceMonoSpacedNumber);
}
