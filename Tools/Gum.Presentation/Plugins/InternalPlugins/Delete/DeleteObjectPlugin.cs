using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Services.Dialogs;

namespace Gum.Gui.Plugins;

/// <summary>
/// Adds the delete confirmation's "Delete children?" and "Delete XML file" options and applies them
/// when the delete is confirmed. Shared by both heads: the options are neutral
/// (<see cref="DeleteOptionsDialogViewModel"/>) and each head renders them.
/// </summary>
[Export(typeof(PluginBase))]
public class DeleteObjectPlugin : CorePriorityPlugin
{
    internal const string DeleteChildrenHeader = "Delete children?";
    internal const string DeleteOnlyParentsLabel = "Delete only parent(s)";
    internal const string DeleteParentsAndChildrenLabel = "Delete parent and children";
    internal const string DeleteXmlLabel = "Delete XML file";

    private readonly InstanceDeletionHelper _instanceDeletionHelper;

    // The options this plugin added to the current confirmation; null when it added none.
    private DeleteOptionChoiceViewModel? _deleteChildrenChoice;
    private DeleteOptionCheckboxViewModel? _deleteXmlOption;

    [ImportingConstructor]
    public DeleteObjectPlugin(
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IDeleteLogic deleteLogic,
        IWireframeCommands wireframeCommands)
    {
        _instanceDeletionHelper = new InstanceDeletionHelper(deleteLogic, guiCommands, wireframeCommands, fileCommands);
    }

    public override void StartUp()
    {
        this.DeleteOptionsShow += HandleDeleteOptionsShow;
        this.DeleteOptionsConfirmed += HandleDeleteConfirmed;
    }

    private void HandleDeleteOptionsShow(DeleteOptionsDialogViewModel dialog, Array objectsToDelete)
    {
        // A cancelled delete never raises DeleteOptionsConfirmed, so start from a clean slate.
        _deleteChildrenChoice = null;
        _deleteXmlOption = null;

        List<InstanceSave> instances = objectsToDelete.OfType<InstanceSave>()
            .Where(instance => instance.ParentContainer != null)
            .ToList();

        if (instances.Count > 0 && _instanceDeletionHelper.AnyInstanceHasChildren(instances))
        {
            _deleteChildrenChoice = new DeleteOptionChoiceViewModel(DeleteChildrenHeader, new[]
            {
                new DeleteOptionCheckboxViewModel { Label = DeleteOnlyParentsLabel, IsChecked = true },
                new DeleteOptionCheckboxViewModel { Label = DeleteParentsAndChildrenLabel, IsChecked = false },
            });
            dialog.Choices.Add(_deleteChildrenChoice);
        }

        // Offer to delete the XML file only if there are no duplicates - if there are more than 1
        // match, we don't want to delete XML files because we don't want to remove the base file if
        // duplicates were somehow added to the .gumx. It's possible the user has multiple components
        // selected, and wants to delete both, but that's an edge case that adds complexity so I'm not
        // going to worry about that.
        if (objectsToDelete.Cast<object>().Any(_instanceDeletionHelper.ShouldOfferDeleteXmlOption))
        {
            _deleteXmlOption = new DeleteOptionCheckboxViewModel { Label = DeleteXmlLabel, IsChecked = true };
            dialog.CheckBoxes.Add(_deleteXmlOption);
        }
    }

    private void HandleDeleteConfirmed(DeleteOptionsDialogViewModel dialog, Array deletedObjects)
    {
        bool shouldDeleteXml = _deleteXmlOption?.IsChecked == true;
        bool shouldDetachChildren = _deleteChildrenChoice?.Options[0].IsChecked == true;
        bool shouldDeleteChildren = _deleteChildrenChoice?.Options[1].IsChecked == true;
        _deleteChildrenChoice = null;
        _deleteXmlOption = null;

        // Collect all instances to delete in a batch
        List<InstanceSave> instancesToDelete = new List<InstanceSave>();

        foreach (object deletedObject in deletedObjects)
        {
            if (deletedObject is InstanceSave asInstance)
            {
                instancesToDelete.Add(asInstance);
            }

            if (shouldDeleteXml)
            {
                // The element XML is user data and Gum's undo restores project state, not files on
                // disk, so it goes to the recycle bin rather than being deleted outright.
                var response = _instanceDeletionHelper.TryRecycleXmlFileForObject(deletedObject);

                if (!response.Succeeded)
                {
                    _dialogService.ShowMessage(response.Message);
                }
            }
        }

        if (instancesToDelete.Count > 0)
        {
            _instanceDeletionHelper.PerformMultipleInstancesDelete(
                instancesToDelete, shouldDetachChildren, shouldDeleteChildren);
        }
    }
}
