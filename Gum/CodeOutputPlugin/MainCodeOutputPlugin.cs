using CodeOutputPlugin.Manager;
using Gum.ProjectServices.CodeGeneration;
using CodeOutputPlugin.Views;
using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Gum.Services.Dialogs;
using ToolsUtilities;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Messages;
using Gum.Localization;
using Gum.Extensions;
using Gum.Gui.Windows;
using System.Windows.Controls;

namespace CodeOutputPlugin;

/// <summary>
/// The WPF head's Code Output plugin: <see cref="CodeOutputPluginBase"/> with the WPF Code tab and the
/// delete dialog's "delete custom code" check box.
/// </summary>
[Export(typeof(PluginBase))]
public class MainCodeOutputPlugin : CodeOutputPluginBase, IDeleteOptionsDialogPlugin
{
    // Lives only between the delete dialog showing and the delete being confirmed; null when the
    // deleted elements had no user-authored custom code, in which case nothing was asked.
    private CheckBox? _deleteCustomCodeCheckBox;

    [ImportingConstructor]
    public MainCodeOutputPlugin(
        IGuiCommands guiCommands,
        IDialogService dialogService,
        INameVerifier nameVerifier,
        LocalizationService localizationService,
        IProjectState projectState,
        ITypeManager typeManager,
        IOutputManager outputManager,
        ISelectedState selectedState,
        IRetryService retryService,
        IMessenger messenger,
        IFileCommands fileCommands)
        : base(guiCommands, dialogService, nameVerifier, localizationService, projectState, typeManager,
            outputManager, selectedState, retryService, messenger, fileCommands)
    {
    }

    /// <inheritdoc/>
    protected override ICodeOutputTabHost CreateTabHost(ViewModels.CodeWindowViewModel viewModel, CodeOutputSettingsMembers settingsMembers) =>
        new Views.CodeWindow(viewModel, settingsMembers);

    void IDeleteOptionsDialogPlugin.CallDeleteOptionsWindowShow(object optionsWindow, Array objectsToDelete) =>
        HandleDeleteOptionsWindowShow((DeleteOptionsWindow)optionsWindow, objectsToDelete);

    void IDeleteOptionsDialogPlugin.CallDeleteConfirmed(object optionsWindow, Array deletedObjects) =>
        HandleDeleteConfirmed((DeleteOptionsWindow)optionsWindow, deletedObjects);

    /// <summary>
    /// Materializes the code-file option into a real WPF checkbox on the delete dialog. One
    /// checkbox covers the whole batch, so deleting a folder's worth of elements asks once.
    /// </summary>
    private void HandleDeleteOptionsWindowShow(DeleteOptionsWindow deleteWindow, Array objectsToDelete)
    {
        // A cancelled delete never fires DeleteConfirmed, so clear the previous dialog's checkbox
        // here - otherwise a later delete that adds no checkbox would read the stale checked state.
        _deleteCustomCodeCheckBox = null;

        var checkboxViewModel = CodeFileDeleteService.HandleDeleteOptionsWindowShow(
            objectsToDelete, ProjectSettings);

        if (checkboxViewModel != null)
        {
            _deleteCustomCodeCheckBox = checkboxViewModel.ToCheckBox();
            deleteWindow.MainStackPanel.Children.Add(_deleteCustomCodeCheckBox);
        }
    }

    /// <summary>
    /// Reads back the checkbox added by <see cref="HandleDeleteOptionsWindowShow"/> (if any) and
    /// removes it from the window afterward, mirroring the other DeleteOptionsWindow contributors.
    /// </summary>
    private void HandleDeleteConfirmed(DeleteOptionsWindow deleteOptionsWindow, Array deletedObjects)
    {
        bool isChecked = _deleteCustomCodeCheckBox?.IsChecked == true;

        CodeFileDeleteService.HandleConfirmDelete(deletedObjects, isChecked, ProjectSettings);

        if (_deleteCustomCodeCheckBox != null)
        {
            if (deleteOptionsWindow.MainStackPanel.Children.Contains(_deleteCustomCodeCheckBox))
            {
                deleteOptionsWindow.MainStackPanel.Children.Remove(_deleteCustomCodeCheckBox);
            }
            _deleteCustomCodeCheckBox = null;
        }
    }
}
