﻿using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using System.Windows.Controls;
using Gum.Commands;
using Gum.ToolCommands;
using GumCommon;
using ToolsUtilities;

namespace Gum.Gui.Plugins;

[Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
public class DeleteObjectPlugin : InternalPlugin
{
    private CheckBox deleteXmlCheckBox;

    private GroupBox deleteGroupBox;
    private RadioButton deleteJustParent;
    private RadioButton deleteAllContainedObjects;
    private readonly ElementCommands _elementCommands;
    private readonly WireframeCommands _wireframeCommands;

    public DeleteObjectPlugin()
    {
        _elementCommands = Locator.GetRequiredService<ElementCommands>();
        _wireframeCommands = Locator.GetRequiredService<WireframeCommands>();
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
        deleteJustParent.Content = "Delete only parent";
        stackPanel.Children.Add(deleteJustParent);

        deleteAllContainedObjects = new RadioButton();
        deleteAllContainedObjects.Content = "Delete all children";
        stackPanel.Children.Add(deleteAllContainedObjects);
    }

    void HandleDeleteConfirm(Windows.DeleteOptionsWindow deleteOptionsWindow, Array deletedObjects)
    {
        foreach (var deletedObject in deletedObjects)
        {

            var asInstance = deletedObject as InstanceSave;

            if (asInstance != null)
            {
                PerformInstanceDeleteLogic(asInstance);
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
                        System.Windows.Forms.MessageBox.Show("Could not delete the file\n" + fileName);
                    }
                }
            }
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
        var shouldDetachChildren = deleteJustParent.IsChecked == true;
        var shouldDeleteChildren = deleteAllContainedObjects.IsChecked == true;

        var element = instance.ParentContainer;

        if (shouldDetachChildren)
        {
            _elementCommands.RemoveParentReferencesToInstance(instance, element);
        }
        if (shouldDeleteChildren)
        {
            RecursivelyDeleteChildrenOf(instance);

            // refresh the property grid, refresh the wireframe, save
            GumCommands.Self.GuiCommands.RefreshElementTreeView(element);
            _wireframeCommands.Refresh();
            GumCommands.Self.FileCommands.TryAutoSaveElement(element);
        }
    }

    private void RecursivelyDeleteChildrenOf(InstanceSave instance)
    {
        var childrenOfInstance = GetChildrenOf(instance);

        foreach (var child in childrenOfInstance)
        {
            // we want to do this bottom up, so go recursively first.:
            RecursivelyDeleteChildrenOf(child);

        }

        // This may have been removed by the main Delete command. If so, then no need
        // to do a full removal, just remove parent references:
        var parentContainer = instance.ParentContainer;
        if (parentContainer.Instances.Contains(instance))
        {
            _elementCommands.RemoveInstance(instance, parentContainer);
        }
        else
        {
            _elementCommands.RemoveParentReferencesToInstance(instance, parentContainer);
        }
    }

    private InstanceSave[] GetChildrenOf(InstanceSave instance)
    {
        var container = instance.ParentContainer;

        var defaultState = container?.DefaultState;

        var variablesUsingInstanceAsparent = defaultState.Variables
            .Where(item =>
                item.Value is string asString &&
                (asString == instance.Name || asString.StartsWith(instance.Name + ".")) &&
                item.SetsValue &&
                item.GetRootName() == "Parent");

        var instanceNames = variablesUsingInstanceAsparent
            .Select(item => item.SourceObject)
            .Distinct()
            .ToArray();

        List<InstanceSave> instanceSaveList = new List<InstanceSave>();

        foreach (var instanceName in instanceNames)
        {
            var childInstance = container.GetInstance(instanceName);

            if (childInstance != null)
            {
                instanceSaveList.Add(childInstance);
            }
        }

        return instanceSaveList.ToArray();
    }

    public FilePath GetFileNameForObject(object deletedObject)
    {
        if (deletedObject is ElementSave)
        {
            ElementSave asElement = deletedObject as ElementSave;

            return asElement.GetFullPathXmlFile();
        }
        else if (deletedObject is BehaviorSave)
        {
            var asBehaviorSave = deletedObject as BehaviorSave;

            return GumCommands.Self.FileCommands.GetFullPathXmlFile(asBehaviorSave);
        }
        else if (deletedObject is InstanceSave)
        {
            return null;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    void HandleDeleteOptionsShow(Windows.DeleteOptionsWindow deleteWindow, Array objectsToDelete)
    {
        bool alreadyAddedUiForInstances = false;
        bool alreadyAddedDeleteXmlCheckBox = false;

        foreach (var objectToDelete in objectsToDelete)
        {
            if (!alreadyAddedUiForInstances)
            {
                var objectAsInstance = objectToDelete as InstanceSave;
                if (objectAsInstance?.ParentContainer != null)
                {
                    var parentContainer = objectAsInstance.ParentContainer;

                    var allVariables = parentContainer.AllStates.SelectMany(item => item.Variables);
                    var instanceName = objectAsInstance.Name;

                    var anyVariableSetsParentToThis = allVariables.Any(item =>
                        item.SetsValue &&
                        item.Value != null &&
                        item.Value is string asString &&
                        (asString == instanceName || asString.StartsWith(instanceName + ".")) &&
                        item.GetRootName() == "Parent");

                    if (anyVariableSetsParentToThis)
                    {
                        deleteWindow.MainStackPanel.Children.Add(deleteGroupBox);

                        deleteJustParent.IsChecked = true;
                        deleteAllContainedObjects.IsChecked = false;
                        alreadyAddedUiForInstances = true;
                    }
                }

            }

            if (objectToDelete is not InstanceSave && !alreadyAddedDeleteXmlCheckBox)
            {
                deleteWindow.MainStackPanel.Children.Add(deleteXmlCheckBox);
                deleteXmlCheckBox.Content = "Delete XML file";
                deleteXmlCheckBox.Width = 220;
                alreadyAddedDeleteXmlCheckBox = true;
            }
        }
    }

    private void CreateDeleteXmlFileComboBox()
    {
        deleteXmlCheckBox = new CheckBox();
        deleteXmlCheckBox.IsChecked = true;


    }

}
