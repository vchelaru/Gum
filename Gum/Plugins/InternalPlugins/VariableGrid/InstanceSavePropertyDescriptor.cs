using System;
using System.ComponentModel;

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

        public string Subtext { get; set; }

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

        public bool IsAssignedByReference { get; set; }

        public string Category { get; set; } = "";

        /// <summary>
        /// How the property should be displayed in the UI. This may not match the actual type of the property, but rather how it should be presented.
        /// For example, a float may be represented as a string in the UI, but the ComponentType would still be float.
        /// </summary>
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

        public override string ToString() => $"{Name} ({ComponentType} displayed as {PropertyType})";
    }
}
