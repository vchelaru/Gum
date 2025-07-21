using CommonFormsAndControls;
using ExCSS;
using Gum.DataTypes;
using Gum.DataTypes.ComponentModel;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Gum.Commands;
using ToolsUtilities;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gum.PropertyGridHelpers
{
    public class StateReferencingInstanceMember : InstanceMember
    {
        #region Fields

        ObjectFinder _objectFinder;
        private readonly HotkeyManager _hotkeyManager;
        private readonly UndoManager _undoManager;
        private readonly IDeleteVariableService _deleteVariableLogic;
        private readonly GuiCommands _guiCommands;
        StateSave mStateSave;
        string mVariableName;
        public InstanceSave InstanceSave { get; private set; }
        public ElementSave ElementSave { get; private set; }

        public object LastOldFullCommitValue { get; private set; }

        InstanceSavePropertyDescriptor mPropertyDescriptor;

        private readonly IEditVariableService _editVariablesService;
        private readonly IExposeVariableService _exposeVariableService;
        private readonly ISelectedState _selectedState;
        #endregion

        #region Properties

        public StateSaveCategory StateSaveCategory { get; set; }

        public StateSave StateSave => mStateSave;


        public override bool IsReadOnly
        {
            get
            {
                if (mPropertyDescriptor != null)
                {
                    return mPropertyDescriptor.IsReadOnly;
                }
                else
                {
                    return base.IsReadOnly;
                }
            }
        }

        public string RootVariableName
        {
            get
            {
                if (mPropertyDescriptor != null)
                {
                    return mPropertyDescriptor.Name;
                }
                else if (mVariableName.Contains('.'))
                {
                    return mVariableName.Substring(mVariableName.LastIndexOf('.') + 1);
                }
                else
                {
                    return mVariableName;
                }

            }
        }

        public override bool IsDefault
        {
            get
            {
                if (this.RootVariableName == "Name")
                {
                    return false; // this can never be default, and if it is that causes all kinds of weirdness in variable displays.
                }

                return GetValueStrictlyOnSelectedState(InstanceSave) == null;
            }
            set
            {
                if (value && SetToDefault != null)
                {
                    SetToDefault(DisplayName);
                }
            }
        }

        private object GetValueStrictlyOnSelectedState(object component)
        {
            if (mStateSave != null)
            {
                return mStateSave.GetValue(Name);
            }
            else
            {
                return null;
            }
        }

        public override IList<object> CustomOptions
        {
            get
            {
                if (mPropertyDescriptor != null && mPropertyDescriptor.Converter != null &&
                    (mPropertyDescriptor.Converter is System.ComponentModel.BooleanConverter == false))
                {
                    var values = mPropertyDescriptor.Converter.GetStandardValues(null);
                    if (values != null)
                    {
                        List<object> toReturn = new List<object>();
                        if (values != null)
                        {
                            foreach (var item in values)
                            {
                                toReturn.Add(item);
                            }
                        }
                        return toReturn;
                    }
                }

                return base.CustomOptions;
            }
        }

        bool IsFile
        {
            get
            {
                if (mPropertyDescriptor != null)
                {
                    var attributes = mPropertyDescriptor.Attributes;

                    if (attributes != null)
                    {
                        foreach (var attribute in attributes)
                        {
                            if (attribute is EditorAttribute)
                            {
                                EditorAttribute editorAttribute = attribute as EditorAttribute;

                                return editorAttribute.EditorTypeName.StartsWith("System.Windows.Forms.Design.FileNameEditor");
                            }
                        }
                        //EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))
                    }

                }

                return false;
            }
        }

        public override Type PreferredDisplayer
        {
            get
            {
                bool shouldBeComboBox = false;
                // we want to still give priority to the base displayer since
                // we may want to replace combo boxes with something like toggles:
                if (CustomOptions != null && base.PreferredDisplayer == null)
                {
                    shouldBeComboBox =
                       CustomOptions.Count != 0 ||
                       // If this is a state, still show the combo box even if there are no
                       // available states to select from. Otherwise it's a confusing text box
                       mPropertyDescriptor.Converter is Converters.AvailableStatesConverter;
                }
                if (shouldBeComboBox)
                {
                    return typeof(WpfDataUi.Controls.ComboBoxDisplay);
                }
                else if (IsFile && base.PreferredDisplayer == null)
                {
                    return typeof(WpfDataUi.Controls.FileSelectionDisplay);
                }
                else
                {
                    return base.PreferredDisplayer;
                }
            }
            set
            {
                base.PreferredDisplayer = value;
            }
        }

        VariableSave VariableSave
        {
            get
            {
                return mStateSave?.Variables.FirstOrDefault(item =>
                    item.Name == mVariableName || item.ExposedAsName == mVariableName);
            }
        }

        public int SortValue
        {
            get;
            set;
        }

        // Prior to April 10 2023 this was always true. Now that we have multi-select, we don't want to
        // call it here if editing multiple objects. Instead, we want to have the multi-select call it and pass
        // the list of variables so that a single undo can be performed.
        public bool IsCallingRefresh { get; set; } = true;

        #endregion

        public event Action<string> SetToDefault;


        public StateReferencingInstanceMember(InstanceSavePropertyDescriptor ispd,
            StateSave stateSave,
            StateSaveCategory stateSaveCategory,
            string variableName, InstanceSave instanceSave,
            IStateContainer stateListCategoryContainer,
            UndoManager undoManager) :
            base(variableName, stateSave)
        {
            _editVariablesService = Locator.GetRequiredService<IEditVariableService>();
            _exposeVariableService = Locator.GetRequiredService<IExposeVariableService>();
            _objectFinder = ObjectFinder.Self;
            _hotkeyManager = HotkeyManager.Self;
            _deleteVariableLogic = Locator.GetRequiredService<IDeleteVariableService>();
            _undoManager = undoManager;
            _selectedState = Locator.GetRequiredService<ISelectedState>();
            _guiCommands = Locator.GetRequiredService<GuiCommands>();

            StateSaveCategory = stateSaveCategory;
            InstanceSave = instanceSave;
            mStateSave = stateSave;
            mVariableName = variableName;
            mPropertyDescriptor = ispd;
            ElementSave = stateListCategoryContainer as ElementSave;

            if (ispd?.IsReadOnly == true)
            {
                // don't assign it (can't null it)
                //this.CustomSetEvent = null;
            }
            else
            {
                this.CustomSetPropertyEvent += HandleCustomSet;
                this.SetToDefault += HandleSetToDefault;
            }
            this.CustomGetEvent += HandleCustomGet;
            this.CustomGetTypeEvent += HandleCustomGetType;

            this.UiCreated += HandleUiCreated;

            this.SortValue = int.MaxValue;

            if (instanceSave != null)
            {
                this.Instance = instanceSave;
            }
            else
            {
                this.Instance = stateListCategoryContainer;
            }

            var alreadyHasSpaces = RootVariableName?.Contains(" ");
            if (alreadyHasSpaces == false)
            {
                DisplayName = ToolsUtilities.StringFunctions.InsertSpacesInCamelCaseString(RootVariableName);
            }
            else
            {
                DisplayName = RootVariableName;
            }

            ModifyContextMenu(instanceSave, stateListCategoryContainer, ispd);

            VariableSave standardVariable = null;
            if (stateListCategoryContainer is ElementSave elementSave)
            {
                standardVariable = _objectFinder.GetRootVariable(mVariableName, elementSave);
            }

            // todo - this needs to go to the standard elements manager
            if (standardVariable != null)
            {
                var standardElement = _objectFinder.GetContainerOf(standardVariable);

                if (standardElement != null && standardElement is StandardElementSave)
                {
                    try
                    {
                        var defaultState = StandardElementsManager.Self.GetDefaultStateFor(standardElement.Name);

                        var definingVariable = defaultState?.Variables.FirstOrDefault(item => item.Name == standardVariable.Name);

                        if (definingVariable != null)
                        {
                            if(definingVariable.PreferredDisplayer != null)
                            {
                                this.PreferredDisplayer = definingVariable.PreferredDisplayer;
                            }
                            this.DetailText = definingVariable.DetailText;

                            foreach (var kvp in definingVariable.PropertiesToSetOnDisplayer)
                            {
                                this.PropertiesToSetOnDisplayer[kvp.Key] = kvp.Value;
                            }

                            this.SortValue = definingVariable.DesiredOrder;
                        }
                    }
                    catch(Exception e)
                    {
                        // this could be a missing standard element save, print output but tolerate it:
                        _guiCommands.PrintOutput("Error getting standard element variable:\n" + e);
                    }
                }
            }

        }

        private void ModifyContextMenu(InstanceSave instanceSave, IStateContainer stateListCategoryContainer, InstanceSavePropertyDescriptor ispd)
        {
            SupportsMakeDefault = this.mVariableName != "Name" && !ispd.IsAssignedByReference;

            TryAddExposeVariableMenuOptions(instanceSave);

            TryAddCopyVariableReferenceMenuOptions();

            _editVariablesService.TryAddEditVariableOptions(this, VariableSave, stateListCategoryContainer);
        }

        private void HandleUiCreated(System.Windows.Controls.UserControl obj)
        {
            if (this.RootVariableName == "VariableReferences")
            {
                var asTextBox = obj as StringListTextBoxDisplay;

                if (asTextBox != null)
                {
                    asTextBox.KeyDown += (s, e) =>
                    {
                        if (_hotkeyManager.GoToDefinition.IsPressed(e))
                        {
                            HandleGotoDefinition(asTextBox);
                        }
                    };
                }
            }
        }

        private void HandleGotoDefinition(StringListTextBoxDisplay asTextBox)
        {
            var text = asTextBox.GetCurrentLineText();

            if (text?.Contains("=") == true)
            {
                var rightSideOfEquals = text.Substring(text.IndexOf("=") + 1).Trim();

                if (rightSideOfEquals.Contains("."))
                {
                    var beforeDot = rightSideOfEquals.Substring(0, rightSideOfEquals.IndexOf("."));

                    if (beforeDot.Contains("/"))
                    {
                        beforeDot = beforeDot.Substring(beforeDot.LastIndexOf("/") + 1);
                    }

                    var element = _objectFinder.GetElementSave(beforeDot);

                    if (element != null)
                    {
                        var afterDot = rightSideOfEquals.Substring(rightSideOfEquals.IndexOf(".") + 1);

                        var instanceName = afterDot;

                        if (afterDot.Contains("."))
                        {
                            instanceName = afterDot.Substring(0, afterDot.IndexOf("."));
                        }

                        InstanceSave instance = null;
                        if (!string.IsNullOrEmpty(instanceName))
                        {
                            instance = element.GetInstance(instanceName);
                        }
                        if (instance != null)
                        {
                            _selectedState.SelectedInstance = instance;
                        }
                        else
                        {
                            _selectedState.SelectedElement = element;
                        }
                    }
                }
            }
        }

        private void TryAddCopyVariableReferenceMenuOptions()
        {
            if (this.mVariableName != null && mVariableName != "Name" && mVariableName != "BaseType")
            {
                ContextMenuEvents.Add("Copy Qualified Variable Name", (sender, e) =>
                {
                    string qualifiedName;

                    if (ElementSave is ScreenSave)
                    {
                        qualifiedName = $"Screens/{ElementSave.Name}";
                    }
                    else if (ElementSave is ComponentSave)
                    {
                        qualifiedName = $"Components/{ElementSave.Name}";
                    }
                    else
                    {
                        qualifiedName = $"Standards/{ElementSave.Name}";
                    }

                    // no need to append instance, the variable name contains the instance name if there is an instance
                    //if(mInstanceSave != null)
                    //{
                    //    qualifiedName += "." + mInstanceSave.Name;
                    //}

                    qualifiedName += "." + mVariableName;

                    Clipboard.SetText(qualifiedName);

                });
            }


            if (VariableSave != null && _deleteVariableLogic.CanDeleteVariable(this.VariableSave))
            {
                ContextMenuEvents.Add($"Delete Variable [{VariableSave.Name}]", (sender, e) =>
                {
                    _deleteVariableLogic.DeleteVariable(this.VariableSave, this.ElementSave);
                });
            }
        }

        private void TryAddExposeVariableMenuOptions(InstanceSave instance)
        {
            bool canExpose = false;
            bool canUnExpose = false;

            if (this.VariableSave != null)
            {
                if (string.IsNullOrEmpty(VariableSave.ExposedAsName))
                {
                    if (instance != null)
                    {
                        canExpose = true;
                    }
                }
                else
                {
                    canUnExpose = true;
                }
            }
            else
            {
                var rootName = Gum.DataTypes.Variables.VariableSave.GetRootName(mVariableName);

                var isVariableList = false;
                if (instance?.ParentContainer != null)
                {
                    var rootVariableList = _objectFinder.GetRootVariableList(mVariableName, instance.ParentContainer);
                    isVariableList = rootVariableList != null;
                }

                canExpose = !isVariableList && rootName != "Name" && rootName != "BaseType"
                    && instance != null;

            }

            if (canExpose)
            {
                // Variable doesn't exist, so they can only expose it, not unexpose it.
                ContextMenuEvents.Add("Expose Variable", HandleExposeVariableClick);
            }
            if (canUnExpose)
            {
                ContextMenuEvents.Add($"Un-expose Variable {VariableSave.ExposedAsName} ({VariableSave.Name})", HandleUnexposeVariableClick);
            }
        }

        private void HandleUnexposeVariableClick(object sender, System.Windows.RoutedEventArgs e)
        {
            // Find this variable in the source instance and make it not exposed
            VariableSave variableSave = this.VariableSave;

            if (variableSave != null)
            {
                _exposeVariableService.HandleUnexposeVariableClick(VariableSave, ElementSave);
            }
        }

        private void HandleExposeVariableClick(object sender, System.Windows.RoutedEventArgs e)
        {
            _exposeVariableService.HandleExposeVariableClick(_selectedState.SelectedInstance,
                this.RootVariableName);
        }


        private object HandleCustomGet(object instance)
        {
            if (RootVariableName == "Name" && instance is InstanceSave asInstanceSave)
            {
                return asInstanceSave.Name;
            }
            else if (RootVariableName == "BaseType" && instance is InstanceSave asInstanceForBehavior)
            {
                return asInstanceForBehavior.BaseType;
            }
            else if (mPropertyDescriptor != null)
            {
                var toReturn = GetValueStrictlyOnSelectedState(instance);

                if (toReturn == null)
                {
                    var effectiveVariableName = VariableSave?.Name ?? mVariableName;

                    if (mStateSave != null)
                    {
                        toReturn = mStateSave.GetValueRecursive(effectiveVariableName);
                    }
                }

                // we want to do this by value:

                return toReturn;
            }
            else
            {
                // October 8, 2023 - why wasn't this recursive?
                //return mStateSave.GetValue(mVariableName);
                var effectiveVariableName = VariableSave?.Name ?? mVariableName;

                return mStateSave.GetValueRecursive(effectiveVariableName);
            }
        }

        private void HandleCustomSet(object gumElementOrInstanceSaveAsObject, SetPropertyArgs setPropertyArgs)
        {
            ////////////////////Early Out/////////////////////////
            if (!CanSetValue(gumElementOrInstanceSaveAsObject, setPropertyArgs))
            {
                setPropertyArgs.IsAssignmentCancelled = true;
                return;
            }
            /////////////////End Early Out////////////////////////

            var stateSave = _selectedState.SelectedStateSave;
            var instanceSave = gumElementOrInstanceSaveAsObject as InstanceSave;
            var elementSave = instanceSave?.ParentContainer ?? gumElementOrInstanceSaveAsObject as ElementSave;

            object newValue = setPropertyArgs.Value;
            if (mPropertyDescriptor != null)
            {
                StoreLastOldValue(setPropertyArgs);
                // <None> is a reserved 
                // value for when we want
                // to allow the user to reset
                // a value through a combo box.
                // If the value is "<None>" then 
                // let's set it to null
                if (newValue is "<None>")
                {
                    newValue = null;
                }


                // the stateSave.SetValue method handles Name and Base Type internally just fine,
                // but if we are on an instance in a behavior, that won't have a state save, so let's do that out here:
                // Check for reserved names
                // This is a variable on an instance
                if (instanceSave != null && RootVariableName == "Name")
                {
                    instanceSave.Name = (string)newValue;
                }
                else if (instanceSave != null && RootVariableName == "BaseType")
                {
                    instanceSave.BaseType = newValue.ToString();
                }
                else if (stateSave != null && elementSave != null)
                {
                    // If we are creating a new variable, we need to make sure it carries the same exposed
                    // name as the variable in base that defines it. We need to first get that variable...
                    VariableSave variableDefinedInThisOrBase = null;
                    var existingVariable = elementSave?.GetVariableFromThisOrBase(Name);
                    if (!string.IsNullOrEmpty(existingVariable?.ExposedAsName))
                    {
                        variableDefinedInThisOrBase = GetVariableDefinedInThisOrBase(existingVariable);
                    }
                    string variableType = existingVariable?.Type ?? elementSave?.GetVariableListFromThisOrBase(Name)?.Type;
                    // ...set variable after getting it from base, or else we'd get the variable we just set...
                    stateSave.SetValue(Name, newValue, instanceSave, variableType);
                    if (!string.IsNullOrEmpty(existingVariable?.ExposedAsName) && variableDefinedInThisOrBase != null)
                    {
                        //... then assign it here if we found it:
                        var variable = stateSave.GetVariableSave(Name);
                        variable.ExposedAsName = existingVariable.ExposedAsName;
                    }
                }

                var response = NotifyVariableLogic(gumElementOrInstanceSaveAsObject, setPropertyArgs.CommitType, trySave: setPropertyArgs.CommitType == SetPropertyCommitType.Full);

                if (response.Succeeded == false)
                {
                    setPropertyArgs.IsAssignmentCancelled = true;
                }
            }
            else
            {
                mStateSave.SetValue(mVariableName, newValue);
            }
        }

        private void StoreLastOldValue(SetPropertyArgs setPropertyArgs)
        {
            object oldValue = base.Value;

            if (setPropertyArgs.CommitType == SetPropertyCommitType.Full)
            {
                LastOldFullCommitValue = oldValue;

                // if the value changes was a list, we want to store off a copy of it or else the modified 
                // list will simply add to the existing list and later checks for equality will always return true:
                if (oldValue is IList oldList)
                {
                    var newList = Activator.CreateInstance(oldList.GetType()) as IList;

                    foreach (var item in oldList as IList)
                    {
                        newList.Add(item);
                    }
                    LastOldFullCommitValue = newList;
                }
            }
        }

        private VariableSave GetVariableDefinedInThisOrBase(VariableSave existingVariable)
        {
            VariableSave variableDefinedInThisOrBase = null;

            if (!string.IsNullOrEmpty(existingVariable?.ExposedAsName))
            {
                // need to set the exposed name on the variable, but only if it is defined in this component or in
                // a base component:

                variableDefinedInThisOrBase = _selectedState.SelectedStateSave.GetVariableSave(Name);
                if (variableDefinedInThisOrBase == null && _selectedState.SelectedStateSave != _selectedState.SelectedElement.DefaultState)
                {
                    variableDefinedInThisOrBase = _selectedState.SelectedElement.DefaultState.GetVariableSave(Name);
                }

                if (variableDefinedInThisOrBase == null)
                {
                    var allBase = _objectFinder.GetBaseElements(_selectedState.SelectedElement);
                    foreach (var baseElement in allBase)
                    {
                        variableDefinedInThisOrBase = baseElement.DefaultState.GetVariableSave(Name);
                        if (variableDefinedInThisOrBase != null)
                        {
                            break;
                        }
                    }
                }
            }

            return variableDefinedInThisOrBase;
        }

        private bool CanSetValue(object gumElementOrInstanceSaveAsObject, SetPropertyArgs setPropertyArgs)
        {
            if (this.RootVariableName == "Points")
            {
                var value = setPropertyArgs.Value as List<System.Numerics.Vector2>;

                if (value?.Count < 4)
                {
                    return false;
                }
            }

            return true;
        }

        private void HandleSetToDefault(string obj)
        {
            string variableName = Name;

            bool shouldReset = false;
            bool affectsTreeView = false;

            var selectedElement = _selectedState.SelectedElement;
            var selectedInstance = _selectedState.SelectedInstance;

            if (selectedInstance != null)
            {
                affectsTreeView = variableName == "Parent";
                //variableName = _selectedState.SelectedInstance.Name + "." + variableName;

                shouldReset = true;
            }
            else if (selectedElement != null)
            {
                shouldReset =
                    // Don't let the user reset standard element variables, they have to have some actual value
                    (selectedElement is StandardElementSave) == false ||
                    // ... unless it's not the default
                    _selectedState.SelectedStateSave != _selectedState.SelectedElement.DefaultState;
            }

            // now we reset, but we don't remove the variable:
            //if(shouldReset)
            //{
            //    // If the variable is part of a category, then we don't allow setting the variable to default - they gotta do it through the cateory itself

            //    if (isPartOfCategory)
            //    {
            //        var window = new DeletingVariablesInCategoriesMessageBox();
            //        window.ShowDialog();

            //        shouldReset = false;
            //    }
            //}

            StateSave state = _selectedState.SelectedStateSave;
            VariableSave variable = state?.GetVariableSave(variableName);
            var oldValue = variable?.Value;
            LastOldFullCommitValue = oldValue;

            bool wasChangeMade = false;
            if (shouldReset)
            {
                bool isPartOfCategory = StateSaveCategory != null;

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
                    //bool shouldRemove = _selectedState.SelectedInstance != null ||
                    //    _selectedState.SelectedStateSave != _selectedState.SelectedElement.DefaultState;
                    // Also, don't remove it if it's an exposed variable, this un-exposes things
                    bool shouldRemove = string.IsNullOrEmpty(variable.ExposedAsName) && !isPartOfCategory && !variable.IsCustomVariable;

                    // Update October 7, 2019
                    // Actually, we can remove any variable so long as the current state isn't the "base definition" for it
                    // For elements - no variables are the base variable definitions except for variables that are categorized
                    // state variables for categories defined in this element
                    if (shouldRemove)
                    {
                        var isState = variable.IsState(selectedElement, out ElementSave categoryContainer, out StateSaveCategory categoryForVariable);

                        if (isState)
                        {
                            var isDefinedHere = categoryForVariable != null && categoryContainer == selectedElement;

                            shouldRemove = !isDefinedHere;
                        }
                    }


                    if (shouldRemove)
                    {
                        state.Variables.Remove(variable);
                    }
                    else if (isPartOfCategory)
                    {
                        var variableInDefault = _selectedState.SelectedElement.DefaultState.GetVariableSave(variable.Name);
                        if (variableInDefault != null)
                        {
                            _guiCommands.PrintOutput(
                                $"The variable {variable.Name} is part of the category {StateSaveCategory.Name} so it cannot be removed. Instead, the value has been set to the value in the default state");

                            variable.Value = variableInDefault.Value;
                        }
                        else
                        {
                            // If it's a state, we can un-set that back to null, that's okay:
                            if (variable.IsState(selectedElement))
                            {
                                variable.Value = null;
                                variable.SetsValue = true;
                            }
                            else
                            {
                                _guiCommands.PrintOutput("Could not set value to default because the default state doesn't set this value");

                            }

                        }

                    }
                    else
                    {
                        variable.Value = null;
                        variable.SetsValue = false;
                    }

                    wasChangeMade = true;
                    // We need to refresh the property grid and the wireframe display

                }
                else if ((obj == "BaseType") || (obj == "Base Type") && ElementSave != null)
                {
                    ElementSave.BaseType = null;
                    wasChangeMade = true;
                }
                else
                {
                    // Maybe this is a variable list?
                    VariableListSave variableList = state?.GetVariableListSave(variableName);
                    if (variableList != null)
                    {
                        state.VariableLists.Remove(variableList);

                        // We don't support this yet:
                        // variableList.SetsValue = false; // just to be safe
                        wasChangeMade = true;
                    }
                }

                if (selectedElement != null)
                {
                    ElementSaveExtensions.ApplyVariableReferences(selectedElement, state);
                }


            }
            else
            {
                IsDefault = false;
            }

            if (wasChangeMade)
            {
                _undoManager.RecordUndo();
                _guiCommands.RefreshVariables(force: true);
                WireframeObjectManager.Self.RefreshAll(true);

                PluginManager.Self.VariableSet(selectedElement, selectedInstance, variableName, oldValue);

                if (affectsTreeView)
                {
                    _guiCommands.RefreshElementTreeView(_selectedState.SelectedElement);
                }

                GumCommands.Self.FileCommands.TryAutoSaveElement(_selectedState.SelectedElement);
            }

            var gumElementOrInstanceSaveAsObject = this.Instance;
            NotifyVariableLogic(gumElementOrInstanceSaveAsObject, SetPropertyCommitType.Full, trySave: true);
        }

        public GeneralResponse NotifyVariableLogic(object gumElementOrInstanceSaveAsObject, SetPropertyCommitType commitType, bool trySave = true)
        {
            GeneralResponse response = GeneralResponse.SuccessfulResponse;

            string name = RootVariableName;

            bool handledByExposedVariable = false;

            bool effectiveRefresh = commitType == SetPropertyCommitType.Full || IsCallingRefresh;

            bool effectiveRecordUndo = IsCallingRefresh && trySave;

            // This might be a tunneled variable, and we want to react to the 
            // change using the underlying variable if so:
            if (gumElementOrInstanceSaveAsObject is ElementSave)
            {
                var elementSave = gumElementOrInstanceSaveAsObject as ElementSave;
                var variable = elementSave.DefaultState.Variables
                    .FirstOrDefault(item => item.ExposedAsName == RootVariableName);
                if (variable != null)
                {
                    var sourceObjectName = variable.SourceObject;

                    name = variable.Name;
                    var instanceInElement = elementSave.Instances
                        .FirstOrDefault(item => item.Name == sourceObjectName);

                    if (instanceInElement != null)
                    {
                        handledByExposedVariable = true;

                        SetVariableLogic.Self.ReactToPropertyValueChanged(variable.GetRootName(), LastOldFullCommitValue, elementSave, instanceInElement, this.StateSave, refresh: effectiveRefresh, recordUndo: effectiveRecordUndo);
                    }

                }
            }

            if (!handledByExposedVariable)
            {
                var element = gumElementOrInstanceSaveAsObject as ElementSave ??
                    (gumElementOrInstanceSaveAsObject as InstanceSave).ParentContainer;
                response = SetVariableLogic.Self.PropertyValueChanged(
                    name,
                    LastOldFullCommitValue,
                    gumElementOrInstanceSaveAsObject as InstanceSave,
                    element?.DefaultState,
                    refresh: effectiveRefresh,
                    recordUndo: effectiveRecordUndo,
                    trySave: trySave);
            }

            return response;
        }

        private Type HandleCustomGetType(object instance)
        {
            if (mPropertyDescriptor != null)
            {
                Type toReturn;
                var typeFromPropertyDescriptor = mPropertyDescriptor.PropertyType;
                toReturn = typeFromPropertyDescriptor;

                if (typeFromPropertyDescriptor == typeof(string))
                {
                    var typeFromVariable = GetTypeFromVariableRecursively();

                    if (typeFromVariable != typeof(string))
                    {
                        toReturn = typeFromVariable;
                    }
                }

                return toReturn;
            }
            else
            {
                return GetTypeFromVariableRecursively();
            }
        }


#if GUM

        private Type GetTypeFromVariableRecursively()
        {
            VariableSave variableSave = GetRootVariableSave();

            if (variableSave?.Type != null)
            {
                return TypeManager.Self.GetTypeFromString(variableSave.Type);
            }
            else
            {
                var variableType = TryGetTypeFromVariableListSave();
                return variableType ??
                    typeof(string);
            }
        }

        // Vic asks - why does this exist? Why aren't we using ObjectFinder?
        public VariableSave GetRootVariableSave()
        {
            VariableSave variableSave = null;

            if (InstanceSave != null)
            {
                if (InstanceSave.ParentContainer == null)
                {
                    // this is an instance in a behavior
                    var elementBaseType = _objectFinder.GetElementSave(InstanceSave);

                    variableSave = elementBaseType?.GetVariableFromThisOrBase(RootVariableName);
                }
                else
                {

                    variableSave = InstanceSave.GetVariableFromThisOrBase(
                         new ElementWithState(InstanceSave.ParentContainer), RootVariableName);
                }
            }
            else
            {
                variableSave = ElementSave.GetVariableFromThisOrBase(
                    RootVariableName);
            }

            return variableSave;
        }

        public Type TryGetTypeFromVariableListSave()
        {
            string typeName = null;

            if (InstanceSave != null)
            {
                var variableList = InstanceSave?.GetVariableListFromThisOrBase(
                    InstanceSave.ParentContainer, RootVariableName);

                typeName = variableList?.Type;
            }
            else
            {
                typeName = ElementSave.GetVariableListFromThisOrBase(RootVariableName)?.Type;
            }

            if (!string.IsNullOrEmpty(typeName))
            {
                return TypeManager.Self.GetTypeFromString($"List<{typeName}>");
            }
            else
            {
                return null;
            }
        }
#endif

    }
}
