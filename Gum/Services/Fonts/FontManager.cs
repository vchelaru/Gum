using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.FontGeneration;
using Gum.ToolStates;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Drawing;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Services.Fonts;

/// <summary>
/// Tool-facing font management service. Delegates all font generation to
/// <see cref="IHeadlessFontGenerationService"/> and wires tool-specific UI feedback
/// through <see cref="ToolFontGenerationCallbacks"/>.
/// </summary>
public class FontManager : IFontManager
{
    private readonly IFileCommands _fileCommands;
    private readonly IHeadlessFontGenerationService _fontGenerationService;
    private readonly IProjectState _projectState;

    /// <inheritdoc/>
    public string AbsoluteFontCacheFolder => _fileCommands.ProjectDirectory + "FontCache/";

    public FontManager(IFileCommands fileCommands,
        IProjectState projectState,
        IHeadlessFontGenerationService fontGenerationService)
    {
        _fileCommands = fileCommands;
        _projectState = projectState;
        _fontGenerationService = fontGenerationService;
    }

    /// <summary>
    /// Deletes the font cache folder for the current project.
    /// </summary>
    public void DeleteFontCacheFolder()
    {
        _fileCommands.DeleteDirectory(AbsoluteFontCacheFolder);
    }

    /// <summary>
    /// Creates all missing font files referenced by the project.
    /// </summary>
    public async Task CreateAllMissingFontFiles(GumProjectSave project, bool forceRecreate = false)
    {
        await _fontGenerationService.CreateAllMissingFontFiles(project, _fileCommands.ProjectDirectory.FullPath, forceRecreate);
    }

    /// <inheritdoc/>
    public void GenerateMissingFontsForReferencingElements(GumProjectSave gumProject,
        StateSave stateSave)
    {
        _fontGenerationService.GenerateMissingFontsForReferencingElements(
            gumProject, stateSave, _fileCommands.ProjectDirectory.FullPath);
    }

    /// <inheritdoc/>
    public void CreateFontIfNecessary(BmfcSave bmfcSave)
    {
        _fontGenerationService.CreateFontIfNecessary(bmfcSave,
            _fileCommands.ProjectDirectory.FullPath,
            _projectState.GumProjectSave?.AutoSizeFontOutputs ?? false);
    }

    /// <inheritdoc/>
    public BmfcSave? TryGetBmfcSaveFor(InstanceSave? instance, StateSave stateSave, string fontRanges,
        int spacingHorizontal, int spacingVertical, StateSave? forcedValues)
    {
        return _fontGenerationService.TryGetBmfcSaveFor(instance, stateSave, fontRanges,
            spacingHorizontal, spacingVertical, forcedValues);
    }

    /// <summary>
    /// Determines the smallest texture size that keeps the font on a single page.
    /// </summary>
    public async Task<GeneralResponse<Point>> GetOptimizedSizeFor(BmfcSave bmfcSave,
        bool forceMonoSpacedNumber, Action<string>? callback)
    {
        return await _fontGenerationService.GetOptimizedSizeFor(bmfcSave, forceMonoSpacedNumber, callback);
    }

    /// <summary>
    /// Generates the font to a temp directory and returns the number of texture pages it requires.
    /// </summary>
    public async Task<GeneralResponse<int>> GetPageCountFor(BmfcSave bmfcSave,
        bool forceMonoSpacedNumber, bool showSpinner, bool createTask)
    {
        return await _fontGenerationService.GetPageCountFor(bmfcSave, forceMonoSpacedNumber);
    }
}
