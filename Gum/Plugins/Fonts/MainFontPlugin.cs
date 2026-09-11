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

// As of ADR-0005 Phase 3, the font-cache logic lives in FontCacheLogic (Gum.Presentation) so it can
// be unit tested headlessly. This plugin keeps only menu wiring; HandleClearFontCache stays here
// unchanged since its catch block reads the handler's own RoutedEventArgs parameter, not the caught
// exception - extracting it would either leak a WPF type into Gum.Presentation or change behavior.
[Export(typeof(PluginBase))]
public class MainFontPlugin : PriorityPlugin
{
    private readonly IFontManager _fontManager;
    private readonly IDialogService _dialogService;
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
        _dialogService = dialogService;
        _fileSystemRevealService = fileSystemRevealService;
        _fontCacheLogic = new FontCacheLogic(fontManager, dialogService, projectState, dispatcher);
    }

    public override void StartUp()
    {
        AddMenuEntry(HandleClearFontCache, "Content", "Clear Font Cache");
        AddMenuEntry(() => HandleRefreshFontCache(forceRecreate: false), "Content", "Re-create missing font files");
        AddMenuEntry(() => HandleRefreshFontCache(forceRecreate: true), "Content", "Force re-create all font files");
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
