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
    /// Creates all missing font files referenced by the project.
    /// </summary>
    Task CreateAllMissingFontFiles(GumProjectSave project, string projectDirectory, bool forceRecreate = false);

    /// <summary>
    /// Creates fonts referenced by the changed instance and its dependents.
    /// </summary>
    void ReactToFontValueSet(InstanceSave instance, GumProjectSave gumProject, StateSave stateSave, StateSave forcedValues, string projectDirectory);

    /// <summary>
    /// Builds a <see cref="BmfcSave"/> describing the font for the given instance/state,
    /// or <c>null</c> if no font is configured.
    /// </summary>
    BmfcSave? TryGetBmfcSaveFor(InstanceSave? instance, StateSave stateSave, string fontRanges,
        int spacingHorizontal, int spacingVertical, StateSave? forcedValues);

    /// <summary>
    /// Determines the smallest texture size that keeps the font on a single page.
    /// </summary>
    Task<GeneralResponse<Point>> GetOptimizedSizeFor(BmfcSave bmfcSave, bool forceMonoSpacedNumber, Action<string>? callback);

    /// <summary>
    /// Generates the font to a temp directory and returns the number of texture pages it requires.
    /// </summary>
    Task<GeneralResponse<int>> GetPageCountFor(BmfcSave bmfcSave, bool forceMonoSpacedNumber);
}
