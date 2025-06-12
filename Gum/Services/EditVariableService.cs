﻿using CommonFormsAndControls;
using ExCSS;
using Gum.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.Plugins.VariableGrid;
using Gum.ToolCommands;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GumCommon;
using WpfDataUi.DataTypes;

namespace Gum.Services;

#region Interface

public interface IEditVariableService
{
    VariableEditMode GetAvailableEditModeFor(VariableSave variableSave, IStateContainer stateCategoryListContainer);

    void ShowEditVariableWindow(VariableSave variable, IStateContainer container);

    void TryAddEditVariableOptions(InstanceMember instanceMember, VariableSave variableSave, IStateContainer stateListCategoryContainer);
}
#endregion

#region Enums

public enum VariableEditMode
{
    None,
    ExposedName,
    FullEdit
}

#endregion

public class EditVariableService : IEditVariableService
{

    private readonly ElementCommands _elementCommands;
    private readonly RenameLogic _renameLogic;

    public EditVariableService(ElementCommands elementCommands)
    {
        _elementCommands = elementCommands;
        _renameLogic = Locator.GetRequiredService<RenameLogic>();
    }

    public void TryAddEditVariableOptions(InstanceMember instanceMember, VariableSave variableSave, IStateContainer stateListCategoryContainer)
    {
        if (CanEditVariable(variableSave, stateListCategoryContainer))
        {
            instanceMember.ContextMenuEvents.Add($"Edit Variable [{variableSave.Name}]", (sender, e) =>
            {
                ShowEditVariableWindow(variableSave, stateListCategoryContainer);
            });
        }
    }

    bool CanEditVariable(VariableSave variableSave, IStateContainer stateListCategoryContainer)
    {
        return GetAvailableEditModeFor(variableSave, stateListCategoryContainer) != VariableEditMode.None;
    }

    public VariableEditMode GetAvailableEditModeFor(VariableSave variableSave, IStateContainer stateCategoryListContainer)
    {
        if (variableSave == null)
        {
            return VariableEditMode.None;
        }

        var behaviorSave = stateCategoryListContainer as BehaviorSave;
        // for now only edit variables inside of behaviors:
        if (behaviorSave != null)
        {
            return VariableEditMode.FullEdit;
        }

        if(variableSave.IsCustomVariable)
        {
            return VariableEditMode.FullEdit;
        }

        if (stateCategoryListContainer is ElementSave elementSave)
        {
            //var rootVariable = ObjectFinder.Self.GetRootVariable(variableSave.Name, stateListCategoryContainer as ElementSave);

            var isExposed = !string.IsNullOrEmpty(variableSave.ExposedAsName);

            return isExposed ? VariableEditMode.ExposedName : VariableEditMode.None;
        }

        return VariableEditMode.None;
    }


    public void ShowEditVariableWindow(VariableSave variable, IStateContainer container)
    {
        var editmode = GetAvailableEditModeFor(variable, container);

        if (editmode == VariableEditMode.ExposedName)
        {
            ShowEditExposedUi(variable, container);
        }
        else if (editmode == VariableEditMode.FullEdit)
        {
            ShowFullEditUi(variable, container);
        }

    }

    private void ShowEditExposedUi(VariableSave variable, IStateContainer container)
    {
        var tiw = new CustomizableTextInputWindow();
        tiw.Message = "Enter desired exposed variable name.";
        tiw.Width = 600;
        tiw.Title = "Edit Variable Name";



        var changes = _renameLogic.GetVariableChangesForRenamedVariable(container, variable.Name, variable.ExposedAsName);
        string changesDetails = GetChangesDetails(changes);

        if(!string.IsNullOrEmpty(changesDetails))
        {
            tiw.Message += "\n\n" + changesDetails;
        }

        tiw.Result = variable.ExposedAsName;

        if (tiw.ShowDialog() == true)
        {
            RenameExposedVariable(variable, tiw.Result, container, changes);
        }
    }

    private static string GetChangesDetails(VariableChangeResponse changes)
    {
        var variableChanges = changes.VariableChanges;

        var changesDetails = string.Empty;

        if (variableChanges.Count > 0)
        {
            if (!string.IsNullOrEmpty(changesDetails))
            {
                changesDetails += "\n\n";
            }
            changesDetails += "This will also rename the following variables:";
            foreach (var change in variableChanges)
            {
                var containerName = change.Container.ToString();
                if (change.Container is ElementSave elementSave)
                {
                    containerName = elementSave.Name;
                }
                changesDetails += $"\n{change.Variable.Name} in {containerName}";
            }
        }
        var variableReferenceChanges = changes.VariableReferenceChanges;
        if (variableReferenceChanges.Count > 0)
        {
            if(!string.IsNullOrEmpty(changesDetails))
            {
                changesDetails += "\n\n";
            }
            changesDetails += "This will also modify the following variable references:";
            foreach (var change in variableReferenceChanges)
            {
                // just in case something changes this on a separate thread, let's be safe:
                try
                {
                    var line = change.VariableReferenceList.ValueAsIList[change.LineIndex];
                    changesDetails += $"\n{line} in {change.Container.Name}";

                }
                catch { }
            }
        }

        return changesDetails;
    }

    private void RenameExposedVariable(VariableSave variable, string newName, IStateContainer container, VariableChangeResponse changeResponse)
    {
        var variableChanges = changeResponse.VariableChanges;

        var oldName = variable.ExposedAsName;

        variable.ExposedAsName = newName;

        HashSet<ElementSave> changedElements = new HashSet<ElementSave>();

        if(container is ElementSave containerElement)
        {
            changedElements.Add(containerElement);
        }

        foreach (var change in variableChanges)
        {
            var element = change.Container as ElementSave;
            if (element != null)
            {
                changedElements.Add(element);
            }

            if(change.Variable.ExposedAsName == oldName)
            {
                change.Variable.ExposedAsName = newName;
            }
            else if(change.Variable.GetRootName() == oldName)
            {
                var prefix = string.Empty;
                if(change.Variable.SourceObject != null)
                {
                    prefix = change.Variable.SourceObject + ".";
                }

                change.Variable.Name = prefix + newName;
            }
        }

        // We can re-use the logic in the AddVariableViewModel:
        var vm = Locator.GetRequiredService<AddVariableViewModel>();
        vm.RenameType = RenameType.ExposedName;
        vm.ApplyVariableReferenceChanges(changeResponse, newName, oldName, changedElements);

        GumCommands.Self.GuiCommands.RefreshVariables(force:true);
        foreach(var element in changedElements)
        {
            GumCommands.Self.FileCommands.TryAutoSaveElement(element);
        }

    }

    private void ShowFullEditUi(VariableSave variable, IStateContainer container)
    {
        var vm = Locator.GetRequiredService<AddVariableViewModel>();
        vm.RenameType = RenameType.NormalName;

        vm.Variable = variable;
        vm.Element = container as ElementSave;

        vm.SelectedItem = variable.Type;
        vm.EnteredName = variable.Name;

        var window = new AddVariableWindow(vm);
        window.Title = "Edit Variable";

        var changes = _renameLogic.GetVariableChangesForRenamedVariable(container, variable.Name, variable.Name);

        var isReferencedInVariableReference = changes.VariableReferenceChanges.Count > 0;
        vm.VariableChangeResponse = changes;

        string changesDetails = GetChangesDetails(changes);
        vm.DetailText = changesDetails;

        var result = window.ShowDialog();

        if (result == true)
        {
            var validityResponse = vm.Validate();
            if(validityResponse.Succeeded == false)
            {
                GumCommands.Self.GuiCommands.ShowMessage(validityResponse.Message);
            }
            else
            {
                vm.DoEdit(variable, changes);
            }
        }
    }
}
