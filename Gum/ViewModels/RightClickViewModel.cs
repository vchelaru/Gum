using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Logic;
using Gum.ToolStates;

namespace Gum.ViewModels;

public class RightClickViewModel
{
    private readonly ISelectedState _selectedState;
    private readonly ReorderLogic _reorderLogic;

    ContextMenuItemViewModel? _moveInFrontOf;

    public RightClickViewModel(ISelectedState selectedState, ReorderLogic reorderLogic)
    {
        _selectedState = selectedState;
        _reorderLogic = reorderLogic;
    }

    public List<ContextMenuItemViewModel> GetMenuItems()
    {
        var items = new List<ContextMenuItemViewModel>();

        if (_selectedState.SelectedInstance == null)
        {
            return items;
        }

        items.Add(new ContextMenuItemViewModel
        {
            Text = "Bring to Front",
            Action = () => _reorderLogic.MoveSelectedInstanceToFront()
        });

        items.Add(new ContextMenuItemViewModel
        {
            Text = "Move Forward",
            Action = () => _reorderLogic.MoveSelectedInstanceForward()
        });

        _moveInFrontOf = new ContextMenuItemViewModel
        {
            Text = "Move In Front Of"
        };
        PopulateMoveInFrontOfChildren(_moveInFrontOf);
        items.Add(_moveInFrontOf);

        items.Add(new ContextMenuItemViewModel
        {
            Text = "Move Backward",
            Action = () => _reorderLogic.MoveSelectedInstanceBackward()
        });

        items.Add(new ContextMenuItemViewModel
        {
            Text = "Send to Back",
            Action = () => _reorderLogic.MoveSelectedInstanceToBack()
        });

        return items;
    }

    private void PopulateMoveInFrontOfChildren(ContextMenuItemViewModel moveInFrontOf)
    {
        var selectedInstance = _selectedState.SelectedInstance;
        var selectedElement = _selectedState.SelectedElement;

        if (selectedInstance == null || selectedElement == null)
        {
            return;
        }

        var selectedParent = GetEffectiveParentNameFor(selectedInstance, selectedElement);

        foreach (var instance in selectedElement.Instances)
        {
            if (instance != selectedInstance)
            {
                var instanceParent = GetEffectiveParentNameFor(instance, selectedElement);
                var hasSameParent = instanceParent == selectedParent;

                if (hasSameParent)
                {
                    var capturedInstance = instance;
                    moveInFrontOf.Children.Add(new ContextMenuItemViewModel
                    {
                        Text = instance.Name,
                        Action = () => _reorderLogic.MoveSelectedInstanceInFrontOf(capturedInstance)
                    });
                }
            }
        }
    }

    public static string? GetEffectiveParentNameFor(InstanceSave instance, ElementSave owner)
    {
        var variableName = instance.Name + ".Parent";

        var state = owner.DefaultState;

        var parentVariableValue = state.Variables.Find(item => item.Name == variableName)?.Value;

        var parentName = (string?)parentVariableValue;

        // even though an instance may have a parent variable, we don't consider
        // it an actual parent if there is no instance with that name (it could be
        // a left-over variable).
        if (!string.IsNullOrEmpty(parentName))
        {
            var matchingInstance = owner.GetInstance(parentName);
            if (matchingInstance == null)
            {
                parentName = null;
            }
        }

        return parentName;
    }

    public void HandleVisibilityChanged()
    {
        if(_moveInFrontOf != null)
        {
            PopulateMoveInFrontOfChildren(_moveInFrontOf);
        }
    }
}
