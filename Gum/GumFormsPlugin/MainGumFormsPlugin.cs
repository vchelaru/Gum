using Gum.DataTypes;
using Gum.Plugins;
using Gum.Menus;
using Gum.Plugins.BaseClasses;
using GumFormsPlugin.ViewModels;
using System.ComponentModel.Composition;

namespace GumFormsPlugin;

// As of ADR-0005 Phase 3, the "has forms"/"needs to save"/view-model-factory decisions live in
// GumFormsLogic (Gum.Presentation) so they can be unit tested headlessly. This plugin keeps only
// the menu-presence wiring (through the shared menu model) and the dialog-show call.
[Export(typeof(PluginBase))]
internal class MainGumFormsPlugin : PluginBase
{
    #region Fields/Properties

    public override string FriendlyName => "Gum Forms Plugin";
    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    // Assigned in StartUp, which the plugin host calls before any of the handlers below run.
    private MenuItemModel _addFormsMenuItem = null!;
    private readonly GumFormsLogic _gumFormsLogic;

    #endregion

    [ImportingConstructor]
    public MainGumFormsPlugin(GumFormsLogic gumFormsLogic)
    {
        _gumFormsLogic = gumFormsLogic;
    }

    public override void StartUp()
    {
        _addFormsMenuItem = AddMenuEntry(HandleAddFormsComponents, "Content", "Add Forms Components");

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

        // The item lives in the shared menu model; the head re-renders when the collection changes.
        MenuItemModel content = Menu!.GetItem("Content")!;
        bool isShown = content.Items.Contains(_addFormsMenuItem);
        if (!shouldShow && isShown)
        {
            content.Items.Remove(_addFormsMenuItem);
        }
        else if (shouldShow && !isShown)
        {
            _addFormsMenuItem = AddMenuEntry(HandleAddFormsComponents, "Content", "Add Forms Components");
        }
    }

    private void HandleAddFormsComponents()
    {
        if (!_gumFormsLogic.TryCreateAddFormsViewModel(out AddFormsViewModel? viewModel, out string? blockedMessage))
        {
            _dialogService.ShowMessage(blockedMessage!);
            return;
        }

        _dialogService.Show(viewModel!);
    }

}


