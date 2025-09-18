using Gum;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using GumFormsPlugin.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace GumFormsPlugin;

[Export(typeof(PluginBase))]
internal class MainGumFormsPlugin : PluginBase
{
    #region Fields/Properties

    public override string FriendlyName => "Gum Forms Plugin";
    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    System.Windows.Forms.ToolStripMenuItem _addFormsMenuItem;
    private readonly FormsFileService _formsFileService;
    private readonly IDialogService _dialogService;
    private readonly ImportLogic _importLogic;

    #endregion

    public MainGumFormsPlugin()
    {
        _formsFileService = new FormsFileService();
        _dialogService = Locator.GetRequiredService<IDialogService>();
        _importLogic = Locator.GetRequiredService<ImportLogic>();
    }

    public override void StartUp()
    {
        _addFormsMenuItem = 
            this.AddMenuItemTo("Add Forms Components", HandleAddFormsComponents, "Content");

        this.ProjectLoad += HandleProjectLoaded;
    }

    private void HandleProjectLoaded(GumProjectSave save)
    {
        // see if it already has forms
        var hasForms = GetIfProjectHasForms();

        var parent = _addFormsMenuItem.GetCurrentParent();
        if(hasForms)
        {
            if(parent != null)
            {
                parent.Items.Remove(_addFormsMenuItem);
            }
        }
        else
        {
            if(parent == null)
            {
                _addFormsMenuItem =
                    this.AddMenuItemTo("Add Forms Components", HandleAddFormsComponents, "Content");
            }
        }
    }

    private bool GetIfProjectHasForms()
    {
        var files = _formsFileService.GetSourceDestinations(false);

        return files.Values.Any(item => item.Extension != "png" && item.Extension != "gutx" && item.Exists());
    }

    private void HandleAddFormsComponents(object sender, EventArgs e)
    {
        #region Early Out

        if (GumState.Self.ProjectState.NeedsToSaveProject)
        {
            _dialogService.ShowMessage("You must first save the project before importing forms");
            return;
        }
        #endregion

        var viewModel = new AddFormsViewModel(_formsFileService, _dialogService, _fileCommands, _importLogic);
        _dialogService.Show(viewModel);
    }

}


