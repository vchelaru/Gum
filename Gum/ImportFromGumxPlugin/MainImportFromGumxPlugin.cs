using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.Services;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ImportFromGumxPlugin.Services;
using ImportFromGumxPlugin.ViewModels;
using System.ComponentModel.Composition;

namespace ImportFromGumxPlugin;

[Export(typeof(PluginBase))]
internal class MainImportFromGumxPlugin : PluginBase
{
    public override string FriendlyName => "Import from .gumx Plugin";
    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    public override void StartUp()
    {
        this.AddMenuItemTo("Import from .gumx...", HandleImportFromGumx, "Content");
    }

    private void HandleImportFromGumx(object? sender, System.Windows.RoutedEventArgs e)
    {
        var projectState = Locator.GetRequiredService<IProjectState>();
        var importLogic = Locator.GetRequiredService<IImportLogic>();
        var fileCommands = Locator.GetRequiredService<IFileCommands>();

        if (projectState.NeedsToSaveProject)
        {
            _dialogService.ShowMessage("You must first save the project before importing.");
            return;
        }

        var sourceService = new GumxSourceService();
        var dependencyResolver = new GumxDependencyResolver();
        var importService = new GumxImportService(importLogic, projectState, fileCommands, sourceService);

        var viewModel = new ImportFromGumxViewModel(
            sourceService,
            dependencyResolver,
            importService,
            projectState);

        _dialogService.Show(viewModel);
    }
}
