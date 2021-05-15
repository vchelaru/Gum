using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.DataTypes.ComponentModel;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Reflection;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WpfDataUi.DataTypes;

namespace Gum.PropertyGridHelpers
{
    public class StateReferencingInstanceMember : InstanceMember
    {
        #region Fields

        StateSave mStateSave;
        string mVariableName;
        InstanceSave mInstanceSave;
        ElementSave mElementSave;

        InstanceSavePropertyDescriptor mPropertyDescriptor;
        #endregion

        #region Properties

        public StateSaveCategory StateSaveCategory { get; set; }

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
                return mPropertyDescriptor.GetValue(mInstanceSave) == null;
            }
            set
            {
                if (value && SetToDefault != null)
                {
                    SetToDefault(DisplayName);
                }
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

        #endregion

        public event Action<string> SetToDefault;

        #region Methods

        public StateReferencingInstanceMember(InstanceSavePropertyDescriptor ispd, StateSave stateSave, 
            StateSaveCategory stateSaveCategory,
            string variableName, InstanceSave instanceSave, ElementSave elementSave) :
            base(variableName, stateSave)
        {
            StateSaveCategory = stateSaveCategory;
            mInstanceSave = instanceSave;
            mStateSave = stateSave;
            mVariableName = variableName;
            mPropertyDescriptor = ispd;
            mElementSave = elementSave;

            if(ispd.IsReadOnly)
            {
                // don't assign it (can't null it)
                //this.CustomSetEvent = null;
            }
            else
            {
                this.CustomSetEvent += HandleCustomSet;
            }
            this.CustomGetEvent += HandleCustomGet;
            this.CustomGetTypeEvent += HandleCustomGetType;



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
                if(defaultStates.ContainsKey(standardElement?.Name))
                {
                    var defaultState = defaultStates[standardElement.Name];

                    var defaultStateVariable = defaultState.Variables.FirstOrDefault(item => item.Name == RootVariableName);

                    if(defaultStateVariable != null)
                    {
                        foreach(var kvp in defaultStateVariable.PropertiesToSetOnDisplayer)
                        {
                            this.PropertiesToSetOnDisplayer[kvp.Key] = kvp.Value;
                        }
                    }
                }
                this.SortValue = standardVariable.DesiredOrder;
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
                    ContextMenuEvents.Add($"Un-expose Variable {VariableSave.ExposedAsName}", HandleUnExposeVariableClick);
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

        private void HandleUnExposeVariableClick(object sender, System.Windows.RoutedEventArgs e)
        {
            // Find this variable in the source instance and make it not exposed
            VariableSave variableSave = this.VariableSave;

            if (variableSave != null)
            {
                variableSave.ExposedAsName = null;
                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
            }

            PropertyGridManager.Self.RefreshUI(force:true);
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

                        GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                        GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);
                    }
                }
            }
        }

        private void HandleUnexposeVariableClick(object sender, System.Windows.RoutedEventArgs e)
        {
            // Find this variable in the source instance and make it not exposed
            VariableSave variableSave = this.VariableSave;

            if (variableSave != null)
            {
                variableSave.ExposedAsName = null;
                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                PropertyGridManager.Self.RefreshUI();
            }
        }

        private object HandleCustomGet(object instance)
        {
            if (mPropertyDescriptor != null)
            {
                var toReturn = mPropertyDescriptor.GetValue(instance);

                if (toReturn == null)
                {
                    toReturn = mStateSave.GetValueRecursive(mVariableName);
                }

                return toReturn;
            }
            else
            {
                return mStateSave.GetValue(mVariableName);
            }
        }

        private void HandleCustomSet(object instance, object value)
        {
            if (mPropertyDescriptor != null)
            {
                object oldValue = base.Value;

                mPropertyDescriptor.SetValue(instance, value);

                string name = RootVariableName;

                bool handledByExposedVariable = false;

                // This might be a tunneled variable, and we want to react to the 
                // change using the underlying variable if so:
                if(instance is ElementSave)
                {
                    var elementSave = instance as ElementSave;
                    var variable = elementSave.DefaultState.Variables
                        .FirstOrDefault(item => item.ExposedAsName == RootVariableName);
                    if(variable != null)
                    {
                        var sourceObjectName = variable.SourceObject;

                        name = variable.Name;
                        var instanceInElement = elementSave.Instances
                            .FirstOrDefault(item => item.Name == sourceObjectName);

                        if(instanceInElement != null)
                        {
                            handledByExposedVariable = true;
                            SetVariableLogic.Self.ReactToPropertyValueChanged(variable.GetRootName(), oldValue, elementSave, instanceInElement, true);
                        }
                        
                    }
                }

                if(!handledByExposedVariable)
                {
                    SetVariableLogic.Self.PropertyValueChanged(name, oldValue, instance as InstanceSave);
                }
            }
            else
            {
                mStateSave.SetValue(mVariableName, value);
            }
            // set the value
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
            VariableSave variableSave = null;

            if (mInstanceSave != null)
            {
                variableSave = mInstanceSave.GetVariableFromThisOrBase(
                     new ElementWithState(mInstanceSave.ParentContainer), RootVariableName);
            }
            else
            {
                variableSave = mElementSave.GetVariableFromThisOrBase(
                    RootVariableName);
            }

            if (variableSave?.Type != null)
            {
                return TypeManager.Self.GetTypeFromString(variableSave.Type);
            }
            else
            {
                return typeof(string);
            }
        }
#endif

        #endregion
    }
}
