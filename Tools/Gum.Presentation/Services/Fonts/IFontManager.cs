using Gum.DataTypes;
using Gum.DataTypes.Variables;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Drawing;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Services.Fonts;

/// <summary>
/// Tool-facing font management interface. Extends <see cref="IRuntimeFontService"/> with
/// tool-specific operations (bulk generation, cache cleanup, texture size optimization).
///
/// On-demand font creation for individual property changes is handled by the shared code in
/// CustomSetPropertyOnRenderable.UpdateToFontValues via IRuntimeFontService.CreateFontIfNecessary.
/// This interface adds bulk operations like <see cref="CreateAllMissingFontFiles"/> that scan
/// the entire project.
/// </summary>
public interface IFontManager : IRuntimeFontService
{
    /// <summary>
    /// Clears the font cache for the current project. The folder itself is left in place so it
    /// can always be watched (#4259) - only its contents are deleted.
    /// </summary>
    void DeleteFontCacheFolder();

    /// <summary>
    /// Creates all missing font files referenced by the project and returns how many were generated.
    /// </summary>
    Task<int> CreateAllMissingFontFiles(GumProjectSave project, bool forceRecreate = false);

    /// <summary>
    /// Generates missing font files for all elements that recursively reference the element
    /// containing the given state. Called when a font property changes in the property grid.
    /// </summary>
    void GenerateMissingFontsForReferencingElements(GumProjectSave gumProject,
        StateSave stateSave);

    /// <summary>
    /// Determines the smallest texture size that keeps the font on a single page.
    /// </summary>
    Task<GeneralResponse<Point>> GetOptimizedSizeFor(BmfcSave bmfcSave,
        bool forceMonoSpacedNumber, Action<string>? callback);

    /// <summary>
    /// Generates the font to a temp directory and returns the number of texture pages it requires.
    /// </summary>
    Task<GeneralResponse<int>> GetPageCountFor(BmfcSave bmfcSave,
        bool forceMonoSpacedNumber, bool showSpinner, bool createTask);
}
