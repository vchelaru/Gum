using Gum.Commands;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using System.ComponentModel.Composition;
using System.Windows;

namespace ImportFromGumxPlugin;

// As of ADR-0005 Phase 3, the "needs to save" guard + view-model wiring live in ImportFromGumxLogic
// (Gum.Presentation) so they can be unit tested headlessly. This plugin keeps only the WPF dialog
// window plumbing.
[Export(typeof(PluginBase))]
internal class MainImportFromGumxPlugin : WpfPluginBase
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
        this.AddMenuItemTo("Import from .gumx...", HandleImportFromGumx, "Content");
    }

    private void HandleImportFromGumx(object? sender, System.Windows.RoutedEventArgs e)
    {
        if (!_importFromGumxLogic.CanImport)
        {
            _dialogService.ShowMessage("You must first save the project before importing.");
            return;
        }

        var viewModel = _importFromGumxLogic.CreateImportViewModel();

        var window = new Gum.Services.Dialogs.DialogWindow
        {
            DataContext = viewModel,
            Owner = Application.Current.MainWindow,
            UseExplicitSize = true,
            Width = 600,
            Height = 650,
            SizeToContent = SizeToContent.Manual
        };
        viewModel.RequestClose += (_, _) => window.Close();
        window.ShowDialog();
    }
}
