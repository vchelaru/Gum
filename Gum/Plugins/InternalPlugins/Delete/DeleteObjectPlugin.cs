using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using Gum.DataTypes;
using System.Windows.Controls;
using Gum.Commands;
using Gum.Managers;

namespace Gum.Gui.Plugins;

[Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
public class DeleteObjectPlugin : PriorityPlugin
{
    private CheckBox deleteXmlCheckBox;

    private GroupBox deleteGroupBox;
    private RadioButton deleteJustParent;
    private RadioButton deleteAllContainedObjects;
    private readonly IWireframeCommands _wireframeCommands;
    private readonly IDeleteLogic _deleteLogic;
    private readonly InstanceDeletionHelper _instanceDeletionHelper;

    [ImportingConstructor]
    public DeleteObjectPlugin(
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IDeleteLogic deleteLogic,
        IWireframeCommands wireframeCommands)
    {
        _wireframeCommands = wireframeCommands;
        _deleteLogic = deleteLogic;
        _instanceDeletionHelper = new InstanceDeletionHelper(_deleteLogic, guiCommands, _wireframeCommands, fileCommands);
    }

    public override void StartUp()
    {
        CreateDeleteXmlFileComboBox();

        CreateDeleteChildrenGroupBox();

        this.DeleteOptionsWindowShow += HandleDeleteOptionsShow;
        this.DeleteConfirmed += HandleDeleteConfirmed;
    }

    private void CreateDeleteChildrenGroupBox()
    {

        deleteGroupBox = new GroupBox();
        deleteGroupBox.Header = "Delete children?";

        var stackPanel = new StackPanel();
        deleteGroupBox.Content = stackPanel;

        deleteJustParent = new RadioButton();
        deleteJustParent.Content = "Delete only parent(s)";
        stackPanel.Children.Add(deleteJustParent);

        deleteAllContainedObjects = new RadioButton();
        deleteAllContainedObjects.Content = "Delete parent and children";
        stackPanel.Children.Add(deleteAllContainedObjects);
    }

    void HandleDeleteConfirmed(Windows.DeleteOptionsWindow deleteOptionsWindow, Array deletedObjects)
    {
        // Collect all instances to delete in a batch
        var instancesToDelete = new List<InstanceSave>();

        foreach (var deletedObject in deletedObjects)
        {
            var asInstance = deletedObject as InstanceSave;

            if (asInstance != null)
            {
                instancesToDelete.Add(asInstance);
            }

            if (deleteXmlCheckBox.IsChecked == true)
            {
                var fileName = _instanceDeletionHelper.GetFileNameForObject(deletedObject);

                if (fileName?.Exists() == true)
                {
                    try
                    {
                        System.IO.File.Delete(fileName.FullPath);
                    }
                    catch
                    {
                        _dialogService.ShowMessage("Could not delete the file\n" + fileName);
                    }
                }
            }
        }

        // Perform batch instance deletion
        if (instancesToDelete.Count > 0)
        {
            var shouldDetachChildren = deleteJustParent.IsChecked == true;
            var shouldDeleteChildren = deleteAllContainedObjects.IsChecked == true;

            _instanceDeletionHelper.PerformMultipleInstancesDelete(
                instancesToDelete, shouldDetachChildren, shouldDeleteChildren);
        }

        if (deleteOptionsWindow.MainStackPanel.Children.Contains(deleteXmlCheckBox))
        {
            deleteOptionsWindow.MainStackPanel.Children.Remove(deleteXmlCheckBox);
        }

        if (deleteOptionsWindow.MainStackPanel.Children.Contains(deleteGroupBox))
        {
            deleteOptionsWindow.MainStackPanel.Children.Remove(deleteGroupBox);
        }
    }

    void HandleDeleteOptionsShow(Windows.DeleteOptionsWindow deleteWindow, Array objectsToDelete)
    {
        bool alreadyAddedUiForInstances = false;
        bool alreadyAddedDeleteXmlCheckBox = false;

        // Collect all instances to check if any have children
        var instances = objectsToDelete.OfType<InstanceSave>()
            .Where(instance => instance.ParentContainer != null)
            .ToList();

        if (!alreadyAddedUiForInstances && instances.Count > 0)
        {
            var anyHasChildren = _instanceDeletionHelper.AnyInstanceHasChildren(instances);

            if (anyHasChildren)
            {
                deleteWindow.MainStackPanel.Children.Add(deleteGroupBox);

                deleteJustParent.IsChecked = true;
                deleteAllContainedObjects.IsChecked = false;
                alreadyAddedUiForInstances = true;
            }
        }

        foreach (var objectToDelete in objectsToDelete)
        {
            // Offer to delete the XML file only if there are no duplicates - if there are more than 1
            // match, we don't want to delete XML files because we don't want to remove the base file if
            // duplicates were somehow added to the .gumx. It's possible the user has multiple components
            // selected, and wants to delete both, but that's an edge case that adds complexity so I'm not
            // going to worry about that.
            var shouldAddDeleteXml = !alreadyAddedDeleteXmlCheckBox
                && _instanceDeletionHelper.ShouldOfferDeleteXmlOption(objectToDelete);

            if (shouldAddDeleteXml)
            {
                deleteWindow.MainStackPanel.Children.Add(deleteXmlCheckBox);
                deleteXmlCheckBox.Content = "Delete XML file";
                deleteXmlCheckBox.Width = 220;
                deleteXmlCheckBox.IsChecked = true;
                alreadyAddedDeleteXmlCheckBox = true;
            }
        }

        if(!alreadyAddedDeleteXmlCheckBox)
        {
            deleteXmlCheckBox.IsChecked = false;
        }
    }

    private void CreateDeleteXmlFileComboBox()
    {
        deleteXmlCheckBox = new CheckBox();
        deleteXmlCheckBox.IsChecked = true;


    }

}
