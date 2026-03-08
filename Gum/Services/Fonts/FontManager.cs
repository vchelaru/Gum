using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.ProjectServices.FontGeneration;
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
public class FontManager
{
    private readonly IFileCommands _fileCommands;
    private readonly IHeadlessFontGenerationService _fontGenerationService;

    /// <summary>
    /// The absolute path to the font cache folder for the currently loaded project.
    /// </summary>
    public string AbsoluteFontCacheFolder => _fileCommands.ProjectDirectory + "FontCache/";

    public FontManager(IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IFileWatchManager fileWatchManager)
    {
        _fileCommands = fileCommands;

        ToolFontGenerationCallbacks callbacks = new ToolFontGenerationCallbacks(guiCommands, fileWatchManager);
        _fontGenerationService = new HeadlessFontGenerationService(callbacks);
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

    /// <summary>
    /// Creates fonts referenced by the changed instance and propagates to dependent elements.
    /// </summary>
    internal void ReactToFontValueSet(InstanceSave instance, GumProjectSave gumProject,
        StateSave stateSave, StateSave forcedValues)
    {
        _fontGenerationService.ReactToFontValueSet(instance, gumProject, stateSave, forcedValues,
            _fileCommands.ProjectDirectory.FullPath);
    }

    /// <summary>
    /// Builds a <see cref="BmfcSave"/> describing the font for the given instance/state,
    /// or <c>null</c> if no font is configured.
    /// </summary>
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
