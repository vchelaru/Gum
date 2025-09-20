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
using System.Windows.Media;
using Gum.Plugins.InternalPlugins.VariableGrid;
using RenderingLibrary.Graphics;
using Gum.Services;
using Gum.Undo;
using Gum.Commands;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.Services.Dialogs;

namespace Gum.Managers;

public partial class PropertyGridManager
{
    #region Fields

    private readonly ISelectedState _selectedState;
    private readonly IExposeVariableService _exposeVariableService;
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly ObjectFinder _objectFinder;
    private readonly SetVariableLogic _setVariableLogic;
    private readonly IDialogService _dialogService;
    private readonly LocalizationManager _localizationManager;
    private readonly ITabManager _tabManager;
    
    WpfDataUi.DataUiGrid mVariablesDataGrid;
    MainPropertyGrid mainControl;

    static PropertyGridManager mPropertyGridManager;

    ElementSaveDisplayer mPropertyGridDisplayer;

    //ToolStripMenuItem mExposeVariable;
    //ToolStripMenuItem mResetToDefault;
    //ToolStripMenuItem mUnExposeVariable;

    ElementSave mLastElement;

    List<InstanceSave> mLastInstanceSaves = new List<InstanceSave>();
    //InstanceSave mLastInstance;
    StateSave mLastState;
    StateSaveCategory mLastCategory;
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

    private ColorPickerLogic _colorPickerLogic;

    #endregion

    #region Properties

    [Obsolete("Remove this - all calls to this should instead rely on the PluginManager")]
    public static PropertyGridManager Self
    {
        get
        {
            if (mPropertyGridManager == null)
            {
                mPropertyGridManager = new PropertyGridManager();
            }
            return mPropertyGridManager;
        }
    }


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

    public PropertyGridManager()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _exposeVariableService =
            Locator.GetRequiredService<IExposeVariableService>();
        _undoManager =
            Locator.GetRequiredService<IUndoManager>();
        _guiCommands = Locator.GetRequiredService<IGuiCommands>();
        _objectFinder = ObjectFinder.Self;
        _setVariableLogic = Locator.GetRequiredService<SetVariableLogic>();
        _dialogService = Locator.GetRequiredService<IDialogService>();
        _fileCommands = Locator.GetRequiredService<IFileCommands>();
        _localizationManager = Locator.GetRequiredService<LocalizationManager>();
        _tabManager = Locator.GetRequiredService<ITabManager>();
    }

    // Normally plugins will initialize through the PluginManager. This needs to happen earlier (see where it's called for info)
    // so some of it will happen here:
    public void InitializeEarly()
    {
        _colorPickerLogic = new ColorPickerLogic(_selectedState,
            _exposeVariableService,
            _undoManager,
            _guiCommands,
            _objectFinder,
            _setVariableLogic);
        mPropertyGridDisplayer = new ElementSaveDisplayer(new SubtextLogic());

        mainControl = new Gum.MainPropertyGrid();

        _tabManager.AddControl(mainControl, "Variables", TabLocation.CenterBottom);

        mVariablesDataGrid = mainControl.DataGrid;

        var deleteVariableLogic = Locator.GetRequiredService<IDeleteVariableService>();
        var editVariableService = Locator.GetRequiredService<IEditVariableService>();

        VariableViewModel = new Plugins.VariableGrid.MainControlViewModel(deleteVariableLogic, editVariableService);
        VariableViewModel.AddVariableButtonVisibility = System.Windows.Visibility.Collapsed;
        mainControl.DataContext = VariableViewModel;
        mainControl.SelectedBehaviorVariableChanged += HandleBehaviorVariableSelected;
        mainControl.AddVariableClicked += HandleAddVariable;

        InitializeRightClickMenu();
    }

    private void HandleBehaviorVariableSelected(object sender, EventArgs e)
    {
        _selectedState.SelectedBehaviorVariable = PropertyGridManager.Self.SelectedBehaviorVariable;
    }

    private void HandleAddVariable(object sender, EventArgs e)
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
        VariableViewModel.ShowVariableGrid = showVariableGrid ?
            System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;

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
    private void RefreshDataGrid(ElementSave element, StateSave state, StateSaveCategory stateCategory, List<InstanceSave> newInstances, 
        BehaviorSave behaviorSave, bool force = false)
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
                        categories = GetMemberCategories(element, state, stateCategory, instance);
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

                    mVariablesDataGrid.Categories.Clear();


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
                        foreach (var memberCategory in listOfCategories[0])
                        {

                            // We used to do this:
                            // Application.DoEvents();
                            // That made things go faster,
                            // but it made the "lock" not work, which could make duplicate UI show up.
                            mVariablesDataGrid.Categories.Add(memberCategory);
                            if (SimultaneousCalls > 1)
                            {
                                SimultaneousCalls--;
                                // EARLY OUT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                return;
                            }
                        }
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
                                member.CustomSetPropertyEvent += (sender, args) =>
                                {
                                    //do just one undo:
                                    _undoManager.RecordUndo();

                                    // and loop through all instances and refrehs:
                                    foreach (var item in member.InstanceMembers)
                                    {
                                        if (item is StateReferencingInstanceMember srim)
                                        {
                                            srim.NotifyVariableLogic((object)srim.InstanceSave ?? srim.ElementSave, args.CommitType);

                                        }
                                        //RefreshInResponseToVariableChange()
                                    }
                                    //StateReferencingInstanceMember.NotifyVariableLogic(owner, )
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

            RefreshBehaviorUi(behaviorSave, newInstances, state, stateCategory);

            mVariablesDataGrid.Refresh();
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
            category.Members.RemoveAll(item => item.DisplayName == "Name" || item.DisplayName == "Base Type");
        }
    }

    private void RefreshStateLabel(ElementSave element, StateSaveCategory category, StateSave state)
    {
        if(element == null)
        {
            VariableViewModel.HasStateInformation = System.Windows.Visibility.Collapsed;
        }
        else if(state == element.DefaultState || state == null)
        {
            VariableViewModel.HasStateInformation = System.Windows.Visibility.Collapsed;
        }
        else if(_selectedState.CustomCurrentStateSave != null)
        {
            VariableViewModel.HasStateInformation = System.Windows.Visibility.Visible;
            VariableViewModel.StateInformation = $"Displaying custom (animated) state";
            VariableViewModel.StateBackground = Brushes.Pink;
        }
        else
        {
            VariableViewModel.StateBackground = Brushes.Yellow;
            VariableViewModel.HasStateInformation = System.Windows.Visibility.Visible;
            string stateName = state.Name;
            if(category != null)
            {
                stateName = category.Name + "/" + stateName;
            }
            VariableViewModel.StateInformation = $"Editing state {stateName}";
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
        this.VariableViewModel.ShowBehaviorUi = isShown ?
            System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        if(isShown)
        {
            mainControl.BehaviorDataGrid.Instance = behaviorSave;
            mainControl.BehaviorDataGrid.Categories.Clear();
            mainControl.BehaviorDataGrid.Categories.AddRange(BehaviorShowingLogic.GetCategoriesFor(behaviorSave));

            if(category != null && 
                // For now let's require explicitly selecting the catgory:
                state == null)
            {
                mainControl.BehaviorDataGrid.Categories.AddRange(
                    StateSaveCategoryDisplayer.GetCategoriesFor(behaviorSave, category));
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
            var behaviors = ProjectState.Self.GumProjectSave.Behaviors;
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
            this.VariableViewModel.HasErrors = showError ?
                System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;



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
        List<MemberCategory> categories = new List<MemberCategory>();

        mLastElement = null;
        mLastState = null;
        mLastInstanceSaves.Clear();
        if (instance != null)
        {
            mLastInstanceSaves.Add(instance);
        }
        mLastCategory = null;

        mPropertyGridDisplayer.GetCategories(behavior, instance, categories);

        return categories;
    }

    private List<MemberCategory> GetMemberCategories(ElementSave instanceOwner, StateSave state, StateSaveCategory stateCategory, InstanceSave instance)
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
            GetMemberCategoriesForState(instanceOwner, instance, categories, stateSave, stateCategory);
        }
        else if(stateCategory != null)
        {
            StateSaveCategoryDisplayer.DisplayMembersForCategoryInElement( instance, categories, stateCategory);
        }
        return categories;

    }

    private void GetMemberCategoriesForState(ElementSave instanceOwner, InstanceSave instance, List<MemberCategory> categories, StateSave stateSave, StateSaveCategory stateSaveCategory)
    {
        categories.Clear();
        mPropertyGridDisplayer.GetCategories(instanceOwner, instance, categories, stateSave, stateSaveCategory);

        foreach (var category in categories)
        {
            var enumerable = category.Members.OrderBy(item => ((StateReferencingInstanceMember)item).SortValue).ToList();
            category.Members.Clear();

            foreach (var value in enumerable)
            {
                category.Members.Add(value);
            }
        }

        ReorganizeCategories(categories);
        CustomizeVariables(categories, stateSave, instanceOwner, instance);
    }

    private void CustomizeVariables(List<MemberCategory> categories, StateSave stateSave, ElementSave element, InstanceSave instance)
    {
        // Hack! I would like to have this set by variables, but that's going to require a ton
        // of refatoring. We need to move off of the intermediate PropertyDescriptor class.
        AdjustTextPreferredDisplayer(categories, stateSave, instance);

        _colorPickerLogic.UpdateColorCategory(categories, element, instance);

        UpdateFileFilters(categories);
    }

    private void UpdateFileFilters(List<MemberCategory> categories)
    {
        foreach (var category in categories)
        {
            foreach (var member in category.Members)
            {
                if((member as StateReferencingInstanceMember)?.RootVariableName == "CustomFontFile")
                {
                    member.PropertiesToSetOnDisplayer.Add("Filter", "Bitmap Font Generator Font|*.fnt");
                }
            }
        }
    }

    private void AdjustTextPreferredDisplayer(List<MemberCategory> categories, StateSave stateSave, InstanceSave instanceSave)
    {
        // This used to only make Text objects multiline, but...maybe we should make all string values multiline?
        foreach(var category in categories)
        {
            foreach(var member in category.Members)
            {
                var isStringMember = member.PreferredDisplayer == null &&
                    member.PropertyType == typeof(string);

                if (isStringMember)
                {
                    var baseVariable = ObjectFinder.Self.GetRootVariable(member.Name, stateSave.ParentContainer);

                    var shouldShowLocalizationUi = (member.CustomOptions?.Count > 0) == false &&
                        baseVariable?.Name == "Text" &&
                        _localizationManager.HasDatabase;

                    // See StandardElementsManager for Text on explanation why this is commented out.
                    //if(shouldShowLocalizationUi)
                    //{
                    //    var prefix = instanceSave == null ? "" : instanceSave.Name + ".";
                    //    var valueAsObject = stateSave.GetValueRecursive(prefix + "Apply Localization");
                    //    if(valueAsObject is bool asBool)
                    //    {
                    //        shouldShowLocalizationUi = asBool;
                    //    }
                    //}


                    if (shouldShowLocalizationUi)
                    {
                        // give it options!
                        member.PreferredDisplayer = typeof(WpfDataUi.Controls.ComboBoxDisplay);
                        member.PropertiesToSetOnDisplayer[nameof(WpfDataUi.Controls.ComboBoxDisplay.IsEditable)] = true;
                        member.CustomOptions = _localizationManager.Keys.OrderBy(item => item).ToArray();
                    }
                    else if(baseVariable?.Name == "Text")
                    {
                        //member.PreferredDisplayer = typeof(ToggleButtonOptionDisplay);
                        member.PreferredDisplayer = typeof(WpfDataUi.Controls.MultiLineTextBoxDisplay);
                        //ToggleButtonOptionDisplay
                    }
                }
            }
        }
        //var category = categories.FirstOrDefault(item => item.Name == "Text");

        //if(category != null)
        //{
        //    var member = category.Members.FirstOrDefault(item => item.DisplayName == "Text");
        //    if(member != null)
        //    {
        //        member.PreferredDisplayer = typeof(WpfDataUi.Controls.MultiLineTextBoxDisplay);
        //    }
        //}
    }

    private static void ReorganizeCategories(List<MemberCategory> categories)
    {
        MemberCategory categoryToMove = categories.FirstOrDefault(item => item.Name == "Position");
        if (categoryToMove != null)
        {
            categories.Remove(categoryToMove);
            categories.Insert(1, categoryToMove);
        }

        categoryToMove = categories.FirstOrDefault(item => item.Name == "Dimensions");
        if (categoryToMove != null)
        {
            categories.Remove(categoryToMove);
            categories.Insert(2, categoryToMove);
        }
    }

    internal void HandleVariableSet(ElementSave element, InstanceSave instance, string strippedName, object oldValue)
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
