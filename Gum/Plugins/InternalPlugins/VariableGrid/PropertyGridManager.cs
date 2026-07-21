using System;
using System.Collections.Generic;
using System.Linq;
using Gum.ToolStates;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.PropertyGridHelpers;
using WpfDataUi.DataTypes;
using System.Collections.ObjectModel;
using Gum.Plugins.VariableGrid;
using Gum.DataTypes.Behaviors;
using Gum.Controls;
using System.Drawing;
using Gum.Plugins.InternalPlugins.VariableGrid;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Gum.Logic;
using Gum.Services;
using Gum.Undo;
using Gum.Commands;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.Services.Dialogs;
using Gum.Wireframe;
using Gum.Localization;
using Gum.Reflection;
using Gum.Plugins;

namespace Gum.Managers;

public partial class PropertyGridManager : IBehaviorVariablePropertyGridSink
{
    #region Fields

    private readonly ISelectedState _selectedState;
    private readonly IExposeVariableService _exposeVariableService;
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly ObjectFinder _objectFinder;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly IDialogService _dialogService;
    private readonly LocalizationService _localizationService;
    private readonly ITabManager _tabManager;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly TypeManager _typeManager;
    private readonly IPluginManager _pluginManager;
    private readonly IProjectState _projectState;
    private readonly ICompositeMemberRegistry _compositeMemberRegistry;
    private readonly INameVerifier _nameVerifier;
    private readonly IDeleteVariableService _deleteVariableService;
    private readonly IEditVariableService _editVariableService;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly IVariableSaveLogic _variableSaveLogic;
    private readonly IClipboardService _clipboardService;
    WpfDataUi.DataUiGrid mVariablesDataGrid;
    MainPropertyGrid mainControl;

    ElementSaveDisplayer mPropertyGridDisplayer;

    //ToolStripMenuItem mExposeVariable;
    //ToolStripMenuItem mResetToDefault;
    //ToolStripMenuItem mUnExposeVariable;

    ElementSave mLastElement;

    List<InstanceSave> mLastInstanceSaves = new List<InstanceSave>();
    //InstanceSave mLastInstance;
    StateSave mLastState;
    StateSaveCategory? mLastCategory;
    BehaviorSave mLastBehaviorSave;

    // Making this public allows the plugin to access it. Eventually we will want to migrate
    // this whole class to a plugin to work more like Glue, but that will take time
    public MainControlViewModel VariableViewModel { get; private set; }

    /// <summary>
    /// This is a list of objects which callers can add themselves to. By doing so, property grid
    /// refreshes are suppressed. The reason for this is because the property grid refreshes when
    /// certain calls are made, but this can cause double-refreshes. We can improve performance by
    /// suppressing and then explicitly refreshing.
    /// </summary>
    public List<object> ObjectsSuppressingRefresh { get; private set; } = new List<object>();

    private CompositeMemberLogic _compositeMemberLogic;
    private StateSaveCategoryDisplayer _stateSaveCategoryDisplayer;
    private BehaviorShowingLogic _behaviorShowingLogic;

    #endregion

    #region Properties

    public VariableSave SelectedBehaviorVariable
    {
        get
        {
            return this.VariableViewModel.EffectiveSelectedBehaviorVariable;
        }
        set
        {
            this.VariableViewModel.SelectedBehaviorVariable = value;
        }
    }

    #endregion

    public PropertyGridManager(
        ISelectedState selectedState,
        IExposeVariableService exposeVariableService,
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IDialogService dialogService,
        LocalizationService localizationService,
        ITabManager tabManager,
        IWireframeObjectManager wireframeObjectManager,
        TypeManager typeManager,
        IPluginManager pluginManager,
        IProjectState projectState,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        ICompositeMemberRegistry compositeMemberRegistry,
        INameVerifier nameVerifier,
        IDeleteVariableService deleteVariableService,
        IEditVariableService editVariableService,
        IHotkeyManager hotkeyManager,
        IVariableSaveLogic variableSaveLogic,
        IClipboardService clipboardService)
    {
        _selectedState = selectedState;
        _exposeVariableService = exposeVariableService;
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _objectFinder = ObjectFinder.Self;
        _setVariableLogic = setVariableLogic;
        _dialogService = dialogService;
        _fileCommands = fileCommands;
        _localizationService = localizationService;
        _tabManager = tabManager;
        _wireframeObjectManager = wireframeObjectManager;
        _typeManager = typeManager;
        _pluginManager = pluginManager;
        _projectState = projectState;
        _compositeMemberRegistry = compositeMemberRegistry;
        _nameVerifier = nameVerifier;
        _deleteVariableService = deleteVariableService;
        _editVariableService = editVariableService;
        _hotkeyManager = hotkeyManager;
        _variableSaveLogic = variableSaveLogic;
        _clipboardService = clipboardService;
        _stateSaveCategoryDisplayer = new StateSaveCategoryDisplayer(variableInCategoryPropagationLogic);
        _behaviorShowingLogic = new BehaviorShowingLogic(fileCommands, projectState);
    }

    // Normally plugins will initialize through the PluginManager. This needs to happen earlier (see where it's called for info)
    // so some of it will happen here:
    public void InitializeEarly()
    {
        _compositeMemberLogic = new CompositeMemberLogic(_selectedState,
            _exposeVariableService,
            _undoManager,
            _guiCommands,
            _objectFinder,
            _compositeMemberRegistry,
            _dialogService,
            _nameVerifier,
            _clipboardService);

        mPropertyGridDisplayer = new ElementSaveDisplayer(
            new SubtextLogic(),
            _typeManager,
            _selectedState,
            _undoManager,
            _pluginManager,
            _variableSaveLogic,
            _editVariableService,
            _exposeVariableService,
            _hotkeyManager,
            _deleteVariableService,
            _guiCommands,
            _fileCommands,
            _setVariableLogic,
            _wireframeObjectManager,
            _clipboardService,
            _projectState);

        mainControl = new Gum.MainPropertyGrid();

        _tabManager.AddControl(mainControl, "Variables", TabLocation.CenterBottom);

        mVariablesDataGrid = mainControl.DataGrid;

        VariableViewModel = new Plugins.VariableGrid.MainControlViewModel(_deleteVariableService, _editVariableService);
        VariableViewModel.IsAddVariableButtonVisible = false;
        mainControl.DataContext = VariableViewModel;
        mainControl.SelectedBehaviorVariableChanged += HandleBehaviorVariableSelected;
        mainControl.AddVariableClicked += HandleAddVariable;
    }

    private void HandleBehaviorVariableSelected(object? sender, EventArgs e)
    {
        _selectedState.SelectedBehaviorVariable = this.SelectedBehaviorVariable;
    }

    private void HandleAddVariable(object? sender, EventArgs e)
    {
        var canShow = _selectedState.SelectedBehavior != null || _selectedState.SelectedElement != null;

        /////////////// Early Out///////////////
        if (!canShow)
        {
            return;
        }
        //////////////End Early Out/////////////

        _dialogService.Show<AddVariableViewModel>(vm =>
        {
            vm.RenameType = RenameType.NormalName;
            vm.Element = _selectedState.SelectedElement;
            vm.Variable = null;
        });
    }

    bool isInRefresh = false;


    public void RefreshEntireGrid(bool force = false)
    {
        if (isInRefresh || ObjectsSuppressingRefresh.Count > 0)
            return;


        isInRefresh = true;

        bool showVariableGrid = 
            (_selectedState.SelectedElement != null ||
            _selectedState.SelectedInstance != null) && 
            _selectedState.CustomCurrentStateSave == null;
        VariableViewModel.ShowVariableGrid = showVariableGrid;

        //if (_selectedState.SelectedInstances.GetCount() > 1)
        //{
            // I don't know if we want to eventually show these
            // but for now we'll hide the PropertyGrid:
        //    mainControl.Visibility = System.Windows.Visibility.Hidden;
        //}
        //else
        {
            mainControl.Visibility = System.Windows.Visibility.Visible;

            //mPropertyGrid.SelectedObject = mPropertyGridDisplayer;
            //mPropertyGrid.Refresh();

            var element = _selectedState.SelectedElement;
            var state = _selectedState.SelectedStateSave;
            var instance = _selectedState.SelectedInstances.ToList();
            var behaviorSave = _selectedState.SelectedBehavior;
            var category = _selectedState.SelectedStateCategorySave;

            RefreshDataGrid(element, state, category, instance, behaviorSave, force);
        }

        isInRefresh = false;
    }


    static object lockObject = new object();
    static List<string> records = new List<string>();

    static int SimultaneousCalls = 0;

    /// <summary>
    /// Refreshes the property grid for the argument element, state, and instance. This will only refresh the grid
    /// if the element, state, or instance values have changed since the last time this function was called, or if
    /// force is true.
    /// </summary>
    /// <param name="element">The element to display. The properties on this element may not be displayed if the instance is not null.</param>
    /// <param name="state">The state to display.</param>
    /// <param name="instance">The instance to display. May be null.</param>
    /// <param name="force">Whether to refresh even if the element, state, and instance have not changed.</param>
    private void RefreshDataGrid(ElementSave? element, 
        StateSave? state, 
        StateSaveCategory? stateCategory, 
        List<InstanceSave> newInstances, 
        BehaviorSave? behaviorSave, bool force = false)
    {
        ObjectFinder.Self.EnableCache();
        try
        {

            bool hasChangedObjectShowing =
                element != mLastElement ||
                state != mLastState ||
                stateCategory != mLastCategory ||
                behaviorSave != mLastBehaviorSave ||
                force;

            if (!hasChangedObjectShowing)
            {
                if (newInstances.Count != mLastInstanceSaves.Count)
                {
                    hasChangedObjectShowing = true;
                }
                else
                {
                    for (int i = 0; i < newInstances.Count; i++)
                    {
                        if (newInstances[i] != mLastInstanceSaves[i])
                        {
                            hasChangedObjectShowing = true;
                            break;
                        }
                    }
                }
            }

            var hasCustomState = _selectedState.CustomCurrentStateSave != null;

            if (hasCustomState)
            {
                hasChangedObjectShowing = false;
            }

            mVariablesDataGrid.IsEnabled = !hasCustomState;

            List<List<MemberCategory>> listOfCategories = new List<List<MemberCategory>>();

            if (newInstances.Count > 0)
            {
                foreach (var instance in newInstances)
                {
                    List<MemberCategory> categories = new List<MemberCategory>();

                    if (element == null)
                    {
                        categories = new List<MemberCategory>();

                        var behavior = ObjectFinder.Self.GetBehaviorContainerOf(instance);
                        if (behavior != null)
                        {
                            categories = GetMemberCategories(behavior, instance);
                        }

                    }
                    else
                    {
                        categories = GetMemberCategories(element, state!, stateCategory, instance);
                    }

                    if (newInstances.Count > 1)
                    {
                        RemoveMembersNotAllowedInMultiEdit(categories);
                    }


                    listOfCategories.Add(categories);
                }
            }
            else
            {
                List<MemberCategory> categories = new List<MemberCategory>();
                if (element == null)
                {
                    // do nothing....
                    mLastInstanceSaves.Clear();
                    mLastInstanceSaves.AddRange(newInstances);
                }
                else
                {

                    categories = GetMemberCategories(element, state, stateCategory, null);
                }
                listOfCategories.Add(categories);

            }

            if (element == null)
            {
                mLastElement = null;
            }

            mLastBehaviorSave = behaviorSave;

            if (hasChangedObjectShowing)
            {
                // UI is fast, I dont' think we need this....
                //Application.DoEvents();
                SimultaneousCalls++;
                lock (lockObject)
                {
                    if (SimultaneousCalls > 1)
                    {
                        SimultaneousCalls--;
                        return;
                    }
                    records.Add("in");

                    mVariablesDataGrid.Instance = (object)behaviorSave ?? _selectedState.SelectedStateSave;

                    // April 10, 2023
                    // I am adding multi-select
                    // editing support. To do this,
                    // we call SetMultipleCategoryLists
                    // which creates wrappers for multi-select
                    // editing. Currently I do this only if more
                    // than one object is selected, in case there
                    // are bugs in multi-select editing which would
                    // cause problems. We may consider having even single
                    // edits use the same code path in the future, but I don't
                    // feel confident in doing that just yet.
                    if (listOfCategories.Count == 1)
                    {
                        if (SimultaneousCalls > 1)
                        {
                            SimultaneousCalls--;
                            // EARLY OUT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                            return;
                        }
                        mVariablesDataGrid.SetCategories(listOfCategories[0]);
                    }
                    else
                    {
                        mVariablesDataGrid.SetMultipleCategoryLists(listOfCategories);

                        // remove the individual calls for setting variables, move it to the 
                        // multi-select object:
                        foreach (var categoryList in listOfCategories)
                        {
                            foreach (var innerCategory in categoryList)
                            {
                                foreach (var member in innerCategory.Members)
                                {
                                    if (member is StateReferencingInstanceMember srim)
                                    {
                                        srim.IsCallingRefresh = false;
                                    }
                                }
                            }
                        }

                        foreach (var gridCategory in mVariablesDataGrid.Categories)
                        {
                            foreach (MultiSelectInstanceMember member in gridCategory.Members)
                            {
                                IDisposable? undoLock = null;

                                member.BeforeMultiSet += (args) =>
                                {
                                    // Only lock for Full commits to avoid locking during intermediate changes (like dragging sliders)
                                    if (args.CommitType == SetPropertyCommitType.Full)
                                    {
                                        undoLock = _undoManager.RequestLock();
                                    }
                                };

                                member.AfterMultiSet += (args) =>
                                {
                                    // Dispose lock if it was created
                                    if (undoLock != null)
                                    {
                                        undoLock.Dispose();
                                        undoLock = null;
                                    }

                                    // Record undo after all values have been set
                                    if (args.CommitType == SetPropertyCommitType.Full)
                                    {
                                        _undoManager.RecordUndo();
                                    }

                                    // Loop through all instances and refresh
                                    foreach (var item in member.InstanceMembers)
                                    {
                                        if (item is StateReferencingInstanceMember srim)
                                        {
                                            srim.NotifyVariableLogic((object)srim.InstanceSave ?? srim.ElementSave, args.CommitType);
                                        }
                                    }
                                };
                            }
                        }
                    }

                }

                SimultaneousCalls--;
            }
            else
            {
                // todo: handle multiselect
                foreach (var newCategory in listOfCategories[0])
                {
                    // let's see if any variables have changed
                    var oldCategory = mVariablesDataGrid.Categories.FirstOrDefault(item => item.Name == newCategory.Name);

                    if (oldCategory != null && DoCategoriesDiffer(oldCategory.Members, newCategory.Members))
                    {
                        int index = mVariablesDataGrid.Categories.IndexOf(oldCategory);

                        mVariablesDataGrid.Categories.RemoveAt(index);
                        mVariablesDataGrid.Categories.Insert(index, newCategory);
                    }
                }
            }

            RefreshErrors(element);

            RefreshStateLabel(element, stateCategory, state);

            RefreshCategoryNotification(stateCategory, state);

            RefreshBehaviorUi(behaviorSave, newInstances, state, stateCategory);

            // When a structural rebuild happened (hasChangedObjectShowing = true), each control's
            // InstanceMember setter already called Refresh(). Calling mVariablesDataGrid.Refresh()
            // here would trigger a second Refresh() on every control via SimulateValueChanged →
            // PropertyChanged → HandlePropertyChange. Skip it in that case.
            // When hasChangedObjectShowing = false, existing InstanceMember objects stay in place
            // and their values may have changed (e.g. after undo), so Refresh() is still needed.
            if (!hasChangedObjectShowing)
            {
                mVariablesDataGrid.Refresh();
            }
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }
        
    }

    private void RemoveMembersNotAllowedInMultiEdit(List<MemberCategory> categories)
    {
        foreach(var category in categories)
        {
            category.Members.RemoveAll(item => item.DisplayName == "Name" );
        }
    }

    private void RefreshStateLabel(ElementSave element, StateSaveCategory category, StateSave state)
    {
        if(element == null)
        {
            VariableViewModel.HasStateInformation = false;
        }
        else if(state == null || state == element.DefaultState)
        {
            VariableViewModel.HasStateInformation = false;
        }
        else if(_selectedState.CustomCurrentStateSave != null)
        {
            VariableViewModel.HasStateInformation = true;
            VariableViewModel.StateInformation = $"Displaying custom (animated) state";
            VariableViewModel.StateBackground = Color.Pink;
        }
        else
        {
            VariableViewModel.StateBackground = Color.Yellow;
            VariableViewModel.HasStateInformation = true;
            string stateName = state.Name;
            if(category != null)
            {
                stateName = category.Name + "/" + stateName;
            }
            VariableViewModel.StateInformation = $"Editing state {stateName}";
        }
    }

    private void RefreshCategoryNotification(StateSaveCategory? stateCategory, StateSave? state)
    {
        if (stateCategory == null || state != null)
        {
            VariableViewModel.HasCategoryNotification = false;
            return;
        }

        if (stateCategory.States.Count == 0)
        {
            VariableViewModel.CategoryNotification = "This category has no states.";
            VariableViewModel.HasCategoryNotification = true;
        }
        else
        {
            var firstState = stateCategory.States[0];
            var hasVariables = firstState.Variables.Any() || firstState.VariableLists.Any();

            if (!hasVariables)
            {
                VariableViewModel.CategoryNotification =
                    "This category does not set any variables. To set a variable, select a state and change the desired variable.";
                VariableViewModel.HasCategoryNotification = true;
            }
            else
            {
                VariableViewModel.HasCategoryNotification = false;
            }
        }
    }

    public void RefreshVariablesDataGridValues()
    {
        ObjectFinder.Self.EnableCache();
        try
        {
            mVariablesDataGrid.Refresh();
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }
    }

    private void RefreshBehaviorUi(BehaviorSave behaviorSave, List<InstanceSave> instances, StateSave state, StateSaveCategory category)
    {

        this.VariableViewModel.BehaviorVariables.Clear();

        var isShown = behaviorSave != null && (instances == null || instances.Count == 0);

        if(isShown)
        {
            this.VariableViewModel.BehaviorVariables.AddRange(behaviorSave.RequiredVariables.Variables);
        }

        this.VariableViewModel.BehaviorSave = behaviorSave;
        this.VariableViewModel.ShowBehaviorUi = isShown;

        if(isShown)
        {
            mainControl.BehaviorDataGrid.Instance = behaviorSave;
            mainControl.BehaviorDataGrid.Categories.Clear();
            mainControl.BehaviorDataGrid.Categories.AddRange(
                ToWpfSynthetic(_behaviorShowingLogic.GetCategoriesFor(behaviorSave)));

            if(category != null &&
                // For now let's require explicitly selecting the catgory:
                state == null)
            {
                mainControl.BehaviorDataGrid.Categories.AddRange(
                    ToWpfSynthetic(StateSaveCategoryDisplayer.GetCategoriesFor(behaviorSave, category)));
            }


            mainControl.BehaviorDataGrid.InsertSpacesInCamelCaseMemberNames();
        }
        else
        {
            mainControl.BehaviorDataGrid.Categories.Clear();
        }

    }

    private void RefreshErrors(ElementSave element)
    {
        var asComponent = element as ComponentSave;

        if(asComponent != null)
        {
            var behaviors = _projectState.GumProjectSave.Behaviors;
            var behaviorReferences = asComponent.Behaviors;

            string message = null;

            foreach(var behaviorReference in behaviorReferences)
            {
                var foundBehavior = behaviors.FirstOrDefault(item => item.Name == behaviorReference.BehaviorName);

                if(foundBehavior != null)
                {
                    var requiredVariables = foundBehavior.RequiredVariables.Variables;

                    foreach(var requiredVariable in requiredVariables)
                    {
                        bool existsInComponent = asComponent.DefaultState.Variables
                            .Any(item =>
                                (item.Name == requiredVariable.Name || item.ExposedAsName == requiredVariable.Name) &&
                                item.Type == requiredVariable.Type);

                        if(!existsInComponent)
                        {
                            message += $"Variable {requiredVariable.Name} of type {requiredVariable.Type} is required.\n";
                        }
                    }
                }
            }

            bool showError = !string.IsNullOrEmpty(message);
            this.VariableViewModel.HasErrors = showError;



            this.VariableViewModel.ErrorInformation = message;
        }
    }

    public bool DoCategoriesDiffer(IEnumerable<InstanceMember> first, IEnumerable<InstanceMember> second)
    {
        foreach (var item in first)
        {
            if (!second.Any(other => other.Name == item.Name))
            {
                return true;
            }
        }

        foreach (var item in second)
        {
            if (!first.Any(other => other.Name == item.Name))
            {
                return true;
            }
        }

        return false;
    }

    private List<MemberCategory> GetMemberCategories(BehaviorSave behavior, InstanceSave instance)
    {
        mLastElement = null;
        mLastState = null;
        mLastInstanceSaves.Clear();
        if (instance != null)
        {
            mLastInstanceSaves.Add(instance);
        }
        mLastCategory = null;

        var descriptors = mPropertyGridDisplayer.GetCategories(behavior, instance);

        return ToWpf(descriptors);
    }

    private List<MemberCategory> GetMemberCategories(ElementSave instanceOwner, StateSave state, StateSaveCategory? stateCategory, InstanceSave instance)
    {
        List<MemberCategory> categories = new List<MemberCategory>();

        mLastElement = instanceOwner;
        mLastState = state;
        mLastInstanceSaves.Clear();
        if(instance != null)
        {
            mLastInstanceSaves.Add(instance);
        }
        mLastCategory = stateCategory;

        var stateSave = _selectedState.SelectedStateSave;
        if (stateSave != null)
        {
            categories = GetMemberCategoriesForState(instanceOwner, instance, stateSave, stateCategory);
        }
        else if(stateCategory != null)
        {
            var descriptor = _stateSaveCategoryDisplayer.BuildCommonMembersCategory(instance, stateCategory);
            if (descriptor != null)
            {
                categories = ToWpfSynthetic(new List<SyntheticCategoryDescriptor> { descriptor });
            }
        }
        return categories;

    }

    /// <summary>
    /// Materializes headless <see cref="VariableCategoryDescriptor"/>s (built by the relocated,
    /// WPF-free <see cref="ElementSaveDisplayer"/>) into the real WPF <see cref="MemberCategory"/>/
    /// <see cref="StateReferencingInstanceMember"/> rows the live grid needs. See ADR-0005 and the
    /// "ui-decoupling-plan.md" known-gotchas list.
    /// </summary>
    private List<MemberCategory> ToWpf(List<VariableCategoryDescriptor> descriptors)
    {
        var categories = new List<MemberCategory>();
        foreach (var descriptor in descriptors)
        {
            var category = new MemberCategory(descriptor.Name);
            if (!string.IsNullOrEmpty(descriptor.HeaderColorHex))
            {
                category.HeaderColor =
                    (System.Windows.Media.Brush)(new System.Windows.Media.BrushConverter().ConvertFrom(descriptor.HeaderColorHex)!);
            }

            foreach (var entry in descriptor.Members)
            {
                category.Members.Add(new StateReferencingInstanceMember(entry));
            }

            categories.Add(category);
        }
        return categories;
    }

    /// <summary>
    /// Materializes headless <see cref="SyntheticCategoryDescriptor"/>s (built by the relocated
    /// <see cref="StateSaveCategoryDisplayer"/>/<see cref="BehaviorShowingLogic"/>) into real WPF
    /// <see cref="MemberCategory"/>/<see cref="InstanceMember"/> rows. Unlike <see cref="ToWpf"/>,
    /// these rows aren't backed by a real <see cref="StateReferencingInstanceMember"/> - they're
    /// ad hoc synthetic rows (a category's "remove from category" button, a behavior's synthetic
    /// property), so a plain <see cref="InstanceMember"/> wired to the descriptor's delegates is
    /// enough.
    /// </summary>
    private List<MemberCategory> ToWpfSynthetic(List<SyntheticCategoryDescriptor> descriptors)
    {
        var categories = new List<MemberCategory>();
        foreach (var descriptor in descriptors)
        {
            var category = new MemberCategory(descriptor.Name);

            foreach (var row in descriptor.Members)
            {
                var instanceMember = new InstanceMember
                {
                    Name = row.Name,
                    DetailText = row.DetailText
                };
                instanceMember.CustomGetTypeEvent += (_) => row.ValueType;
                instanceMember.CustomGetEvent += (_) => row.Get();
                if (row.Set != null)
                {
                    instanceMember.CustomSetEvent += (_, newValue) => row.Set(newValue);
                }
                if (row.CustomOptions != null)
                {
                    instanceMember.CustomOptions = row.CustomOptions;
                }
                if (row.PreferredDisplayerKindOverride == VariableDisplayerKind.RemoveButton)
                {
                    instanceMember.PreferredDisplayer = typeof(VariableRemoveButton);
                }

                category.Members.Add(instanceMember);
            }

            categories.Add(category);
        }
        return categories;
    }

    private List<MemberCategory> GetMemberCategoriesForState(ElementSave instanceOwner, InstanceSave instance, StateSave stateSave, StateSaveCategory stateSaveCategory)
    {
        var descriptors = mPropertyGridDisplayer.GetCategories(instanceOwner, instance, stateSave, stateSaveCategory);
        var categories = ToWpf(descriptors);

        foreach (var category in categories)
        {
            var enumerable = category.Members.OrderBy(item => ((StateReferencingInstanceMember)item).SortValue).ToList();
            category.Members.Clear();

            foreach (var value in enumerable)
            {
                category.Members.Add(value);
            }
        }

        CustomizeVariables(categories, stateSave, instanceOwner, instance);

        return categories;
    }

    private void CustomizeVariables(List<MemberCategory> categories, StateSave stateSave, ElementSave element, InstanceSave instance)
    {
        // Hack! I would like to have this set by variables, but that's going to require a ton
        // of refatoring. We need to move off of the intermediate PropertyDescriptor class.
        AdjustStringPreferredDisplayer(categories, stateSave, instance);

        _compositeMemberLogic.Apply(categories, element, instance);

        UpdateFileFilters(categories);

        AdjustFontSourceToggle(categories, stateSave, instance);
    }

    private void UpdateFileFilters(List<MemberCategory> categories)
    {
        foreach (var category in categories)
        {
            foreach (var member in category.Members)
            {
                var rootVariableName = (member as StateReferencingInstanceMember)?.RootVariableName;
                if(rootVariableName == "CustomFontFile")
                {
                    member.PropertiesToSetOnDisplayer["Filter"] = "Bitmap Font Generator Font|*.fnt";
                }
                else if(rootVariableName == "SourceShaderFile")
                {
                    member.PropertiesToSetOnDisplayer["Filter"] = "Effect File (*.fx)|*.fx";
                }
            }
        }
    }

    private void AdjustFontSourceToggle(List<MemberCategory> categories, StateSave stateSave, InstanceSave? instance)
    {
        var fontCategory = categories.FirstOrDefault(c => c.Name == "Font");
        if (fontCategory == null)
        {
            return;
        }

        var fontMember = fontCategory.Members
            .OfType<StateReferencingInstanceMember>()
            .FirstOrDefault(m => m.RootVariableName == "Font");
        if (fontMember == null)
        {
            return;
        }

        // Determine current mode from the Font value
        string prefix = instance != null ? instance.Name + "." : "";
        string currentFontValue = stateSave.GetValueRecursive(prefix + "Font") as string ?? "Arial";
        bool isTtfMode = BmfcSave.IsFontFilePath(currentFontValue);

        // If currently in TTF mode, swap the Font row to FileSelectionDisplay
        if (isTtfMode)
        {
            fontMember.PreferredDisplayer = typeof(WpfDataUi.Controls.FileSelectionDisplay);
            fontMember.PropertiesToSetOnDisplayer["Filter"] = "TrueType Font|*.ttf";

            // Warn if project uses bmfont.exe, which can't handle .ttf file fonts
            var fontGenerator = _projectState.GumProjectSave?.FontGenerator ?? DataTypes.FontGeneratorType.BmFont;
            if (fontGenerator == DataTypes.FontGeneratorType.BmFont)
            {
                fontMember.DetailText = "bmfont cannot generate from .ttf files. Switch to KernSmith in Project Properties.";
            }
        }

        // Create the toggle member
        var toggleMember = new InstanceMember("Font Source", stateSave);
        toggleMember.PreferredDisplayer = typeof(WpfDataUi.Controls.ComboBoxDisplay);
        toggleMember.CustomOptions = new List<object> { "System Font", "From File" };
        toggleMember.CustomGetTypeEvent += (_) => typeof(string);
        toggleMember.CustomGetEvent += (_) => isTtfMode ? "From File" : "System Font";
        toggleMember.CustomSetPropertyEvent += (_, args) =>
        {
            string? newValue = args.Value as string;
            if (newValue == "From File" && !isTtfMode)
            {
                isTtfMode = true;
                // Swap Font row to FileSelectionDisplay
                fontMember.PreferredDisplayer = typeof(WpfDataUi.Controls.FileSelectionDisplay);
                fontMember.PropertiesToSetOnDisplayer["Filter"] = "TrueType Font|*.ttf";

                // Warn if project uses bmfont.exe
                var fontGenerator = _projectState.GumProjectSave?.FontGenerator ?? DataTypes.FontGeneratorType.BmFont;
                if (fontGenerator == DataTypes.FontGeneratorType.BmFont)
                {
                    fontMember.DetailText = "bmfont cannot generate from .ttf files. Switch to KernSmith in Project Properties.";
                }

                // Force container recreation by remove + reinsert
                int idx = fontCategory.Members.IndexOf(fontMember);
                if (idx >= 0)
                {
                    fontCategory.Members.RemoveAt(idx);
                    fontCategory.Members.Insert(idx, fontMember);
                }
            }
            else if (newValue == "System Font" && isTtfMode)
            {
                isTtfMode = false;
                // Reset Font value to a system font default
                fontMember.Value = "Arial";
                // Swap Font row back to ComboBox
                fontMember.PreferredDisplayer = null;
                fontMember.PropertiesToSetOnDisplayer.Remove("Filter");
                fontMember.DetailText = null;

                // Force container recreation
                int idx = fontCategory.Members.IndexOf(fontMember);
                if (idx >= 0)
                {
                    fontCategory.Members.RemoveAt(idx);
                    fontCategory.Members.Insert(idx, fontMember);
                }
            }
        };

        // Insert toggle before Font member
        int fontIndex = fontCategory.Members.IndexOf(fontMember);
        fontCategory.Members.Insert(fontIndex, toggleMember);
    }

    private void AdjustStringPreferredDisplayer(List<MemberCategory> categories, StateSave stateSave, InstanceSave instanceSave)
    {
        foreach (var category in categories)
        {
            foreach (var member in category.Members)
            {
                var isStringMember = member.PreferredDisplayer == null &&
                    member.PropertyType == typeof(string) &&
                    (member.CustomOptions?.Count > 0) == false;

                if (!isStringMember)
                {
                    continue;
                }

                // Standard variables resolve through ObjectFinder. Behavior FormsProperties
                // (e.g. ToolTip) aren't a project-defined variable, so the lookup returns
                // null — fall back to the trailing identifier of the member's qualified name.
                var baseVariable = ObjectFinder.Self.GetRootVariable(member.Name, stateSave.ParentContainer);
                var rootName = baseVariable?.Name ?? GetTrailingName(member.Name);

                if (IsEligibleStringDisplayerRootName(rootName))
                {
                    ApplyLocalizedOrMultilineStringDisplayer(
                        member,
                        _localizationService.HasDatabase,
                        _localizationService.Keys.OrderBy(item => item).ToArray());
                }
            }
        }
    }

    private static string? GetTrailingName(string? qualifiedName)
    {
        if (string.IsNullOrEmpty(qualifiedName))
        {
            return qualifiedName;
        }
        int lastDot = qualifiedName.LastIndexOf('.');
        return lastDot < 0 ? qualifiedName : qualifiedName.Substring(lastDot + 1);
    }

    // Variables eligible for the localization-key dropdown / multi-line editor pair.
    // Both code paths share the same UX: a project with a localization database surfaces
    // a sorted, editable combo of keys; without one, authors get a multi-line text box.
    internal static bool IsEligibleStringDisplayerRootName(string? rootName)
    {
        return rootName == "Text" || rootName == "ToolTip";
    }

    internal static void ApplyLocalizedOrMultilineStringDisplayer(
        InstanceMember member,
        bool hasLocalizationDatabase,
        IEnumerable<string> sortedKeys)
    {
        if (hasLocalizationDatabase)
        {
            member.PreferredDisplayer = typeof(WpfDataUi.Controls.ComboBoxDisplay);
            member.PropertiesToSetOnDisplayer[nameof(WpfDataUi.Controls.ComboBoxDisplay.IsEditable)] = true;
            // string[] -> IList<object> via array covariance (matches the prior
            // _localizationService.Keys.OrderBy(...).ToArray() assignment).
            member.CustomOptions = sortedKeys.ToArray();
        }
        else
        {
            member.PreferredDisplayer = typeof(WpfDataUi.Controls.MultiLineTextBoxDisplay);
        }
    }



    internal void HandleVariableSet(ElementSave element, InstanceSave? instance, string strippedName, object? oldValue)
    {
        if (strippedName == "VariableReferences")
        {
            // force refresh:
            RefreshEntireGrid(force: true);
        }
        if (_selectedState.SelectedStateCategorySave != null && _selectedState.SelectedStateSave != null)
        {
            // If setting a value on a variable in a category, the variable may be newly-added to the state.
            // If we don't already indicate that this is set by this category, we should update the grid immediately:
            var nameToSearchFor = strippedName;
            if(instance != null)
            {
                nameToSearchFor = instance.Name + "." + strippedName;
            }

            var existingSrim = mVariablesDataGrid.GetInstanceMember(nameToSearchFor);

            if(existingSrim != null)
            {
                var alreadyShowsSetBy = 
                    // We could get even more specific here, but doing so may
                    // result in the app breaking if we change how we display it
                    // so...this is good enough for now, should handle the most common cases:
                    existingSrim.DetailText?.Contains("Set by") == true;
                if(!alreadyShowsSetBy)
                {
                    RefreshEntireGrid(true);
                }
            }
        }
    }

    //private void ReactIfChangedMemberIsAnimation(ElementSave parentElement, string changedMember, object oldValue, out bool saveProject)
    //{
    //    const string sourceFileString = "SourceFile";
    //    if (changedMember == sourceFileString)
    //    {
    //        StateSave stateSave = _selectedState.SelectedStateSave;

    //        string value = (string)stateSave.GetValueRecursive(sourceFileString);

    //        if (!FileManager.IsRelative)
    //        {

    //        }

    //        saveProject = true;
    //    }

    //    saveProject = false;
    //}

    /// <summary>
    /// Called when the user clicks the "Make Default" menu item
    /// </summary>
    /// <param name="variableName">The variable to make default.</param>
}
