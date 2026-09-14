using System;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Gum.Services.Dialogs;
using Gum.Services;
using Gum.Services.Fonts;

namespace Gum.Plugins.Fonts;

/// <summary>
/// The Content menu's font cache entries (clear, re-create missing, force re-create, view) and the
/// missing-font creation scheduled after a project loads. Shared by both heads; the logic is
/// <see cref="FontCacheLogic"/>.
/// </summary>
[Export(typeof(PluginBase))]
public class MainFontPlugin : CorePriorityPlugin
{
    private readonly IFontManager _fontManager;
    private readonly FontCacheLogic _fontCacheLogic;
    private readonly IFileSystemRevealService _fileSystemRevealService;

    [ImportingConstructor]
    public MainFontPlugin(
        IGuiCommands guiCommands,
        IFontManager fontManager,
        IDialogService dialogService,
        IProjectState projectState,
        IDispatcher dispatcher,
        IFileSystemRevealService fileSystemRevealService)
    {
        _fontManager = fontManager;
        _fileSystemRevealService = fileSystemRevealService;
        _fontCacheLogic = new FontCacheLogic(fontManager, dialogService, projectState, dispatcher);
    }

    public override void StartUp()
    {
        AddMenuEntry(HandleClearFontCache, "Content", "Clear Font Cache");
        AddMenuEntry(async () => await HandleRefreshFontCache(forceRecreate: false), "Content", "Re-create missing font files");
        AddMenuEntry(async () => await HandleRefreshFontCache(forceRecreate: true), "Content", "Force re-create all font files");
        AddMenuEntry(HandleViewFontCache, "Content", "View Font Cache");



        this.ProjectLoad += HandleProjectLoaded;

    }

    private void HandleProjectLoaded(GumProjectSave save) =>
        _fontCacheLogic.ScheduleMissingFontCreationForLoadedProject();

    private void HandleClearFontCache()
    {
        try
        {
            _fontManager.DeleteFontCacheFolder();
        }
        catch (Exception exception)
        {
            _dialogService.ShowMessage("Error deleting font cache:\n" + exception);
        }
    }

    private void HandleViewFontCache()
    {
        string folder = _fontCacheLogic.GetOrCreateFontCacheFolder();
        _fileSystemRevealService.OpenFolder(folder);
    }

    private async Task HandleRefreshFontCache(bool forceRecreate) =>
        await _fontCacheLogic.RefreshFontCache(forceRecreate);
}
