using Gum.Commands;
using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Gum.Services.Dialogs;
using System.Diagnostics;
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

    [ImportingConstructor]
    public MainFontPlugin(
        IGuiCommands guiCommands,
        IFontManager fontManager,
        IDialogService dialogService,
        IProjectState projectState)
    {
        _fontManager = fontManager;
        _dialogService = dialogService;
        _fontCacheLogic = new FontCacheLogic(fontManager, dialogService, projectState);
    }

    public override void StartUp()
    {
        var clearFontCacheMenuItem = this.AddMenuItem(new[] {
            "Content", "Clear Font Cache" });
        clearFontCacheMenuItem.Click += HandleClearFontCache;

        var refreshFontCacheMenuItem = this.AddMenuItem(new[]
        {
            "Content", "Re-create missing font files"
        });
        refreshFontCacheMenuItem.Click += (_,_) => HandleRefreshFontCache(forceRecreate:false);


        var forceFontRecreationMenuItem = this.AddMenuItem(new[]
        {
            "Content", "Force re-create all font files"
        });
        forceFontRecreationMenuItem.Click += (_, _) => HandleRefreshFontCache(forceRecreate: true);

        var viewFontCache = this.AddMenuItem(new[]
        {
            "Content", "View Font Cache"});
        viewFontCache.Click += HandleViewFontCache;



        this.ProjectLoad += HandleProjectLoaded;

    }

    private async void HandleProjectLoaded(GumProjectSave save) =>
        await _fontCacheLogic.CreateMissingFontFilesForLoadedProject();

    private void HandleClearFontCache(object? sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            _fontManager.DeleteFontCacheFolder();
        }
        catch
        {
            _dialogService.ShowMessage("Error deleting font cache:\n" + e.ToString());
        }
    }

    private void HandleViewFontCache(object? sender, System.Windows.RoutedEventArgs e)
    {
        string folder = _fontCacheLogic.GetOrCreateFontCacheFolder();

        var processStartInfo = new ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        };

        Process.Start(processStartInfo);
    }

    private async Task HandleRefreshFontCache(bool forceRecreate) =>
        await _fontCacheLogic.RefreshFontCache(forceRecreate);
}
