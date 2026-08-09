using Gum.DataTypes;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using GumFormsPlugin.ViewModels;
using System.ComponentModel.Composition;

namespace GumFormsPlugin;

// As of ADR-0005 Phase 3, the "has forms"/"needs to save"/view-model-factory decisions live in
// GumFormsLogic (Gum.Presentation) so they can be unit tested headlessly. This plugin keeps only
// the WPF menu-presence wiring and the dialog-show call.
[Export(typeof(PluginBase))]
internal class MainGumFormsPlugin : WpfPluginBase
{
    #region Fields/Properties

    public override string FriendlyName => "Gum Forms Plugin";
    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    // Assigned in StartUp, which the plugin host calls before any of the handlers below run.
    private System.Windows.Controls.MenuItem _addFormsMenuItem = null!;
    private readonly GumFormsLogic _gumFormsLogic;

    #endregion

    [ImportingConstructor]
    public MainGumFormsPlugin(GumFormsLogic gumFormsLogic)
    {
        _gumFormsLogic = gumFormsLogic;
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
        if (!_gumFormsLogic.TryCreateAddFormsViewModel(out AddFormsViewModel? viewModel, out string? blockedMessage))
        {
            _dialogService.ShowMessage(blockedMessage!);
            return;
        }

        _dialogService.Show(viewModel!);
    }

}


