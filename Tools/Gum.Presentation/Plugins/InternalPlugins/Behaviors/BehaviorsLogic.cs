using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Commands;
using Gum.Plugins;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Managers;

namespace Gum.Plugins.Behaviors;

/// <summary>
/// Business logic behind the Behaviors panel, relocated out of the WPF-hosted
/// <c>MainBehaviorsPlugin</c> (ADR-0005 Phase 3, #3928) so it can be unit tested headlessly.
/// The plugin stays a thin wrapper: it builds the WPF view/tab in <c>StartUp</c>, constructs this
/// class, and forwards its own plugin events into the matching methods here.
/// </summary>
public class BehaviorsLogic
{
    private readonly ISelectedState _selectedState;
    private readonly IElementCommands _elementCommands;
    private readonly IUndoManager _undoManager;
    private readonly IPluginManager _pluginManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly BehaviorsViewModel _viewModel;
    private readonly ITabVisibility _tab;

    private bool _isApplyingChanges;

    public BehaviorsLogic(
        ISelectedState selectedState,
        IElementCommands elementCommands,
        IUndoManager undoManager,
        IPluginManager pluginManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        BehaviorsViewModel viewModel,
        ITabVisibility tab)
    {
        _selectedState = selectedState;
        _elementCommands = elementCommands;
        _undoManager = undoManager;
        _pluginManager = pluginManager;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _viewModel = viewModel;
        _tab = tab;
    }

    public void HandleRefreshBehaviorView()
    {
        HandleElementSelected(_selectedState.SelectedElement);
    }

    public void HandleStateMovedToCategory(StateSave stateSave, StateSaveCategory newCategory, StateSaveCategory oldCategory)
    {
        BehaviorSave? behavior = ObjectFinder.Self.GumProjectSave.Behaviors
            .FirstOrDefault(item => item.AllStates.Contains(stateSave));

        if (behavior != null)
        {
            AddStateToElementsImplementingBehavior(stateSave, behavior);
        }
    }

    public void HandleStateAdd(StateSave stateSave)
    {
        BehaviorSave? behavior = ObjectFinder.Self.GumProjectSave.Behaviors
            .FirstOrDefault(item => item.AllStates.Contains(stateSave));

        if (behavior != null)
        {
            AddStateToElementsImplementingBehavior(stateSave, behavior);
        }
    }

    private void AddStateToElementsImplementingBehavior(StateSave stateSave, BehaviorSave behavior)
    {
        StateSaveCategory? category = behavior.Categories.FirstOrDefault(item => item.States.Contains(stateSave));

        List<ElementSave> elementsUsingBehavior = ObjectFinder.Self.GumProjectSave.AllElements
            .Where(item => item.Behaviors.Any(b => b.BehaviorName == behavior.Name))
            .ToList();

        string? categoryName = category?.Name;

        foreach (ElementSave element in elementsUsingBehavior)
        {
            StateSaveCategory? categoryInElement = element.Categories.FirstOrDefault(item => item.Name == categoryName);

            if (categoryInElement != null)
            {
                StateSave? existingState = categoryInElement.States.FirstOrDefault(item => item.Name == stateSave.Name);

                if (existingState == null)
                {
                    // add a new state to this category
                    _elementCommands.AddState(element, categoryInElement, stateSave.Name);
                }
            }
        }
    }

    public void HandleApplyBehaviorChanges(object? sender, EventArgs e)
    {
        ComponentSave? component = _selectedState.SelectedComponent;
        if (component == null)
        {
            return;
        }

        _isApplyingChanges = true;

        try
        {
            using UndoLock undoLock = _undoManager.RequestLock();

            List<string> selectedBehaviorNames = _viewModel.AllBehaviors
                .Where(item => item.IsChecked)
                .Select(item => item.Name)
                .ToList();

            List<string> addedBehaviors = selectedBehaviorNames
                .Except(component.Behaviors.Select(item => item.BehaviorName))
                .ToList();

            List<string> removedBehaviors = component.Behaviors.Select(item => item.BehaviorName)
                .Except(selectedBehaviorNames)
                .ToList();

            if (removedBehaviors.Any() || addedBehaviors.Any())
            {
                component.Behaviors.Clear();
                foreach (CheckListBehaviorItem behavior in _viewModel.AllBehaviors.Where(item => item.IsChecked))
                {
                    _elementCommands.AddBehaviorTo(behavior.Name, component, performSave: false);
                }

                _guiCommands.RefreshStateTreeView();
                _fileCommands.TryAutoSaveElement(component);
                // _elementCommands.AddBehaviorTo only raises BehaviorReferencesChanged
                // when a real (project-existing) behavior is added. Pure removals — e.g.
                // unchecking only a stale orphan — would otherwise leave error state stale.
                _pluginManager.BehaviorReferencesChanged(component);
            }
            _viewModel.UpdateTo(component);
        }
        finally
        {
            _isApplyingChanges = false;
        }
    }

    public void HandleBehaviorReferencesChanged(ElementSave element)
    {
        if (_isApplyingChanges)
        {
            return;
        }
        HandleElementSelected(element);

        if (element == _selectedState.SelectedElement)
        {
            _tab.Show();
        }
    }

    public void HandleElementSelected(ElementSave? element)
    {
        // In case the user left without clicking "OK" on the previous edit:
        _viewModel.IsEditing = false;
        UpdateTabPresence();
    }

    private void UpdateTabPresence()
    {
        ComponentSave? asComponent = _selectedState.SelectedComponent;

        bool shouldShow = asComponent != null &&
            // Don't show behaviors if an instance is selected since that can be confusing
            // Only show it on the element to be clear that behaviors are element-wide.
            _selectedState.SelectedInstance == null;

        if (shouldShow)
        {
            _viewModel.UpdateTo(asComponent!);

            _tab.Show();
        }
        else
        {
            _tab.Hide();
        }
    }

    public void HandleInstanceSelected(ElementSave save1, InstanceSave save2)
    {
        UpdateTabPresence();
    }
}
