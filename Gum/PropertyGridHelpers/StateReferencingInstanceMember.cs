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
                return mStateSave.Variables.FirstOrDefault(item => item.Name == mVariableName);
            }
        }

        #endregion

        public event Action<string> SetToDefault;

        #region Methods

        public StateReferencingInstanceMember(InstanceSavePropertyDescriptor ispd, StateSave stateSave, string variableName, InstanceSave instanceSave, ElementSave elementSave) :
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

            if (instanceSave != null)
            {
                this.Instance = instanceSave;
            }
            else
            {
                this.Instance = elementSave;
            }

            DisplayName = RootVariableName;

            if (this.VariableSave != null)
            {
                if (string.IsNullOrEmpty(VariableSave.ExposedAsName))
                {

                    ContextMenuEvents.Add("Expose Variable", HandleExposeVariableClick);
                }
                else
                {
                    ContextMenuEvents.Add("Un-expose Variable", HandleUnExposeVariableClick);
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

            PropertyGridManager.Self.RefreshUI();
        }

        private void HandleExposeVariableClick(object sender, System.Windows.RoutedEventArgs e)
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter variable name:";




            InstanceSave instanceSave = SelectedState.Self.SelectedInstance;
            StateSave currentStateSave = SelectedState.Self.SelectedStateSave;

            VariableSave variableSave = this.VariableSave;



            if (variableSave == null)
            {
                // This variable hasn't been assigned yet.  Let's make a new variable with a null value

                string variableName = instanceSave.Name + "." + this.RootVariableName;
                string rawVariableName = this.RootVariableName;

                ElementSave elementForInstance = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);
                string variableType = elementForInstance.DefaultState.GetVariableSave(rawVariableName).Type;

                currentStateSave.SetValue(variableName, null, instanceSave, variableType);

                // Now the variable should be created so we can access it
                variableSave = VariableSave;
            }

            //tiw.Result = instanceSave.Name + variableSave.Name;
            // We want to use the name without the dots.
            // So something like TextInstance.Text would be
            // TextInstanceText
            tiw.Result = variableSave.Name.Replace(".", "");
            DialogResult result = tiw.ShowDialog();

            if (result == DialogResult.OK)
            {
                variableSave.ExposedAsName = tiw.Result;

                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
            }
            else
            {
                currentStateSave.Variables.Remove(variableSave);
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

            }

            PropertyGridManager.Self.RefreshUI();
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


                Gum.Managers.PropertyGridManager.Self.PropertyValueChanged(RootVariableName, oldValue);

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
