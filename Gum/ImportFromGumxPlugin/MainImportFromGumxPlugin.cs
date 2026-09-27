using Gum.Commands;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ImportFromGumxPlugin.ViewModels;
using System.ComponentModel.Composition;

namespace ImportFromGumxPlugin;

/// <summary>
/// Adds Content > Import > .gumx. The "needs to save" guard and the view-model wiring live in
/// <see cref="ImportFromGumxLogic"/> (Gum.Presentation); each head registers the dialog's view, so
/// this plugin loads under both heads.
/// </summary>
[Export(typeof(PluginBase))]
public class MainImportFromGumxPlugin : PluginBase
{
    public override string FriendlyName => "Import from .gumx Plugin";
    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    private readonly ImportFromGumxLogic _importFromGumxLogic;

    [ImportingConstructor]
    public MainImportFromGumxPlugin(
        IProjectState projectState,
        IImportLogic importLogic,
        IFileCommands fileCommands,
        IDialogService dialogService,
        IDispatcher dispatcher)
    {
        _importFromGumxLogic = new ImportFromGumxLogic(
            projectState, importLogic, fileCommands, dialogService, dispatcher);
    }

    public override void StartUp()
    {
        AddMenuEntry(HandleImportFromGumx, "Content", "Import", ".gumx…");
    }

    private void HandleImportFromGumx()
    {
        if (!_importFromGumxLogic.CanImport)
        {
            _dialogService.ShowMessage("You must first save the project before importing.");
            return;
        }

        ImportFromGumxViewModel viewModel = _importFromGumxLogic.CreateImportViewModel();
        _dialogService.Show(viewModel);
    }
}
