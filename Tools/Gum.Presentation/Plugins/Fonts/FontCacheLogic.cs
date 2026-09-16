using Gum.Commands;
using Gum.DataTypes;
using Gum.Services;
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
    private readonly IDispatcher _dispatcher;
    private readonly IWireframeCommands _wireframeCommands;

    public FontCacheLogic(IFontManager fontManager, IDialogService dialogService, IProjectState projectState,
        IDispatcher dispatcher, IWireframeCommands wireframeCommands)
    {
        _fontManager = fontManager;
        _dialogService = dialogService;
        _projectState = projectState;
        _dispatcher = dispatcher;
        _wireframeCommands = wireframeCommands;
    }

    /// <summary>
    /// Queues creation of any missing font files for the loaded project to run after the caller
    /// returns, rather than on the project-load path. Scanning the project for required fonts costs
    /// hundreds of milliseconds and, in the normal case, finds every font already cached - so it is
    /// pre-generation, not a prerequisite: a font that really is missing is still created on demand
    /// when text using it renders (see <c>CustomSetPropertyOnRenderable.UpdateToFontValues</c>).
    /// The scan reads the current project when it runs, so a project loaded in the meantime is the
    /// one scanned.
    /// </summary>
    public void ScheduleMissingFontCreationForLoadedProject()
    {
        _dispatcher.Post(async () => await CreateMissingFontFilesForLoadedProject());
    }

    /// <summary>
    /// Creates any missing font files for the loaded project. Prefer
    /// <see cref="ScheduleMissingFontCreationForLoadedProject"/> on the project-load path.
    /// </summary>
    public async Task CreateMissingFontFilesForLoadedProject()
    {
        GumProjectSave? gumProjectSave = _projectState.GumProjectSave;
        if (gumProjectSave == null)
        {
            return;
        }

        using var _ = Gum.Diagnostics.StartupTiming.Time("FontCacheLogic.CreateMissingFontFilesForLoadedProject (total)");
        int generatedCount = await _fontManager.CreateAllMissingFontFiles(gumProjectSave);
        ReloadWireframeContentIfFontsChanged(generatedCount);
    }

    /// <summary>
    /// Text that resolved its font while that font was still being generated is showing a
    /// placeholder; reloading content re-resolves every font now that the files are on disk (#4799).
    /// </summary>
    private void ReloadWireframeContentIfFontsChanged(int generatedCount)
    {
        if (generatedCount > 0)
        {
            _wireframeCommands.Refresh(forceLayout: true, forceReloadContent: true);
        }
    }

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
            int generatedCount = await _fontManager.CreateAllMissingFontFiles(gumProjectSave, forceRecreate: forceRecreate);
            DateTime after = DateTime.Now;

            TimeSpan difference = after - before;
            Debug.WriteLine($"Total time: {difference.TotalMilliseconds:N0}");
            ReloadWireframeContentIfFontsChanged(generatedCount);
        }
    }
}
