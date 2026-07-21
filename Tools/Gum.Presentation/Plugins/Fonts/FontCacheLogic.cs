using Gum.DataTypes;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Gum.Plugins.Fonts;

/// <summary>
/// Font-cache business logic relocated out of the WPF-hosted <c>MainFontPlugin</c> (ADR-0005 Phase
/// 3) so it can be unit tested headlessly. Menu wiring, and the "Clear Font Cache" handler (which
/// reads its own <c>RoutedEventArgs</c> parameter rather than the caught exception - a pre-existing
/// quirk left unchanged), stay on the plugin.
/// </summary>
public class FontCacheLogic
{
    private readonly IFontManager _fontManager;
    private readonly IDialogService _dialogService;
    private readonly IProjectState _projectState;

    public FontCacheLogic(IFontManager fontManager, IDialogService dialogService, IProjectState projectState)
    {
        _fontManager = fontManager;
        _dialogService = dialogService;
        _projectState = projectState;
    }

    /// <summary>
    /// Creates any missing font files for the just-loaded project.
    /// </summary>
    public Task CreateMissingFontFilesForLoadedProject() =>
        _fontManager.CreateAllMissingFontFiles(_projectState.GumProjectSave);

    /// <summary>
    /// Returns the font cache folder path, creating it first if it doesn't already exist.
    /// </summary>
    public string GetOrCreateFontCacheFolder()
    {
        if (!Directory.Exists(_fontManager.AbsoluteFontCacheFolder))
        {
            Directory.CreateDirectory(_fontManager.AbsoluteFontCacheFolder);
        }

        return _fontManager.AbsoluteFontCacheFolder;
    }

    /// <summary>
    /// Re-creates missing (or, if <paramref name="forceRecreate"/>, all) font files for the loaded
    /// project. Shows a message instead if no project is loaded.
    /// </summary>
    public async Task RefreshFontCache(bool forceRecreate)
    {
        GumProjectSave? gumProjectSave = _projectState.GumProjectSave;
        if (gumProjectSave == null)
        {
            _dialogService.ShowMessage(
                "A Gum project must first be loaded before recreating font files");
        }
        else
        {
            DateTime before = DateTime.Now;
            await _fontManager.CreateAllMissingFontFiles(gumProjectSave, forceRecreate: forceRecreate);
            DateTime after = DateTime.Now;

            TimeSpan difference = after - before;
            Debug.WriteLine($"Total time: {difference.TotalMilliseconds:N0}");
        }
    }
}
