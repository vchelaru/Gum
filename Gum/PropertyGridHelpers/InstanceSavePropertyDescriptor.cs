using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using GumRuntime;

namespace Gum.DataTypes.ComponentModel
{
    #region Delegate Definitions

    public delegate object GetValueDelegate(object component);
    public delegate void SetValueDelegate(object component, object value);

    #endregion

    public class InstanceSavePropertyDescriptor
        //: PropertyDescriptor
    {
        public Attribute[] Attributes { get; }
        #region Fields

        Type mParameterType;

		TypeConverter mTypeConverter;


        #endregion

        #region Properties

        public string Name { get; set; }

        public TypeConverter TypeConverter
        {
            get { return Converter; }
            set { mTypeConverter = value; }
        }

        public TypeConverter Converter
        {
            get
            {
                if (mTypeConverter == null)
                {
                    if (mParameterType == typeof(bool))
                    {
                        return TypeDescriptor.GetConverter(typeof(bool));
                    }
                    else
                    {
                        return TypeDescriptor.GetConverter(typeof(string));
                    }
                }
                else
                {
                    return mTypeConverter;
                }
            }
        }


        public object Owner
		{
			get;
			set;
		}

        #endregion

        // Eventually we're going to move off of using the base 
        // PropertyDescriptor
        public bool IsReadOnly
        {
            get;
            set;
        }

        public string Category { get; set; } = "";

        public Type PropertyType
        {
            get 
			{
				if (mParameterType == typeof(bool))
				{
					return typeof(bool);
				}
				else
				{
					return typeof(string);
				}
			}
        }

        public InstanceSavePropertyDescriptor(string name, Type type, Attribute[] attrs)
        {
            this.Attributes = attrs;
			mParameterType = type;
            Name = name;
        }

        public bool CanResetValue(object component)
        {
            return false;
        }

        public Type ComponentType
        {
            get 
            {

                return mParameterType;
                // add more things here
				//return typeof(string);
            
            }
        }

        public object GetValue(object component)
        {
            StateSave stateSave = SelectedState.Self.SelectedStateSave;
            if (stateSave != null)
            {
                string name = GetVariableNameConsideringSelection(component as InstanceSave);
                return stateSave.GetValue(name);
            }
            else
            {
                return null;
            }
        }

        private string GetVariableNameConsideringSelection(InstanceSave instance)
        {
            string name = this.Name;
            if (instance != null)
            {
                name = instance.Name + "." + name;
            }
            return name;
        }

        public void SetValue(object selectedItem, object value)
        {
            ElementSave elementSave = null;
            StateSave stateSave = SelectedState.Self.SelectedStateSave;
            //InstanceSave instanceSave = SelectedState.Self.SelectedInstance;

            var instanceSave = selectedItem as InstanceSave;

            if(instanceSave != null)
            {
                elementSave = instanceSave.ParentContainer;
            }
            else // instance is null, so assign the element
            {
                elementSave = selectedItem as ElementSave;
            }

            //////////////// Early Out/////////////
            if (stateSave == null || elementSave == null)
            {
                return;
            }


            ///////////////End Early Out///////////

            string name = GetVariableNameConsideringSelection(instanceSave);

            // <None> is a reserved 
            // value for when we want
            // to allow the user to reset
            // a value through a combo box.
            // If the value is "<None>" then 
            // let's set it to null
            if (value is string && ((string)value) == "<None>")
            {
                value = null;
            }

            string variableType = null;
            var existingVariable = elementSave.GetVariableFromThisOrBase(name);
            if(existingVariable != null)
            {
                variableType = existingVariable.Type;
            }
            else
            {
                var listVariableType = elementSave.GetVariableListFromThisOrBase(name)?.Type;
                if(!string.IsNullOrEmpty(listVariableType))
                {
                    variableType = $"List<{listVariableType}>";
                }
            }
            
            stateSave.SetValue(name, value, instanceSave, variableType);

            // Oct 13, 2022
            // This should set 
            // values on all contained objects for this particular state
            // Maybe this could be slow? not sure, but this covers all cases so if
            // there are performance issues, will investigate later.
            ElementSaveExtensions.ApplyVariableReferences(elementSave, stateSave);
        }

        public void ResetValue(object component)
        {
            
            // do nothing here
        }

        //private void SetEventValue(string categoryString, IEventContainer eventContainer, string value)
        //{
        //    switch (categoryString)
        //    {
        //        case "Active Events":

        //            EventResponseSave eventToModify = null;

        //            for (int i = eventContainer.Events.Count - 1; i > -1; i--)
        //            {
        //                if (eventContainer.Events[i].EventName == this.Name)
        //                {
        //                    eventToModify = eventContainer.Events[i];
        //                    break;
        //                }
        //            }

        //            if (eventToModify == null)
        //            {
        //                throw new Exception("Could not find an event by the name of " + Name);
        //            }
        //            else
        //            {
        //                string valueAsString = value;

        //                if (string.IsNullOrEmpty(valueAsString) || valueAsString == "<NONE>")
        //                {
        //                    eventContainer.Events.Remove(eventToModify);
        //                }
        //                else
        //                {
        //                    //eventToModify.InstanceMethod = valueAsString;
        //                }
        //            }
        //            //EventSave eventSave = EditorLogic.Current
        //            break;
        //        case "Unused Events":

        //            //EventSave eventSave = EventManager.AllEvents[Name];

        //            //eventSave.InstanceMethod = value;

        //            //EditorLogic.CurrentEventContainer.Events.Add(eventSave);
        //            break;
        //    }
        //}

        public bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public override string ToString()
        {
            return mParameterType + " " + Name;
        }
    
    }
}
