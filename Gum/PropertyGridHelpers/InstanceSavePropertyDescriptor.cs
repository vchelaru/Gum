using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using GumRuntime;
using Gum.Managers;

namespace Gum.DataTypes.ComponentModel
{
    #region Delegate Definitions

    public delegate object GetValueDelegate(object component);
    public delegate void SetValueDelegate(object component, object value);

    #endregion

    public class InstanceSavePropertyDescriptor
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

        public Type ComponentType
        {
            get 
            {

                return mParameterType;
                // add more things here
				//return typeof(string);
            
            }
        }

    }
}
