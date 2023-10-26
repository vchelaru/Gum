using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.DataTypes.ComponentModel;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Reflection;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.PropertyGridHelpers
{
    public class StateReferencingInstanceMember : InstanceMember
    {
        #region Fields

        StateSave mStateSave;
        string mVariableName;
        public InstanceSave InstanceSave { get; private set; }
        public ElementSave ElementSave { get; private set; }

        public object LastOldValue { get; private set; }

        InstanceSavePropertyDescriptor mPropertyDescriptor;
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
                if(this.RootVariableName == "Name")
                {
                    return false; // this can never be default, and if it is that causes all kinds of weirdness in variable displays.
                }

                return GetValue(InstanceSave) == null;
            }
            set
            {
                if (value && SetToDefault != null)
                {
                    SetToDefault(DisplayName);
                }
            }
        }

        private object GetValue(object component)
        {
            StateSave stateSave = SelectedState.Self.SelectedStateSave;
            if (stateSave != null)
            {
                return stateSave.GetValue(Name);
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
                    if(values != null)
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
                        foreach(var attribute in attributes)
                        {
                            if(attribute is EditorAttribute)
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
                if(CustomOptions != null && base.PreferredDisplayer == null)
                {
                    shouldBeComboBox =
                       CustomOptions.Count != 0 || 
                    // If this is a state, still show the combo box even if there are no
                    // available states to select from. Otherwise it's a confusing text box
                       mPropertyDescriptor.Converter is Converters.AvailableStatesConverter;
                }
                if(shouldBeComboBox)
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
                return mStateSave.Variables.FirstOrDefault(item => 
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

        #region Methods

        public StateReferencingInstanceMember(InstanceSavePropertyDescriptor ispd, StateSave stateSave, 
            StateSaveCategory stateSaveCategory,
            string variableName, InstanceSave instanceSave, ElementSave elementSave) :
            base(variableName, stateSave)
        {
            StateSaveCategory = stateSaveCategory;
            InstanceSave = instanceSave;
            mStateSave = stateSave;
            mVariableName = variableName;
            mPropertyDescriptor = ispd;
            ElementSave = elementSave;

            if(ispd?.IsReadOnly == true)
            {
                // don't assign it (can't null it)
                //this.CustomSetEvent = null;
            }
            else
            {
                this.CustomSetPropertyEvent += HandleCustomSet;
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
                this.Instance = elementSave;
            }

            var alreadyHasSpaces = RootVariableName?.Contains(" ");
            if(alreadyHasSpaces == false)
            {
                DisplayName =  ToolsUtilities.StringFunctions.InsertSpacesInCamelCaseString(RootVariableName);
            }
            else
            {
                DisplayName = RootVariableName;
            }

            TryAddExposeVariableMenuOptions(instanceSave);

            TryAddCopyVariableReferenceMenuOptions();

            TryAddEditVariableOption();

            // This could be slow since we have to check it for every variable in an object.
            // Maybe we'll want to pass this in to the function?
            StandardElementSave standardElement = null;
            if (instanceSave != null)
            {
                standardElement = ObjectFinder.Self.GetRootStandardElementSave(instanceSave);
            }
            else
            {
                standardElement = ObjectFinder.Self.GetRootStandardElementSave(elementSave);
            }


            VariableSave standardVariable = null;

            if (standardElement != null)
            {
                standardVariable = standardElement.DefaultState.Variables.FirstOrDefault(item => item.Name == RootVariableName);
            }

            if (standardVariable != null)
            {
                var defaultStates = StandardElementsManager.Self.DefaultStates;

                StateSave defaultState = null;
                if (defaultStates.ContainsKey(standardElement?.Name))
                {
                    defaultState = defaultStates[standardElement.Name];
                }
                else
                {
                    // check plugin:
                    defaultState = PluginManager.Self.GetDefaultStateFor(standardElement.Name);
                }

                if(defaultState != null)
                {
                    var defaultStateVariable = defaultState.Variables.FirstOrDefault(item => item.Name == RootVariableName);

                    if (defaultStateVariable != null)
                    {
                        if (defaultStateVariable.PreferredDisplayer != null)
                        {
                            this.PreferredDisplayer = defaultStateVariable.PreferredDisplayer;
                        }
                        foreach (var kvp in defaultStateVariable.PropertiesToSetOnDisplayer)
                        {
                            this.PropertiesToSetOnDisplayer[kvp.Key] = kvp.Value;
                        }
                    }
                }

                this.SortValue = standardVariable.DesiredOrder;
            }

        }

        private void HandleUiCreated(System.Windows.Controls.UserControl obj)
        {
            if (this.RootVariableName == "VariableReferences")
            {
                var asTextBox = obj as StringListTextBoxDisplay;

                if(asTextBox != null)
                {
                    asTextBox.KeyDown += (s, e) =>
                    {
                        if (e.Key == System.Windows.Input.Key.F12)
                        {
                            var text = asTextBox.GetCurrentLineText();

                            if(text?.Contains("=") == true)
                            {
                                var rightSideOfEquals = text.Substring(text.IndexOf("=") + 1).Trim();

                                if(rightSideOfEquals.Contains("."))
                                {
                                    var beforeDot = rightSideOfEquals.Substring(0, rightSideOfEquals.IndexOf("."));

                                    if(beforeDot.Contains("/"))
                                    {
                                        beforeDot = beforeDot.Substring(beforeDot.LastIndexOf("/") + 1);
                                    }

                                    var element = ObjectFinder.Self.GetElementSave(beforeDot);

                                    if(element != null)
                                    {
                                        var afterDot = rightSideOfEquals.Substring(rightSideOfEquals.IndexOf(".") + 1);

                                        var instanceName = afterDot;

                                        if(afterDot.Contains("."))
                                        {
                                            instanceName = afterDot.Substring(0, afterDot.IndexOf("."));
                                        }

                                        InstanceSave instance = null;
                                        if (!string.IsNullOrEmpty(instanceName))
                                        {
                                            instance = element.GetInstance(instanceName);
                                        }
                                        if(instance != null)
                                        {
                                            SelectedState.Self.SelectedInstance = instance;
                                        }
                                        else
                                        {
                                            SelectedState.Self.SelectedElement = element;
                                        }
                                    }
                                }
                            }

                        }
                    };
                }
            }
        }

        private void TryAddEditVariableOption()
        {
            var variable = this.VariableSave;
            if (variable != null)
            {
                ContextMenuEvents.Add("Edit Variable", (sender, e) =>
                {
                    GumCommands.Self.GuiCommands.ShowEditVariableWindow(variable);
                });
            }
        }

        private void TryAddCopyVariableReferenceMenuOptions()
        {
            if(this.mVariableName != null)
            {
                ContextMenuEvents.Add("Copy Qualified Variable Name", (sender, e) =>
                {
                    string qualifiedName;

                    if (ElementSave is ScreenSave)
                    {
                        qualifiedName = $"Screens/{ElementSave.Name}";
                    }
                    else if(ElementSave is ComponentSave)
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

            if(this.VariableSave?.IsCustomVariable == true)
            {
                ContextMenuEvents.Add("Delete Variable", (sender, e) =>
                {
                    if(ElementSave?.DefaultState.Variables.Contains(this.VariableSave) == true)
                    {
                        ElementSave.DefaultState.Variables.Remove(this.VariableSave);

                        GumCommands.Self.FileCommands.TryAutoSaveElement(ElementSave);
                        GumCommands.Self.GuiCommands.RefreshPropertyGrid(force:true);
                    }
                });
            }
        }

        private void TryAddExposeVariableMenuOptions(InstanceSave instance)
        {
            if (this.VariableSave != null)
            {
                if (string.IsNullOrEmpty(VariableSave.ExposedAsName))
                {
                    if (instance != null)
                    {
                        ContextMenuEvents.Add("Expose Variable", HandleExposeVariableClick);
                    }
                }
                else
                {
                    ContextMenuEvents.Add($"Un-expose Variable {VariableSave.ExposedAsName} ({VariableSave.Name})", HandleUnexposeVariableClick);
                }
            }
            else
            {
                var rootName = Gum.DataTypes.Variables.VariableSave.GetRootName(mVariableName);

                bool canExpose = rootName != "Name" && rootName != "Base Type"
                    && instance != null;

                if (canExpose)
                {
                    // Variable doesn't exist, so they can only expose it, not unexpose it.
                    ContextMenuEvents.Add("Expose Variable", HandleExposeVariableClick);
                }
            }
        }

        private void HandleUnexposeVariableClick(object sender, System.Windows.RoutedEventArgs e)
        {
            // Find this variable in the source instance and make it not exposed
            VariableSave variableSave = this.VariableSave;
            
            if (variableSave != null)
            {
                var oldExposedName = variableSave.ExposedAsName;
                variableSave.ExposedAsName = null;

                PluginManager.Self.VariableDelete(ElementSave, oldExposedName);
                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                PropertyGridManager.Self.RefreshUI(force: true);
            }
        }

        private void HandleExposeVariableClick(object sender, System.Windows.RoutedEventArgs e)
        {
            InstanceSave instanceSave = SelectedState.Self.SelectedInstance;
            if (instanceSave == null)
            {
                MessageBox.Show("Cannot expose variables on components or screens, only on instances");
                return;
            }
            
            // Update June 1, 2017
            // This code used to expose
            // a variable on whatever state
            // was selected; however, exposed
            // variables should be exposed on the
            // default state or else Gum doesn't properly
            //StateSave currentStateSave = SelectedState.Self.SelectedStateSave;
            StateSave stateToExposeOn = SelectedState.Self.SelectedElement.DefaultState;
            VariableSave variableSave = this.VariableSave;

            if (variableSave == null)
            {
                // This variable hasn't been assigned yet.  Let's make a new variable with a null value

                string variableName = instanceSave.Name + "." + this.RootVariableName;
                string rawVariableName = this.RootVariableName;

                ElementSave elementForInstance = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);
                var variableInDefault = elementForInstance.DefaultState.GetVariableSave(rawVariableName);
                while(variableInDefault == null && !string.IsNullOrEmpty(elementForInstance.BaseType))
                {
                    elementForInstance = ObjectFinder.Self.GetElementSave(elementForInstance.BaseType);
                    if(elementForInstance?.DefaultState == null)
                    {
                        break;
                    }
                    variableInDefault = elementForInstance.DefaultState.GetVariableSave(rawVariableName);
                }

                if (variableInDefault != null)
                {
                    string variableType = variableInDefault.Type;

                    stateToExposeOn.SetValue(variableName, null, instanceSave, variableType);

                    // Now the variable should be created so we can access it
                    variableSave = stateToExposeOn.GetVariableSave(variableName);
                    // Since it's newly-created, there is no value being set:
                    variableSave.SetsValue = false;
                }
            }

            if (variableSave == null)
            {
                MessageBox.Show("This variable cannot be exposed.");
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter variable name:";
                // We want to use the name without the dots.
                // So something like TextInstance.Text would be
                // TextInstanceText
                tiw.Result = variableSave.Name.Replace(".", "");

                DialogResult result = tiw.ShowDialog();

                if (result == DialogResult.OK)
                {
                    string whyNot;
                    if (!NameVerifier.Self.IsExposedVariableNameValid(tiw.Result, SelectedState.Self.SelectedElement, out whyNot))
                    {
                        MessageBox.Show(whyNot);
                    }
                    else
                    {
                        var elementSave = SelectedState.Self.SelectedElement;
                        // if there is an inactive variable,
                        // we should get rid of it:
                        var existingVariable = SelectedState.Self.SelectedElement.GetVariableFromThisOrBase(tiw.Result);

                        // there's a variable but we shouldn't consider it
                        // unless it's "Active" - inactive variables may be
                        // leftovers from a type change


                        if (existingVariable != null)
                        {
                            var isActive = VariableSaveLogic.GetIfVariableIsActive(existingVariable, elementSave, null);
                            if (isActive == false)
                            {
                                // gotta remove the variable:
                                if(elementSave.DefaultState.Variables.Contains(existingVariable))
                                {
                                    // We may need to worry about inheritance...eventually
                                    elementSave.DefaultState.Variables.Remove(existingVariable);
                                }
                            }

                        }

                        variableSave.ExposedAsName = tiw.Result;

                        PluginManager.Self.VariableAdd(elementSave, tiw.Result);

                        GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                        GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);
                    }
                }
            }
        }


        private object HandleCustomGet(object instance)
        {
            if(RootVariableName == "Name" && instance is InstanceSave asInstanceSave)
            {
                return asInstanceSave.Name;
            }

            else if (mPropertyDescriptor != null)
            {
                var toReturn = GetValue(instance);

                if (toReturn == null)
                {
                    var effectiveVariableName = VariableSave?.Name ?? mVariableName;
                    toReturn = mStateSave.GetValueRecursive(effectiveVariableName);
                }

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
            object newValue = setPropertyArgs.Value;
            if (mPropertyDescriptor != null)
            {
                object oldValue = base.Value;
                LastOldValue = oldValue;


                //mPropertyDescriptor.SetValue(gumElementOrInstanceSaveAsObject, newValue);
                ElementSave elementSave = null;
                StateSave stateSave = SelectedState.Self.SelectedStateSave;
                //InstanceSave instanceSave = SelectedState.Self.SelectedInstance;

                var instanceSave = gumElementOrInstanceSaveAsObject as InstanceSave;

                if (instanceSave != null)
                {
                    elementSave = instanceSave.ParentContainer;
                }
                else // instance is null, so assign the element
                {
                    elementSave = gumElementOrInstanceSaveAsObject as ElementSave;
                }

                if (stateSave != null && elementSave != null)
                {

                    // <None> is a reserved 
                    // value for when we want
                    // to allow the user to reset
                    // a value through a combo box.
                    // If the value is "<None>" then 
                    // let's set it to null
                    if (newValue is string && ((string)newValue) == "<None>")
                    {
                        newValue = null;
                    }

                    string variableType = null;
                    var existingVariable = elementSave.GetVariableFromThisOrBase(Name);
                    if (existingVariable != null)
                    {
                        variableType = existingVariable.Type;
                    }
                    else
                    {
                        var listVariableType = elementSave.GetVariableListFromThisOrBase(Name)?.Type;
                        if (!string.IsNullOrEmpty(listVariableType))
                        {
                            variableType = $"List<{listVariableType}>";
                        }
                    }

                    stateSave.SetValue(Name, newValue, instanceSave, variableType);
                }

                NotifyVariableLogic(gumElementOrInstanceSaveAsObject, trySave:setPropertyArgs.CommitType == SetPropertyCommitType.Full);
            }
            else
            {
                mStateSave.SetValue(mVariableName, newValue);
            }
            // set the value
        }

        public void NotifyVariableLogic(object gumElementOrInstanceSaveAsObject, bool? forceRefresh = null, bool trySave = true)
        {
            string name = RootVariableName;

            bool handledByExposedVariable = false;

            bool effectiveRefresh = forceRefresh ?? IsCallingRefresh;

            bool effectiveRecordUndo = IsCallingRefresh;

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

                        SetVariableLogic.Self.ReactToPropertyValueChanged(variable.GetRootName(), LastOldValue, elementSave, instanceInElement, this.StateSave, refresh: effectiveRefresh, recordUndo: effectiveRecordUndo);
                    }

                }
            }

            if (!handledByExposedVariable)
            {
                SetVariableLogic.Self.PropertyValueChanged(name, LastOldValue, gumElementOrInstanceSaveAsObject as InstanceSave, refresh: effectiveRefresh, recordUndo: effectiveRecordUndo,
                    trySave:trySave);
            }
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

        public VariableSave GetRootVariableSave()
        {
            VariableSave variableSave = null;

            if (InstanceSave != null)
            {
                variableSave = InstanceSave.GetVariableFromThisOrBase(
                     new ElementWithState(InstanceSave.ParentContainer), RootVariableName);
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
            
            if(InstanceSave != null)
            {
                typeName = InstanceSave?.GetVariableListFromThisOrBase(
                    InstanceSave.ParentContainer, RootVariableName)?.Type;
            }
            else
            {
                typeName = ElementSave.GetVariableListFromThisOrBase(RootVariableName)?.Type;
            }

            if(!string.IsNullOrEmpty(typeName))
            {
                return TypeManager.Self.GetTypeFromString($"List<{typeName}>");
            }
            else
            {
                return null;
            }
        }
#endif

        #endregion
    }
}
