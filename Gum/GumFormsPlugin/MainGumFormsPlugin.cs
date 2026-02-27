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
using Gum.Logic.FileWatch;

namespace GumFormsPlugin;

[Export(typeof(PluginBase))]
internal class MainGumFormsPlugin : PluginBase
{
    #region Fields/Properties

    public override string FriendlyName => "Gum Forms Plugin";
    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    System.Windows.Controls.MenuItem _addFormsMenuItem;
    private readonly FormsFileService _formsFileService;
    private readonly IImportLogic _importLogic;
    private readonly IFileWatchManager _fileWatchManager;

    #endregion

    public MainGumFormsPlugin()
    {
        _formsFileService = new FormsFileService();
        _importLogic = Locator.GetRequiredService<IImportLogic>();
        _fileWatchManager = Locator.GetRequiredService<IFileWatchManager>();
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
        // A newly created project has no FullFileName yet, so it cannot have forms.
        // Checking the save parameter directly avoids any stale state in projectState.
        var hasForms = !string.IsNullOrEmpty(save?.FullFileName) && GetIfProjectHasForms();

        var parent = _addFormsMenuItem.Parent as System.Windows.Controls.ItemsControl;
        if (hasForms)
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

    private bool GetIfProjectHasForms()
    {
        var files = _formsFileService.GetSourceDestinations(false);

        var firstMatch = files.Values
            .FirstOrDefault(item => 
                item.Extension != "png" && 
                item.Extension != "gutx" &&
                item.Extension != "fnt" && 
                item.Extension != "bmfc" &&
                item.Exists());

        return firstMatch != null;
    }

    private void HandleAddFormsComponents(object? sender, System.Windows.RoutedEventArgs e)
    {
        var projectState = Locator.GetRequiredService<IProjectState>();

        #region Early Out

        if (projectState.NeedsToSaveProject)
        {
            _dialogService.ShowMessage("You must first save the project before importing forms");
            return;
        }
        #endregion

        var viewModel = new AddFormsViewModel(
            _formsFileService, 
            _dialogService,
            _fileCommands, 
            _importLogic, 
            projectState,
            _fileWatchManager);
        _dialogService.Show(viewModel);
    }

}


