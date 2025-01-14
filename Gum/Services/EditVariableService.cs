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
using WpfDataUi.DataTypes;

namespace Gum.Services;

#region Interface

internal interface IEditVariableService
{
    void TryAddEditVariableOptions(InstanceMember instanceMember, VariableSave variableSave, IStateCategoryListContainer stateListCategoryContainer);
}
#endregion

internal class EditVariableService : IEditVariableService
{
    #region Enums

    enum EditMode
    {
        None,
        ExposedName,
        FullEdit
    }

    #endregion

    private readonly ElementCommands _elementCommands;

    public EditVariableService(ElementCommands elementCommands)
    {
        _elementCommands = elementCommands;
    }

    public void TryAddEditVariableOptions(InstanceMember instanceMember, VariableSave variableSave, IStateCategoryListContainer stateListCategoryContainer)
    {
        if (ShouldAddEditVariableOptions(variableSave, stateListCategoryContainer))
        {
            instanceMember.ContextMenuEvents.Add("Edit Variable", (sender, e) =>
            {
                ShowEditVariableWindow(variableSave, stateListCategoryContainer);
            });
        }
    }

    bool ShouldAddEditVariableOptions(VariableSave variableSave, IStateCategoryListContainer stateListCategoryContainer)
    {
        return GetAvailableEditModeFor(variableSave, stateListCategoryContainer) != EditMode.None;
    }

    EditMode GetAvailableEditModeFor(VariableSave variableSave, IStateCategoryListContainer stateCategoryListContainer)
    {
        if (variableSave == null)
        {
            return EditMode.None;
        }

        var behaviorSave = stateCategoryListContainer as BehaviorSave;
        // for now only edit variables inside of behaviors:
        if (behaviorSave != null)
        {
            return EditMode.FullEdit;
        }

        if(variableSave.IsCustomVariable)
        {
            return EditMode.FullEdit;
        }

        if (stateCategoryListContainer is ElementSave elementSave)
        {
            //var rootVariable = ObjectFinder.Self.GetRootVariable(variableSave.Name, stateListCategoryContainer as ElementSave);

            var isExposed = !string.IsNullOrEmpty(variableSave.ExposedAsName);

            return isExposed ? EditMode.ExposedName : EditMode.None;
        }

        return EditMode.None;
    }


    private void ShowEditVariableWindow(VariableSave variable, IStateCategoryListContainer container)
    {
        var editmode = GetAvailableEditModeFor(variable, container);

        if (editmode == EditMode.ExposedName)
        {
            ShowEditExposedUi(variable, container);
        }
        else if (editmode == EditMode.FullEdit)
        {
            ShowFullEditUi(variable, container);
        }

    }

    private void ShowEditExposedUi(VariableSave variable, IStateCategoryListContainer container)
    {
        var tiw = new CustomizableTextInputWindow();
        tiw.Message = "Enter new exposed variable name.";
        tiw.Width = 600;
        tiw.Title = "New exposed variable";

        var changes = RenameLogic.GetVariableChangesForRenamedVariable(container, variable, variable.ExposedAsName);
        if (changes.Count > 0)
        {
            tiw.Message += "\n\nThis will also rename the following variables:";
            foreach (var change in changes)
            {
                var containerName = change.Container.ToString();
                if (change.Container is ElementSave elementSave)
                {
                    containerName = elementSave.Name;
                }
                tiw.Message += $"\n{change.Variable.Name} in {containerName}";
            }

        }
        tiw.Result = variable.ExposedAsName;

        if (tiw.ShowDialog() == true)
        {
            RenameExposedVariable(variable, tiw.Result, container, changes);
        }
    }

    private void RenameExposedVariable(VariableSave variable, string newName, IStateCategoryListContainer container, List<VariableChange> changes)
    {
        var oldName = variable.ExposedAsName;

        variable.ExposedAsName = newName;

        HashSet<ElementSave> changedElements = new HashSet<ElementSave>();

        if(container is ElementSave containerElement)
        {
            changedElements.Add(containerElement);
        }

        foreach (var change in changes)
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

        GumCommands.Self.GuiCommands.RefreshVariables(force:true);
        foreach(var element in changedElements)
        {
            GumCommands.Self.FileCommands.TryAutoSaveElement(element);
        }

    }

    private void ShowFullEditUi(VariableSave variable, IStateCategoryListContainer container)
    {
        var host = Builder.App;
        var services = host.Services;

        var vm = services.GetRequiredService<AddVariableViewModel>();
        vm.Variable = variable;
        vm.Element = container as ElementSave;

        vm.SelectedItem = variable.Type;
        vm.EnteredName = variable.Name;

        var window = new AddVariableWindow(vm);
        window.Title = "Edit Variable";
        var result = window.ShowDialog();

        if (result == true)
        {
            vm.DoEdit(variable);
        }
    }
}
