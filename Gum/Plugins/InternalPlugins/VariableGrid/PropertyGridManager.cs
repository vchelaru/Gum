using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gum.ToolStates;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.PropertyGridHelpers;
using Gum.Wireframe;
using RenderingLibrary.Graphics.Fonts;
using CommonFormsAndControls;
using Gum.ToolCommands;
using RenderingLibrary;
using Gum.Converters;
using Gum.Plugins;
using Gum.RenderingLibrary;
using RenderingLibrary.Content;
using WpfDataUi.DataTypes;
using Gum.DataTypes.ComponentModel;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.ObjectModel;
using WpfDataUi;
using Gum.Plugins.VariableGrid;
using Gum.DataTypes.Behaviors;
using Gum.Controls;
using WpfDataUi.Controls;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace Gum.Managers
{
    public partial class PropertyGridManager
    {
        #region Fields

        WpfDataUi.DataUiGrid mVariablesDataGrid;
        TestWpfControl mainControl;

        static PropertyGridManager mPropertyGridManager;

        ElementSaveDisplayer mPropertyGridDisplayer = new ElementSaveDisplayer();

        //ToolStripMenuItem mExposeVariable;
        //ToolStripMenuItem mResetToDefault;
        //ToolStripMenuItem mUnExposeVariable;

        ElementSave mLastElement;
        InstanceSave mLastInstance;
        StateSave mLastState;
        StateSaveCategory mLastCategory;
        BehaviorSave mLastBehaviorSave;

        MainControlViewModel variableViewModel;

        #endregion

        #region Properties

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
                return this.variableViewModel.EffectiveSelectedBehaviorVariable;
            }
            set
            {
                this.variableViewModel.SelectedBehaviorVariable = value;
            }
        }

        #endregion


        public void Initialize()
        {
            mainControl = new Gum.TestWpfControl();

            GumCommands.Self.GuiCommands.AddControl(mainControl, "Variables", TabLocation.CenterBottom);

            mVariablesDataGrid = mainControl.DataGrid;
            variableViewModel = new Plugins.VariableGrid.MainControlViewModel();
            mainControl.DataContext = variableViewModel;
            mainControl.SelectedBehaviorVariableChanged += HandleBehaviorVariableSelected;
            mainControl.AddVariableClicked += HandleAddVariable;

            InitializeRightClickMenu();
        }

        private void HandleBehaviorVariableSelected(object sender, EventArgs e)
        {
            SelectedState.Self.UpdateToSelectedBehaviorVariable();
        }

        private void HandleAddVariable(object sender, EventArgs e)
        {
            var window = new AddVariableWindow();

            var result = window.ShowDialog();

            if(result == true)
            {
                var type = window.SelectedType;
                var name = window.EnteredName;

                string whyNotValid;
                bool isValid = NameVerifier.Self.IsVariableNameValid(
                    name, out whyNotValid);

                if(!isValid)
                {
                    MessageBox.Show(whyNotValid);
                }
                else
                {
                    var behavior = SelectedState.Self.SelectedBehavior;

                    var newVariable = new VariableSave();
                    newVariable.Name = name;
                    newVariable.Type = type;

                    behavior.RequiredVariables.Variables.Add(newVariable);
                    GumCommands.Self.GuiCommands.RefreshPropertyGrid();
                    GumCommands.Self.FileCommands.TryAutoSaveBehavior(behavior);
                }
            }
        }

        bool isInRefresh = false;

        /// <summary>
        /// Attempts to refresh the grid. The grid will only refresh if a new element, instance, or state
        /// have been selected since the last refresh. If only values have changed, then a true value can be passed
        /// to force a refresh.
        /// </summary>
        /// <param name="force">Whether to force the refresh. If this is true, the grid will refresh. If this
        /// is false, the refresh will only happen if a new element, state, or instance has been selected.</param>
        public void RefreshUI(bool force = false)
        {
            if (isInRefresh)
                return;


            isInRefresh = true;

            bool showVariableGrid = 
                (SelectedState.Self.SelectedElement != null ||
                SelectedState.Self.SelectedInstance != null) && 
                SelectedState.Self.CustomCurrentStateSave == null;
            variableViewModel.ShowVariableGrid = showVariableGrid ?
                System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;

            if (SelectedState.Self.SelectedInstances.GetCount() > 1)
            {
                // I don't know if we want to eventually show these
                // but for now we'll hide the PropertyGrid:
                mainControl.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                mainControl.Visibility = System.Windows.Visibility.Visible;

                //mPropertyGrid.SelectedObject = mPropertyGridDisplayer;
                //mPropertyGrid.Refresh();

                var element = SelectedState.Self.SelectedElement;
                var state = SelectedState.Self.SelectedStateSave;
                var instance = SelectedState.Self.SelectedInstance;
                var behaviorSave = SelectedState.Self.SelectedBehavior;
                var category = SelectedState.Self.SelectedStateCategorySave;

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
        private void RefreshDataGrid(ElementSave element, StateSave state, StateSaveCategory category, InstanceSave instance, 
            BehaviorSave behaviorSave, bool force = false)
        {

            bool hasChangedObjectShowing = 
                element != mLastElement || 
                instance != mLastInstance || 
                state != mLastState ||
                category != mLastCategory ||
                behaviorSave != mLastBehaviorSave ||
                force;

            var hasCustomState = SelectedState.Self.CustomCurrentStateSave != null;

            if (hasCustomState)
            {
                hasChangedObjectShowing = false;
            }

            mVariablesDataGrid.IsInnerGridEnabled = !hasCustomState;

            List<MemberCategory> categories = element == null
                ? new List<MemberCategory>()
                : GetMemberCategories(element, state, category, instance);

            mLastBehaviorSave = behaviorSave;

            if (hasChangedObjectShowing)
            {
                // UI is fast, I dont' think we need this....
                //Application.DoEvents();
                SimultaneousCalls ++;
                lock (lockObject)
                {
                    if(SimultaneousCalls > 1)
                    {
                        SimultaneousCalls--;
                        return;
                    }
                    records.Add("in");

                    mVariablesDataGrid.Instance = (object)behaviorSave ?? SelectedState.Self.SelectedStateSave;

                    mVariablesDataGrid.Categories.Clear();

                    // There's a bug here where drag+dropping a new instance will create 
                    // duplicate UI members.  I am going to deal with it now because it is


                    foreach (var memberCategory in categories)
                    {

                        // We used to do this:
                        // Application.DoEvents();
                        // That made things go faster,
                        // but it made the "lock" not work, which could make duplicate UI show up.
                        mVariablesDataGrid.Categories.Add(memberCategory);
                        if(SimultaneousCalls > 1)
                        {
                            SimultaneousCalls--;
                            // EARLY OUT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                            return;
                        }
                    }

                }

                SimultaneousCalls--;
            }
            else
            {
                foreach (var newCategory in categories)
                {
                    // let's see if any variables have changed
                    var oldCategory = mVariablesDataGrid.Categories.FirstOrDefault(item => item.Name == newCategory.Name);

                    if ( oldCategory != null && DoCategoriesDiffer(oldCategory.Members, newCategory.Members))
                    {
                        int index = mVariablesDataGrid.Categories.IndexOf(oldCategory);

                        mVariablesDataGrid.Categories.RemoveAt(index);
                        mVariablesDataGrid.Categories.Insert(index, newCategory);
                    }
                }
            }

            RefreshErrors(element);

            RefreshStateLabel(element, category, state);

            RefreshBehaviorUi(behaviorSave);

            mVariablesDataGrid.Refresh();
            
        }

        private void RefreshStateLabel(ElementSave element, StateSaveCategory category, StateSave state)
        {
            if(element == null)
            {
                variableViewModel.HasStateInformation = System.Windows.Visibility.Collapsed;
            }
            else if(state == element.DefaultState || state == null)
            {
                variableViewModel.HasStateInformation = System.Windows.Visibility.Collapsed;
            }
            else if(SelectedState.Self.CustomCurrentStateSave != null)
            {
                variableViewModel.HasStateInformation = System.Windows.Visibility.Visible;
                variableViewModel.StateInformation = $"Displaying custom (animated) state";
                variableViewModel.StateBackground = Brushes.Pink;
            }
            else
            {
                variableViewModel.StateBackground = Brushes.Yellow;
                variableViewModel.HasStateInformation = System.Windows.Visibility.Visible;
                string stateName = state.Name;
                if(category != null)
                {
                    stateName = category.Name + "/" + stateName;
                }
                variableViewModel.StateInformation = $"Editing state {stateName}";
            }
        }

        public void RefreshVariablesDataGridValues()
        {
            mVariablesDataGrid.Refresh();
        }

        private void RefreshBehaviorUi(BehaviorSave behaviorSave)
        {

            this.variableViewModel.BehaviorVariables.Clear();
            if(behaviorSave != null)
            {
                this.variableViewModel.BehaviorVariables.AddRange(behaviorSave.RequiredVariables.Variables);
            }


            this.variableViewModel.ShowBehaviorUi = behaviorSave != null ?
                System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            if(behaviorSave != null)
            {
                mainControl.BehaviorDataGrid.Instance = behaviorSave;
                mainControl.BehaviorDataGrid.Categories.Clear();
                mainControl.BehaviorDataGrid.Categories.AddRange(BehaviorShowingLogic.GetCategoriesFor(behaviorSave));
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
                this.variableViewModel.HasErrors = showError ?
                    System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;



                this.variableViewModel.ErrorInformation = message;
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


        private List<MemberCategory> GetMemberCategories(ElementSave element, StateSave state, StateSaveCategory stateCategory, InstanceSave instance)
        {
            List<MemberCategory> categories = new List<MemberCategory>();

            mLastElement = element;
            mLastState = state;
            mLastInstance = instance;
            mLastCategory = stateCategory;

            var stateSave = SelectedState.Self.SelectedStateSave;
            if (stateSave != null)
            {
                GetMemberCategoriesForState(element, instance, categories, stateSave, stateCategory);
            }
            else if(stateCategory != null)
            {
                GetMemberCategoriesForStateCategory(element, instance, categories, stateCategory);
            }
            return categories;

        }

        private void GetMemberCategoriesForStateCategory(ElementSave element, InstanceSave instance, List<MemberCategory> categories, StateSaveCategory stateCategory)
        {
            categories.Clear();

            List<string> commonMembers = new List<string>();

            var firstState = stateCategory.States.FirstOrDefault();

            if(firstState != null)
            {
                foreach(var variable in firstState.Variables)
                {
                    bool canAdd = variable.ExcludeFromInstances == false || instance == null;

                    if(canAdd)
                    {
                        commonMembers.Add(variable.Name);
                    }
                }

                foreach(var variableList in firstState.VariableLists)
                {
                    bool canAdd = true;

                    if(canAdd)
                    {
                        commonMembers.Add(variableList.Name);
                    }
                }
            }

            if(commonMembers.Any())
            {
                var memberCategory = new MemberCategory();
                memberCategory.Name = $"{stateCategory.Name} Variables";
                categories.Add(memberCategory);

                foreach(var commonMember in commonMembers)
                {
                    var instanceMember = new InstanceMember();

                    instanceMember.Name = commonMember;
                    instanceMember.CustomGetTypeEvent += (member) => typeof(string);
                    instanceMember.CustomGetEvent += (member) => commonMember;
                    instanceMember.CustomSetEvent += (not, used) =>
                    {
                        VariableInCategoryPropagationLogic.Self
                            .AskRemoveVariableFromAllStatesInCategory(commonMember, stateCategory);
                    };

                    instanceMember.PreferredDisplayer = typeof(VariableRemoveButton);

                    memberCategory.Members.Add(instanceMember);
                }
            }
        }

        private void GetMemberCategoriesForState(ElementSave element, InstanceSave instance, List<MemberCategory> categories, StateSave stateSave, StateSaveCategory stateSaveCategory)
        {
            categories.Clear();
            mPropertyGridDisplayer.GetCategories(element, instance, categories, stateSave, stateSaveCategory);

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
            CustomizeVariables(categories, stateSave, instance);
        }

        private void CustomizeVariables(List<MemberCategory> categories, StateSave stateSave, InstanceSave instance)
        {
            // Hack! I would like to have this set by variables, but that's going to require a ton
            // of refatoring. We need to move off of the intermediate PropertyDescriptor class.
            AdjustTextPreferredDisplayer(categories, stateSave, instance);

            UpdateColorCategory(categories);

            SetDisplayerForAlignment(categories);

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

        private void SetDisplayerForAlignment(List<MemberCategory> categories)
        {
            // This used to only make Text objects multiline, but...maybe we should make all string values multiline?
            foreach (var category in categories)
            {
                foreach (var member in category.Members)
                {
                    var propertyType = member.PropertyType;
                    if (propertyType == typeof(global::RenderingLibrary.Graphics.HorizontalAlignment) &&
                        member.Name == "HorizontalAlignment" || member.Name.EndsWith(".HorizontalAlignment"))
                    {
                        member.PreferredDisplayer = typeof(TextHorizontalAlignmentControl);
                    }
                    else if (propertyType == typeof(global::RenderingLibrary.Graphics.VerticalAlignment) &&
                        member.Name == "VerticalAlignment" || member.Name.EndsWith(".VerticalAlignment"))
                    {
                        member.PreferredDisplayer = typeof(TextVerticalAlignmentControl);
                    }
                    else if (propertyType == typeof(PositionUnitType) &&
                        member.Name == "X Units" || member.Name.EndsWith(".X Units"))
                    {
                        member.PreferredDisplayer = typeof(XUnitsControl);
                    }
                    else if (propertyType == typeof(PositionUnitType) &&
                        member.Name == "Y Units" || member.Name.EndsWith(".Y Units"))
                    {
                        member.PreferredDisplayer = typeof(YUnitsControl);
                    }
                    else if (propertyType == typeof(global::RenderingLibrary.Graphics.HorizontalAlignment) &&
                        member.Name == "X Origin" || member.Name.EndsWith(".X Origin"))
                    {
                        member.PreferredDisplayer = typeof(XOriginControl);
                    }
                    else if (propertyType == typeof(global::RenderingLibrary.Graphics.VerticalAlignment) &&
                        member.Name == "Y Origin" || member.Name.EndsWith(".Y Origin"))
                    {
                        member.PreferredDisplayer = typeof(YOriginControl);
                    }
                    else if (propertyType == typeof(DimensionUnitType) &&
                        member.Name == "Width Units" || member.Name.EndsWith(".Width Units"))
                    {
                        member.PreferredDisplayer = typeof(WidthUnitsControl);
                    }
                    else if (propertyType == typeof(DimensionUnitType) &&
                        member.Name == "Height Units" || member.Name.EndsWith(".Height Units"))
                    {
                        member.PreferredDisplayer = typeof(HeightUnitsControl);
                    }
                    else if (propertyType == typeof(ChildrenLayout) &&
                        member.Name == "Children Layout" || member.Name.EndsWith(".Children Layout"))
                    {
                        member.PreferredDisplayer = typeof(ChildrenLayoutControl);
                    }
                    else if(propertyType == typeof(List<string>))
                    {
                        member.PreferredDisplayer = typeof(WpfDataUi.Controls.StringListTextBoxDisplay);
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
                        member.PropertyType == typeof(string) &&
                        member.Name != "Name" && member.Name.EndsWith(".Name") == false;

                    if (isStringMember)
                    {
                        var rootVariable = (member as StateReferencingInstanceMember)?.GetRootVariableSave();

                        var shouldShowLocalizationUi = LocalizationManager.HasDatabase && (member.CustomOptions?.Count > 0) == false &&
                            ((member as StateReferencingInstanceMember)?.GetRootVariableSave())?.GetRootName() == "Text";

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
                            member.CustomOptions = LocalizationManager.Keys.OrderBy(item => item).ToArray();
                        }
                        else
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

        private void UpdateColorCategory(List<MemberCategory> categories)
        {
            var category = categories.FirstOrDefault(item => item.Name == "Rendering");

            if(category != null)
            {
                string redVariableName;
                string greenVariableName;
                string blueVariableName;
                GetRedGreenBlueVarNames(out redVariableName, out greenVariableName, out blueVariableName);

                var redVar = category.Members.FirstOrDefault(item => item.Name == redVariableName);
                var greenVar = category.Members.FirstOrDefault(item => item.Name == greenVariableName);
                var blueVar = category.Members.FirstOrDefault(item => item.Name == blueVariableName);

                if (redVar != null && greenVar != null && blueVar != null)
                {
                    InstanceMember instanceMember = new InstanceMember("Color", null);
                    instanceMember.PreferredDisplayer = typeof(Gum.Controls.DataUi.ColorDisplay);
                    instanceMember.CustomGetTypeEvent += (arg) => typeof(Microsoft.Xna.Framework.Color);
                    instanceMember.CustomGetEvent += GetCurrentColor;
                    instanceMember.CustomSetEvent += SetCurrentColor;

                    // so color updates
                    redVar.PropertyChanged += delegate
                    { 
                        instanceMember.SimulateValueChanged();
                    };
                    greenVar.PropertyChanged += delegate { instanceMember.SimulateValueChanged(); };
                    blueVar.PropertyChanged += delegate { instanceMember.SimulateValueChanged(); };

                    category.Members.Add(instanceMember);

                }
            }
        }

        object GetCurrentColor(object arg)
        {
            string redVariableName;
            string greenVariableName;
            string blueVariableName;
            GetRedGreenBlueVarNames(out redVariableName, out greenVariableName, out blueVariableName);

            var selectedState = SelectedState.Self.SelectedStateSave;

            int red = 0;
            int green = 0;
            int blue = 0;
            if(selectedState != null)
            {
                object redAsObject = selectedState.GetValueRecursive(redVariableName);
                if(redAsObject != null)
                {
                    red = (int)redAsObject;
                }

                object greenAsObject = selectedState.GetValueRecursive(greenVariableName);
                if (greenAsObject != null)
                {
                    green = (int)greenAsObject;
                }

                object blueAsObject = selectedState.GetValueRecursive(blueVariableName);
                if (blueAsObject != null)
                {
                    blue = (int)blueAsObject;
                }
            }

            return new Microsoft.Xna.Framework.Color(red, green, blue);
        }

        private static void GetRedGreenBlueVarNames(out string redVariableName, out string greenVariableName, out string blueVariableName)
        {
            string prefix = "";

            if (SelectedState.Self.SelectedInstance != null)
            {
                prefix = SelectedState.Self.SelectedInstance.Name + ".";
            }

            redVariableName = prefix + "Red";
            greenVariableName = prefix + "Green";
            blueVariableName = prefix + "Blue";
        }

        void SetCurrentColor(object arg1, object colorAsObject)
        {
            var oldColor = (Microsoft.Xna.Framework.Color)GetCurrentColor(null);


            string redVariableName;
            string greenVariableName;
            string blueVariableName;
            GetRedGreenBlueVarNames(out redVariableName, out greenVariableName, out blueVariableName);

            var state = SelectedState.Self.SelectedStateSave;

            var color = (Microsoft.Xna.Framework.Color)colorAsObject;

            state.SetValue(redVariableName, (int)color.R, "int");
            state.SetValue(greenVariableName, (int)color.G, "int");
            state.SetValue(blueVariableName, (int)color.B, "int");

            // Only need to refresh on one of the colors, so do it on any that have changed:
            // actually why not refresh all? It's fast now since it doesn't re-create the entire view, 
            // and plugins may depend on it:
            //var refreshRed = oldColor.R != color.R;
            //var refreshGreen = !refreshRed && oldColor.G != color.G;
            //var refreshBlue = !refreshRed && !refreshGreen && oldColor.B != color.B;

            var instance = SelectedState.Self.SelectedInstance;
            // These functions take unqualified:

            SetVariableLogic.Self.PropertyValueChanged("Red", (int)oldColor.R, instance, true);
            SetVariableLogic.Self.PropertyValueChanged("Green", (int)oldColor.G, instance, true);
            SetVariableLogic.Self.PropertyValueChanged("Blue", (int)oldColor.B, instance, true);

            RefreshUI();
        }

        //private void ReactIfChangedMemberIsAnimation(ElementSave parentElement, string changedMember, object oldValue, out bool saveProject)
        //{
        //    const string sourceFileString = "SourceFile";
        //    if (changedMember == sourceFileString)
        //    {
        //        StateSave stateSave = SelectedState.Self.SelectedStateSave;

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
}
