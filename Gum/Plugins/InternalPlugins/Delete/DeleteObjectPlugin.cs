using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using System.Windows.Controls;
using Gum.Commands;
using Gum.ToolCommands;
using ToolsUtilities;
using Gum.Services;
using Gum.Managers;

namespace Gum.Gui.Plugins;

[Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
public class DeleteObjectPlugin : InternalPlugin
{
    private CheckBox deleteXmlCheckBox;

    private GroupBox deleteGroupBox;
    private RadioButton deleteJustParent;
    private RadioButton deleteAllContainedObjects;
    private readonly IElementCommands _elementCommands;
    private readonly WireframeCommands _wireframeCommands;
    private readonly DeleteLogic _deleteLogic;
    private readonly InstanceDeletionHelper _instanceDeletionHelper;

    public DeleteObjectPlugin()
    {
        _elementCommands = Locator.GetRequiredService<IElementCommands>();
        _wireframeCommands = Locator.GetRequiredService<WireframeCommands>();
        _deleteLogic = Locator.GetRequiredService<DeleteLogic>();
        _instanceDeletionHelper = new InstanceDeletionHelper(_deleteLogic, _guiCommands, _wireframeCommands, _fileCommands);
    }

    public override void StartUp()
    {
        CreateDeleteXmlFileComboBox();

        CreateDeleteChildrenGroupBox();

        this.DeleteOptionsWindowShow += HandleDeleteOptionsShow;
        this.DeleteConfirm += HandleDeleteConfirm;
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

    void HandleDeleteConfirm(Windows.DeleteOptionsWindow deleteOptionsWindow, Array deletedObjects)
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
                var fileName = GetFileNameForObject(deletedObject);

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
            PerformMultipleInstancesDeleteLogic(instancesToDelete);
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

    private void PerformInstanceDeleteLogic(InstanceSave instance)
    {
        PerformMultipleInstancesDeleteLogic(new[] { instance });
    }

    private void PerformMultipleInstancesDeleteLogic(IEnumerable<InstanceSave> instances)
    {
        var instancesList = instances.ToList();
        if (instancesList.Count == 0)
            return;

        var shouldDetachChildren = deleteJustParent.IsChecked == true;
        var shouldDeleteChildren = deleteAllContainedObjects.IsChecked == true;

        // Use the first instance's parent container (all instances in a batch should have the same parent)
        var element = instancesList.First().ParentContainer;

        if (shouldDetachChildren)
        {
            _instanceDeletionHelper.DetachChildrenFromInstances(instancesList);
        }
        if (shouldDeleteChildren)
        {
            _instanceDeletionHelper.RecursivelyDeleteChildrenOfInstances(instancesList, element);
        }
    }

    public FilePath GetFileNameForObject(object deletedObject)
    {
        return deletedObject switch
        {
            ElementSave elementSave => elementSave.GetFullPathXmlFile(),
            BehaviorSave behaviorSave => _fileCommands.GetFullPathXmlFile(behaviorSave),
            InstanceSave => null,
            _ => throw new NotImplementedException($"Unsupported object type: {deletedObject?.GetType().Name}")
        };
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

            var shouldAddDeleteXml = objectToDelete is not InstanceSave && !alreadyAddedDeleteXmlCheckBox;

            // offer to delete this only if there are no duplicates
            if(shouldAddDeleteXml)
            {
                if(objectToDelete is ElementSave elementSave)
                {
                    var numberOfMatches = ObjectFinder.Self.GumProjectSave?.AllElements.Count(item => item.Name == elementSave.Name) ?? 0;
                    // If there are more than 1 match, we don't want to delete XML files because we don't want to remove the base file if
                    // duplicates were somehow added to the .gumx.
                    // it's possible the user has multiple components selected, and wants to delete both, but that's an edge case that adds complexity
                    // so I'm not going to worry about that.
                    shouldAddDeleteXml = numberOfMatches < 2;
                }
            }

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
