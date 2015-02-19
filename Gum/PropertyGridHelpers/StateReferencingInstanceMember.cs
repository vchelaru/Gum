using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.DataTypes.ComponentModel;
using Gum.DataTypes.Variables;
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

        string RootVariableName
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

                return false;
            }
        }

        public override Type PreferredDisplayer
        {
            get
            {
                if(CustomOptions.Count != 0)
                {
                    return typeof(WpfDataUi.Controls.ComboBoxDisplay);
                }
                else if (IsFile)
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
            string variableName, InstanceSave instanceSave, ElementSave elementSave) :
            base(variableName, stateSave)
        {
            mInstanceSave = instanceSave;
            mStateSave = stateSave;
            mVariableName = variableName;
            mPropertyDescriptor = ispd;
            mElementSave = elementSave;
            this.CustomGetEvent += GetEvent;
            this.CustomSetEvent += SetEvent;
            this.CustomGetTypeEvent += GetTypeEvent;

            this.SortValue = int.MaxValue;

            if (instanceSave != null)
            {
                this.Instance = instanceSave;
            }
            else
            {
                this.Instance = elementSave;
            }

            DisplayName = RootVariableName;

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
                    ContextMenuEvents.Add("Un-expose Variable", HandleUnExposeVariableClick);
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
            

            StateSave currentStateSave = SelectedState.Self.SelectedStateSave;
            VariableSave variableSave = this.VariableSave;
            bool tempVariable = false;

            if (variableSave == null)
            {
                // This variable hasn't been assigned yet.  Let's make a new variable with a null value

                string variableName = instanceSave.Name + "." + this.RootVariableName;
                string rawVariableName = this.RootVariableName;

                ElementSave elementForInstance = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);

                var variableInDefault = elementForInstance.DefaultState.GetVariableSave(rawVariableName);

                if (variableInDefault != null)
                {
                    string variableType = variableInDefault.Type;

                    currentStateSave.SetValue(variableName, null, instanceSave, variableType);

                    // Now the variable should be created so we can access it
                    variableSave = VariableSave;
                    tempVariable = true;
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
                        variableSave.ExposedAsName = tiw.Result;
                        tempVariable = false;

                        GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                        GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);
                    }
                }
            }

            if (tempVariable)
                currentStateSave.Variables.Remove(variableSave);
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

        private object GetEvent(object instance)
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

        private void SetEvent(object instance, object value)
        {
            if (mPropertyDescriptor != null)
            {
                object oldValue = base.Value;

                mPropertyDescriptor.SetValue(instance, value);

                SetVariableLogic.Self.PropertyValueChanged(RootVariableName, oldValue);
            }
            else
            {
                mStateSave.SetValue(mVariableName, value);
            }
            // set the value
        }

        private Type GetTypeEvent(object instance)
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

            if (variableSave != null)
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
