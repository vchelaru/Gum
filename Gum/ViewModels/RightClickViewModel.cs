using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.ViewModels;

public class RightClickViewModel
{
    private readonly ISelectedState _selectedState;
    private readonly IReorderLogic _reorderLogic;
    private readonly ObjectFinder _objectFinder;
    private readonly IElementCommands _elementCommands;
    private readonly INameVerifier _nameVerifier;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly ICircularReferenceManager _circularReferenceManager;
    private readonly IFavoriteComponentManager _favoriteComponentManager;
    ContextMenuItemViewModel? _moveInFrontOf;

    public RightClickViewModel(
        ISelectedState selectedState,
        IReorderLogic reorderLogic,
        ObjectFinder objectFinder,
        IElementCommands elementCommands,
        INameVerifier nameVerifier,
        ISetVariableLogic setVariableLogic,
        ICircularReferenceManager circularReferenceManager,
        IFavoriteComponentManager favoriteComponentManager)
    {
        _selectedState = selectedState;
        _reorderLogic = reorderLogic;
        _objectFinder = objectFinder;
        _elementCommands = elementCommands;
        _nameVerifier = nameVerifier;
        _setVariableLogic = setVariableLogic;
        _circularReferenceManager = circularReferenceManager;
        _favoriteComponentManager = favoriteComponentManager;
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


        items.Add(new ContextMenuItemViewModel
        {
            IsSeparator = true
        });


        items.Add(AddCreateInstanceMenuItems($"Add child object to '{_selectedState.SelectedInstance.Name}'"));

        return items;
    }

    private ContextMenuItemViewModel AddCreateInstanceMenuItems(string itemText)
    {
        var parentMenuItem = new ContextMenuItemViewModel();
        parentMenuItem.Text = itemText;

        // Add favorited components submenu
        var favoritedComponents = _favoriteComponentManager.GetFilteredFavoritedComponentsFor(
            _selectedState.SelectedElement,
            _circularReferenceManager);
        if (favoritedComponents.Count > 0)
        {
            var favoritesParent = new ContextMenuItemViewModel();
            favoritesParent.Text = "Favorited Components";
            parentMenuItem.Children.Add(favoritesParent);

            foreach (var component in favoritedComponents)
            {
                var menuItem = new ContextMenuItemViewModel();
                menuItem.Text = component.Name;
                favoritesParent.Children.Add(menuItem);

                var componentName = component.Name;
                menuItem.Action = () =>
                {
                    var selectedElement = _selectedState.SelectedElement;
                    if (selectedElement != null)
                    {
                        var newInstanceElementType = _objectFinder.GetElementSave(componentName)!;
                        var name = _elementCommands.GetUniqueNameForNewInstance(newInstanceElementType, selectedElement);

                        var viewModel = new AddInstanceDialogViewModel(
                            _selectedState,
                            _nameVerifier,
                            _elementCommands,
                            _setVariableLogic);
                        viewModel.TypeToCreate = componentName;
                        viewModel.Value = name;
                        viewModel.OnAffirmative();
                    }
                };
            }

            // Add separator between favorites and standard elements
            var separator = new ContextMenuItemViewModel();
            separator.IsSeparator = true;
            parentMenuItem.Children.Add(separator);
        }

        // Add child menu items for each type
        var types = new[] { "Sprite", "Text", "NineSlice", "ColoredRectangle", "Container" };

        foreach (var type in types)
        {
            var menuItem = new ContextMenuItemViewModel();
            menuItem.Text = type;
            parentMenuItem.Children.Add(menuItem);

            menuItem.Action = () =>
            {
                var selectedElement = _selectedState.SelectedElement;
                if (selectedElement != null)
                {
                    var newInstanceElementType = _objectFinder.GetElementSave(type)!;
                    var name = _elementCommands.GetUniqueNameForNewInstance(newInstanceElementType, selectedElement);

                    var viewModel = new AddInstanceDialogViewModel(
                        _selectedState,
                        _nameVerifier,
                        _elementCommands,
                        _setVariableLogic);
                    viewModel.TypeToCreate = type;
                    viewModel.Value = name;
                    viewModel.OnAffirmative();
                }
            };
        }

        return parentMenuItem;
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

}
