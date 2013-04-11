using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Gum.Reflection;

namespace Gum.DataTypes.ComponentModel
{
    class ReflectingParameterDescriptor : PropertyDescriptor
    {
        #region Fields

        Type mComponentType;


        TypeConverter mTypeConverter;

        #endregion

        #region Properties

        public object Instance
        {
            get;
            set;
        }

        #endregion

        public MemberChangeEventHandler MemberChangeEvent;
        public Func<object> CustomGetMember;

        public TypeConverter TypeConverter
        {
            get { return Converter; }
            set { mTypeConverter = value; }
        }

        public override TypeConverter Converter
        {
            get
            {
                if (mTypeConverter == null)
                {
                    if (mComponentType == typeof(bool))
                    {
                        return TypeDescriptor.GetConverter(typeof(bool));
                    }
                    else if(ComponentType == typeof(int))
                    {
                        return TypeDescriptor.GetConverter(typeof(int));
                    }
                    else if (ComponentType == typeof(float))
                    {
                        return TypeDescriptor.GetConverter(typeof(float));
                    }
                    else if (ComponentType == typeof(double))
                    {
                        return TypeDescriptor.GetConverter(typeof(double));
                    }
                    else if (ComponentType.IsEnum)
                    {
                        EnumConverter enumConverter = new EnumConverter(ComponentType);
                        return enumConverter;
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
        

        public override bool CanResetValue(object component)
        {
            return false;
        }


        public void SetComponentType(Type componentType)
        {
            mComponentType = componentType;
        }

        public override Type ComponentType
        {

            get
            {
                return mComponentType;
            }
        }

        public override object GetValue(object component)
        {
            try
            {
                if (CustomGetMember != null)
                {
                    return CustomGetMember();
                }

                else if (Instance != null)
                {
                    return LateBinder.GetInstance(Instance.GetType()).GetValue(Instance, this.Name);
                }
                else
                {
                    return null;
                }
            }
            catch(Exception e)
            {
                int m = 3;
                return null;
            }
        }

        public override void SetValue(object component, object value)
        {
            if (MemberChangeEvent != null)
            {
                MemberChangeEvent(null, new MemberChangeArgs { Owner=Instance, Member=this.Name, Value = value });
            }

            else if (Instance != null)
            {
                LateBinder.GetInstance(Instance.GetType()).SetValue(Instance, this.Name, value);
            }
        }
        public override bool IsReadOnly
        {
            get 
            {
                foreach (Attribute attribute in this.Attributes)
                {
                    if (attribute is ReadOnlyAttribute && ((ReadOnlyAttribute)attribute).IsReadOnly)
                    {
                        return true;
                    }
                }

                return false; 
            
            }
        }

        public override Type PropertyType
        {
            get { return mComponentType; }
        }

        public ReflectingParameterDescriptor(string name, Attribute[] attributes)
            : base(name, attributes)
        {

        }


        public override void ResetValue(object component)
        {

        }



        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
