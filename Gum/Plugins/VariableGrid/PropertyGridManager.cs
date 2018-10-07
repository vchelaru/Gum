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

namespace Gum.Managers
{
    public partial class PropertyGridManager
    {
        #region Fields

        WpfDataUi.DataUiGrid mVariablesDataGrid;

        static PropertyGridManager mPropertyGridManager;

        ElementSaveDisplayer mPropertyGridDisplayer = new ElementSaveDisplayer();

        //ToolStripMenuItem mExposeVariable;
        //ToolStripMenuItem mResetToDefault;
        //ToolStripMenuItem mUnExposeVariable;

        ElementSave mLastElement;
        InstanceSave mLastInstance;
        StateSave mLastState;
        StateSaveCategory mLastCategory;

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


        public void Initialize(TestWpfControl variablesDataUiGrid)
        {
            mVariablesDataGrid = variablesDataUiGrid.DataGrid;
            variableViewModel = new Plugins.VariableGrid.MainControlViewModel();
            variablesDataUiGrid.DataContext = variableViewModel;
            variablesDataUiGrid.SelectedBehaviorVariableChanged += HandleBehaviorVariableSelected;
            variablesDataUiGrid.AddVariableClicked += HandleAddVariable;

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
        public async void RefreshUI(bool force = false)
        {
            if (isInRefresh)
                return;


            isInRefresh = true;

            bool showVariableGrid = SelectedState.Self.SelectedElement != null ||
                SelectedState.Self.SelectedInstance != null;
            variableViewModel.ShowVariableGrid = showVariableGrid ?
                System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;

            if (SelectedState.Self.SelectedInstances.GetCount() > 1)
            {
                // I don't know if we want to eventually show these
                // but for now we'll hide the PropertyGrid:
                mVariablesDataGrid.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                //mPropertyGrid.SelectedObject = mPropertyGridDisplayer;
                //mPropertyGrid.Refresh();

                var element = SelectedState.Self.SelectedElement;
                var state = SelectedState.Self.SelectedStateSave;
                var instance = SelectedState.Self.SelectedInstance;
                var behaviorSave = SelectedState.Self.SelectedBehavior;
                var category = SelectedState.Self.SelectedStateCategorySave;

                bool shouldMakeYellow = element != null && state != element.DefaultState;


                // This can take a little bit of time and we don't want the app to pop/freeze


                //Task task = new Task(() => RefreshDataGrid(element, state, instance));
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

            bool hasChangedObjectShowing = element != mLastElement || 
                instance != mLastInstance || 
                state != mLastState ||
                category != mLastCategory ||
                force;


            if (hasChangedObjectShowing)
            {
                List<MemberCategory> categories = GetMemberCategories(element, state, category, instance);
                Application.DoEvents();
                SimultaneousCalls ++;
                lock (lockObject)
                {
                    if(SimultaneousCalls > 1)
                    {
                        SimultaneousCalls--;
                        return;
                    }
                    records.Add("in");

                    mVariablesDataGrid.Instance = SelectedState.Self.SelectedStateSave;

                    mVariablesDataGrid.Visibility = System.Windows.Visibility.Hidden;

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
                Application.DoEvents();

                mVariablesDataGrid.Visibility = System.Windows.Visibility.Visible;

            }
            else
            {
                // let's see if any variables have been added/removed
                var categories = GetMemberCategories(element, state, category, instance);

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

            RefreshBehaviorUi(behaviorSave);

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
            var properties = mPropertyGridDisplayer.GetProperties(null);
            foreach (InstanceSavePropertyDescriptor propertyDescriptor in properties)
            {
                StateReferencingInstanceMember srim;

                if (instance != null)
                {
                    srim =
                        new StateReferencingInstanceMember(propertyDescriptor, stateSave, stateSaveCategory, instance.Name + "." + propertyDescriptor.Name, instance, element);
                }
                else
                {
                    srim =
                        new StateReferencingInstanceMember(propertyDescriptor, stateSave, stateSaveCategory, propertyDescriptor.Name, instance, element);
                }

                srim.SetToDefault += (memberName) => ResetVariableToDefault(srim);

                string category = propertyDescriptor.Category.Trim();

                var categoryToAddTo = categories.FirstOrDefault(item => item.Name == category);

                if (categoryToAddTo == null)
                {
                    categoryToAddTo = new MemberCategory(category);
                    categories.Add(categoryToAddTo);
                }

                categoryToAddTo.Members.Add(srim);

            }

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
            CustomizeVariables(categories);
        }

        private void CustomizeVariables(List<MemberCategory> categories)
        {
            // Hack! I would like to have this set by variables, but that's going to require a ton
            // of refatoring. We need to move off of the intermediate PropertyDescriptor class.
            MakeTextMultiline(categories);

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
                    if (member.PropertyType == typeof(global::RenderingLibrary.Graphics.HorizontalAlignment) &&
                        member.Name == "HorizontalAlignment" || member.Name.EndsWith(".HorizontalAlignment"))
                    {
                        member.PreferredDisplayer = typeof(TextHorizontalAlignmentControl);
                    }
                    else if (member.PropertyType == typeof(global::RenderingLibrary.Graphics.VerticalAlignment) &&
                        member.Name == "VerticalAlignment" || member.Name.EndsWith(".VerticalAlignment"))
                    {
                        member.PreferredDisplayer = typeof(TextVerticalAlignmentControl);
                    }
                    else if (member.PropertyType == typeof(PositionUnitType) &&
                        member.Name == "X Units" || member.Name.EndsWith(".X Units"))
                    {
                        member.PreferredDisplayer = typeof(XUnitsControl);
                    }
                    else if (member.PropertyType == typeof(PositionUnitType) &&
                        member.Name == "Y Units" || member.Name.EndsWith(".Y Units"))
                    {
                        member.PreferredDisplayer = typeof(YUnitsControl);
                    }
                    else if (member.PropertyType == typeof(global::RenderingLibrary.Graphics.HorizontalAlignment) &&
                        member.Name == "X Origin" || member.Name.EndsWith(".X Origin"))
                    {
                        member.PreferredDisplayer = typeof(XOriginControl);
                    }
                    else if (member.PropertyType == typeof(global::RenderingLibrary.Graphics.VerticalAlignment) &&
                        member.Name == "Y Origin" || member.Name.EndsWith(".Y Origin"))
                    {
                        member.PreferredDisplayer = typeof(YOriginControl);
                    }
                    else if (member.PropertyType == typeof(DimensionUnitType) &&
                        member.Name == "Width Units" || member.Name.EndsWith(".Width Units"))
                    {
                        member.PreferredDisplayer = typeof(WidthUnitsControl);
                    }
                    else if (member.PropertyType == typeof(DimensionUnitType) &&
                        member.Name == "Height Units" || member.Name.EndsWith(".Height Units"))
                    {
                        member.PreferredDisplayer = typeof(HeightUnitsControl);
                    }
                    else if (member.PropertyType == typeof(ChildrenLayout) &&
                        member.Name == "Children Layout" || member.Name.EndsWith(".Children Layout"))
                    {
                        member.PreferredDisplayer = typeof(ChildrenLayoutControl);
                    }

                }
            }
        }

        private void MakeTextMultiline(List<MemberCategory> categories)
        {
            // This used to only make Text objects multiline, but...maybe we should make all string values multiline?
            foreach(var category in categories)
            {
                foreach(var member in category.Members)
                {
                    if(member.PreferredDisplayer == null && 
                        member.PropertyType == typeof(string) &&
                        member.Name != "Name" && member.Name.EndsWith(".Name") == false)
                    {
                        //member.PreferredDisplayer = typeof(ToggleButtonOptionDisplay);
                        member.PreferredDisplayer = typeof(WpfDataUi.Controls.MultiLineTextBoxDisplay);
                        //ToggleButtonOptionDisplay
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
            object redAsObject = selectedState.GetValueRecursive(redVariableName);
            if(redAsObject != null)
            {
                red = (int)redAsObject;
            }

            int green = 0;
            object greenAsObject = selectedState.GetValueRecursive(greenVariableName);
            if (greenAsObject != null)
            {
                green = (int)greenAsObject;
            }

            int blue = 0;
            object blueAsObject = selectedState.GetValueRecursive(blueVariableName);
            if (blueAsObject != null)
            {
                blue = (int)blueAsObject;
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

            // These functions take unqualified:
            SetVariableLogic.Self.PropertyValueChanged("Red", (int)oldColor.R, false );
            SetVariableLogic.Self.PropertyValueChanged("Green", (int)oldColor.G, false );
            SetVariableLogic.Self.PropertyValueChanged("Blue", (int)oldColor.B, true);

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
        private void ResetVariableToDefault(StateReferencingInstanceMember srim)
        {
            string variableName = srim.Name;

            bool shouldReset = false;
            bool affectsTreeView = false;

            if (SelectedState.Self.SelectedInstance != null)
            {
                affectsTreeView = variableName == "Parent";
                //variableName = SelectedState.Self.SelectedInstance.Name + "." + variableName;

                shouldReset = true;
            }
            else if (SelectedState.Self.SelectedElement != null)
            {
                shouldReset =
                    // Don't let the user reset standard element variables, they have to have some actual value
                    (SelectedState.Self.SelectedElement is StandardElementSave) == false ||
                    // ... unless it's not the default
                    SelectedState.Self.SelectedStateSave != SelectedState.Self.SelectedElement.DefaultState;
            }

            if(shouldReset)
            {
                // If the variable is part of a category, then we don't allow setting the variable to default - they gotta do it through the cateory itself
                bool isPartOfCategory = srim.StateSaveCategory != null;

                if (isPartOfCategory)
                {
                    var window = new DeletingVariablesInCategoriesMessageBox();
                    window.ShowDialog();
                    
                    shouldReset = false;
                }
            }

            if (shouldReset)
            {
                StateSave state = SelectedState.Self.SelectedStateSave;
                bool wasChangeMade = false;
                VariableSave variable = state.GetVariableSave(variableName);
                if (variable != null)
                {
                    // Don't remove the variable if it's part of an element - we still want it there
                    // so it can be set, we just don't want it to set a value
                    // Update August 13, 2013
                    // Actually, we do want to remove it if it's part of an element but not the
                    // default state
                    // Update October 17, 2017
                    // Now that components do not
                    // necessarily need to have all
                    // of their variables, we can remove
                    // the variable now. In fact, we should
                    //bool shouldRemove = SelectedState.Self.SelectedInstance != null ||
                    //    SelectedState.Self.SelectedStateSave != SelectedState.Self.SelectedElement.DefaultState;

                    // Also, don't remove it if it's an exposed variable, this un-exposes things
                    bool shouldRemove = string.IsNullOrEmpty(variable.ExposedAsName);

                    if (shouldRemove)
                    {
                        state.Variables.Remove(variable);
                    }
                    else
                    {
                        variable.Value = null;
                        variable.SetsValue = false;
                    }

                    wasChangeMade = true;
                    // We need to refresh the property grid and the wireframe display

                }
                else
                {
                    // Maybe this is a variable list?
                    VariableListSave variableList = state.GetVariableListSave(variableName);
                    if (variableList != null)
                    {
                        state.VariableLists.Remove(variableList);

                        // We don't support this yet:
                        // variableList.SetsValue = false; // just to be safe
                        wasChangeMade = true;
                    }
                }

                if (wasChangeMade)
                {
                    RefreshUI();
                    WireframeObjectManager.Self.RefreshAll(true);
                    SelectionManager.Self.Refresh();

                    if (affectsTreeView)
                    {
                        GumCommands.Self.GuiCommands.RefreshElementTreeView(SelectedState.Self.SelectedElement);
                    }

                    if (ProjectManager.Self.GeneralSettingsFile.AutoSave)
                    {
                        ProjectManager.Self.SaveElement(SelectedState.Self.SelectedElement);
                    }
                }
            }
            else
            {
                srim.IsDefault = false;
            }
        }
    }
}
