using Gum.Plugins.BaseClasses;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.DataTypes.Behaviors;
using WpfDataUi;
using System.Windows.Forms;
using Gum.Undo;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System.Collections.Generic;
using Gum.ToolCommands;
using Gum.Services;

namespace Gum.Plugins.Behaviors;

[Export(typeof(PluginBase))]
public class MainBehaviorsPlugin : InternalPlugin
{
    BehaviorsControl control;
    private readonly ISelectedState _selectedState;
    private readonly ElementCommands _elementCommands;
    private readonly UndoManager _undoManager;
    
    BehaviorsViewModel viewModel;
    DataUiGrid stateDataUiGrid;
    PluginTab behaviorsTab;

    public MainBehaviorsPlugin()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _elementCommands = Locator.GetRequiredService<ElementCommands>();
        _undoManager = Locator.GetRequiredService<UndoManager>();
    }

    public override void StartUp()
    {

        viewModel = new BehaviorsViewModel(_selectedState);
        viewModel.ApplyChangedValues += HandleApplyBehaviorChanges;


        control = new BehaviorsControl();
        control.DataContext = viewModel;
        behaviorsTab = this.CreateTab(control, "Behaviors", TabLocation.CenterBottom);

        stateDataUiGrid = new DataUiGrid();
        AssignEvents();
    }

    private void AssignEvents()
    {
        this.ElementSelected += HandleElementSelected;
        this.InstanceSelected += HandleInstanceSelected;
        this.BehaviorSelected += HandleBehaviorSelected;
        this.StateWindowTreeNodeSelected += HandleStateSelected;
        this.BehaviorReferencesChanged += HandleBehaviorReferencesChanged;

        this.RefreshBehaviorView += HandleRefreshBehaviorView;

        this.StateAdd += HandleStateAdd;
        this.StateMovedToCategory += HandleStateMovedToCategory;
    }

    private void HandleRefreshBehaviorView()
    {
        HandleElementSelected(_selectedState.SelectedElement);
    }

    private void HandleStateMovedToCategory(StateSave stateSave, StateSaveCategory newCategory, StateSaveCategory oldCategory)
    {
        var behavior = ObjectFinder.Self.GumProjectSave.Behaviors
            .FirstOrDefault(item => item.AllStates.Contains(stateSave));

        if (behavior != null)
        {
            AddStateToElementsImplementingBehavior(stateSave, behavior);
        }
    }

    private void HandleStateAdd(StateSave stateSave)
    {
        var behavior = ObjectFinder.Self.GumProjectSave.Behaviors
            .FirstOrDefault(item => item.AllStates.Contains(stateSave));

        if (behavior != null)
        {
            AddStateToElementsImplementingBehavior(stateSave, behavior);
        }
    }

    private void AddStateToElementsImplementingBehavior(StateSave stateSave, BehaviorSave behavior)
    {
        var category = behavior.Categories.FirstOrDefault(item => item.States.Contains(stateSave));

        var elementsUsingBehavior = ObjectFinder.Self.GumProjectSave.AllElements
            .Where(item => item.Behaviors.Any(b => b.BehaviorName == behavior.Name))
            .ToList();

        var categoryName = category?.Name;

        List<ElementSave> elementsToSave = new List<ElementSave>();

        foreach (var element in elementsUsingBehavior)
        {
            var categoryInElement = element.Categories.FirstOrDefault(item => item.Name == categoryName);

            if (categoryInElement != null)
            {
                var existingState = categoryInElement.States.FirstOrDefault(item => item.Name == stateSave.Name);

                if (existingState == null)
                {
                    // add a new state to this category
                    _elementCommands.AddState(element, categoryInElement, stateSave.Name);

                    elementsToSave.Add(element);
                }
            }
        }
    }

    private void HandleStateSelected(TreeNode obj)
    {

    }

    private void HandleBehaviorSelected(BehaviorSave obj)
    {

    }

    bool isApplyingChanges = false;
    private void HandleApplyBehaviorChanges(object sender, EventArgs e)
    {

        var component = _selectedState.SelectedComponent;
        if (component == null) return;

        isApplyingChanges = true;

        try
        {
            using var undoLock = _undoManager.RequestLock();

            var selectedBehaviorNames = viewModel.AllBehaviors
                .Where(item => item.IsChecked)
                .Select(item => item.Name)
                .ToList();

            var addedBehaviors = selectedBehaviorNames
                .Except(component.Behaviors.Select(item => item.BehaviorName))
                .ToList();

            var removedBehaviors = component.Behaviors.Select(item => item.BehaviorName)
                .Except(selectedBehaviorNames)
                .ToList();

            if (removedBehaviors.Any())
            {
                // ask the user what to do
            }

            if (removedBehaviors.Any() || addedBehaviors.Any())
            {
                component.Behaviors.Clear();
                foreach (var behavior in viewModel.AllBehaviors.Where(item => item.IsChecked))
                {
                    _elementCommands.AddBehaviorTo(behavior.Name, component, performSave: false);
                }

                _guiCommands.RefreshStateTreeView();
                _fileCommands.TryAutoSaveElement(component);
            }
            viewModel.UpdateTo(component);

        }
        finally
        {
            isApplyingChanges = false;
        }
    }


    private void HandleBehaviorReferencesChanged(ElementSave element)
    {
        if (isApplyingChanges)
        {
            return;
        }
        HandleElementSelected(element);

        if(element == _selectedState.SelectedElement)
        {
            this.behaviorsTab.Show();
        }
    }

    private void HandleElementSelected(ElementSave element)
    {
        // In case the user left without clicking "OK" on the previous edit:
        viewModel.IsEditing = false;
        UpdateTabPresence();
    }

    private void UpdateTabPresence()
    {
        var asComponent = _selectedState.SelectedComponent;


        bool shouldShow = asComponent != null &&
            // Don't show behaviors if an instance is selected since that can be confusing
            // Only show it on the element to be clear that behaviors are element-wide.
            _selectedState.SelectedInstance == null;

        if (shouldShow)
        {
            viewModel.UpdateTo(asComponent);

            this.behaviorsTab.Show(select: false);
        }
        else
        {
            this.behaviorsTab.Hide();
        }
    }

    private void HandleInstanceSelected(ElementSave save1, InstanceSave save2)
    {
        UpdateTabPresence();
    }


}
