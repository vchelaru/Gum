using Gum.Commands;
using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Threading.Tasks;
using Gum.Services.Dialogs;
using System.Diagnostics;
using Gum.Services.Fonts;

namespace Gum.Plugins.Fonts;

[Export(typeof(PluginBase))]
public class MainFontPlugin : InternalPlugin
{

    private readonly IGuiCommands _guiCommands;
    private readonly FontManager _fontManager;
    private readonly IDialogService _dialogService;
    private readonly IProjectState _projectState;

    public MainFontPlugin()
    {
        _guiCommands = Locator.GetRequiredService<IGuiCommands>();
        _fontManager = Locator.GetRequiredService<FontManager>();
        _dialogService = Locator.GetRequiredService<IDialogService>();
        _projectState = Locator.GetRequiredService<IProjectState>();
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

    private async void HandleProjectLoaded(GumProjectSave save)
    {
        await _fontManager.CreateAllMissingFontFiles(
            _projectState.GumProjectSave);
    }

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
        if(!System.IO.Directory.Exists(_fontManager.AbsoluteFontCacheFolder))
        {
            System.IO.Directory.CreateDirectory(_fontManager.AbsoluteFontCacheFolder);
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = _fontManager.AbsoluteFontCacheFolder,
            UseShellExecute = true
        };

        System.Diagnostics.Process.Start(processStartInfo);
    }

    private async Task HandleRefreshFontCache(bool forceRecreate)
    {
        var gumProjectSave = _projectState.GumProjectSave;
        if(gumProjectSave == null)
        {
            _dialogService.ShowMessage(
                "A Gum project must first be loaded before recreating font files");
        }
        else
        {
            var before = DateTime.Now;
            await _fontManager.CreateAllMissingFontFiles(
                _projectState.GumProjectSave, forceRecreate:forceRecreate);
            var after = DateTime.Now;

            var difference = after - before;
            System.Diagnostics.Debug.WriteLine($"Total time: {difference.TotalMilliseconds:N0}");
        }
    }
}
