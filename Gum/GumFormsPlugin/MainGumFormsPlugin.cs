using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using System.ComponentModel.Composition;
using Gum.Services;
using Gum.Logic;
using Gum.Logic.FileWatch;

namespace GumFormsPlugin;

// As of ADR-0005 Phase 3, the "has forms"/"needs to save"/view-model-factory decisions live in
// GumFormsLogic (Gum.Presentation) so they can be unit tested headlessly. This plugin keeps only
// the WPF menu-presence wiring and the dialog-show call.
[Export(typeof(PluginBase))]
internal class MainGumFormsPlugin : PluginBase
{
    #region Fields/Properties

    public override string FriendlyName => "Gum Forms Plugin";
    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    System.Windows.Controls.MenuItem _addFormsMenuItem;
    private readonly GumFormsLogic _gumFormsLogic;

    #endregion

    [ImportingConstructor]
    public MainGumFormsPlugin(
        IImportLogic importLogic,
        IFileWatchManager fileWatchManager,
        IProjectState projectState,
        IFileCommands fileCommands,
        IDialogService dialogService)
    {
        // Note: PluginBase's own _fileCommands/_dialogService [Import] properties aren't set until
        // after construction, so these must be taken as explicit ctor params here (both are already
        // bridged into the plugin container for other plugins).
        IFormsFileService formsFileService = new FormsFileService(projectState);
        _gumFormsLogic = new GumFormsLogic(
            formsFileService,
            projectState,
            importLogic,
            fileCommands,
            fileWatchManager,
            dialogService,
            Locator.GetRequiredService<ISkiaShapeStandardsLogic>());
    }

    public override void StartUp()
    {
        _addFormsMenuItem =
            this.AddMenuItemTo("Add Forms Components", HandleAddFormsComponents, "Content");

        this.ProjectLoad += HandleProjectLoaded;
        this.AfterProjectSave += HandleProjectSave;
    }

    private void HandleProjectSave(GumProjectSave save)
    {
        RefreshAddFormsMenuPresence(save);
    }

    private void HandleProjectLoaded(GumProjectSave save)
    {
        RefreshAddFormsMenuPresence(save);
    }

    private void RefreshAddFormsMenuPresence(GumProjectSave save)
    {
        bool shouldShow = _gumFormsLogic.ShouldShowAddFormsMenuItem(save);

        var parent = _addFormsMenuItem.Parent as System.Windows.Controls.ItemsControl;
        if (!shouldShow)
        {
            if (parent != null)
            {
                parent.Items.Remove(_addFormsMenuItem);
            }
        }
        else
        {
            if (parent == null)
            {
                _addFormsMenuItem =
                    this.AddMenuItemTo("Add Forms Components", HandleAddFormsComponents, "Content");
            }
        }
    }

    private void HandleAddFormsComponents(object? sender, System.Windows.RoutedEventArgs e)
    {
        if (!_gumFormsLogic.TryCreateAddFormsViewModel(out var viewModel, out var blockedMessage))
        {
            _dialogService.ShowMessage(blockedMessage);
            return;
        }

        _dialogService.Show(viewModel);
    }

}


