using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Gum.DataTypes.ComponentModel;
using System.Reflection;

namespace Gum.PropertyGridHelpers
{
    public class PropertyGridMember
    {
        public string Name;
        public Type Type;
        public MemberChangeEventHandler MemberChange;
        public Func<object> CustomGetMember;
        public bool IsReadOnly;

        public List<Attribute> Attributes = new List<Attribute>();

        public bool IsExplicitlyIncluded
        {
            get;
            set;
        }

        public TypeConverter TypeConverter
        {
            get;
            set;
        }

        internal TypeConverter GetTypeConverter()
        {
            if (TypeConverter == null)
            {
                return TypeDescriptor.GetConverter(Type);
            }
            else
            {
                return TypeConverter;
            }
        }

        public void SetAttributes(object[] attributesInObjectArray)
        {
            Attributes.Clear();


            for (int i = 0; i < attributesInObjectArray.Length; i++)
            {
                Attributes.Add((Attribute)attributesInObjectArray[i]);
            }
        }

        public override string ToString()
        {
            return Type + " " + Name;
        }
    }




    class ReflectingPropertyDescriptorHelper
    {

        public object CurrentInstance
        {
            get;
            set;
        }

        public PropertyDescriptorCollection GetEmpty()
        {
            List<PropertyDescriptor> properties = new List<PropertyDescriptor>(0);

            return new PropertyDescriptorCollection(properties.ToArray());
        }


        static PropertyDescriptor GetPropertyDescriptor(PropertyDescriptorCollection pdc, string name)
        {
            for (int i = 0; i < pdc.Count; i++)
            {
                PropertyDescriptor pd = pdc[i];

                if (pd.Name == name)
                {
                    return pd;
                }
            }
            return null;
        }


        public static PropertyDescriptorCollection RemoveProperty(PropertyDescriptorCollection pdc, string propertyName)
        {
            List<PropertyDescriptor> properties = new List<PropertyDescriptor>();

            for (int i = 0; i < pdc.Count; i++)
            {
                PropertyDescriptor pd = pdc[i];

                if (pd.Name != propertyName)
                {
                    properties.Add(pd);
                }
            }
            return new PropertyDescriptorCollection(properties.ToArray());
        }

        public PropertyDescriptorCollection AddProperty(PropertyDescriptorCollection pdc, string propertyName, Type propertyType)
        {
            return AddProperty(pdc, propertyName, propertyType, null, new Attribute[0]);
        }

        public PropertyDescriptorCollection AddProperty(PropertyDescriptorCollection pdc, string propertyName, Type propertyType, TypeConverter converter,
            Attribute[] attributes)
        {
            return AddProperty(pdc, propertyName, propertyType, converter, attributes, null, null);
        }


        public PropertyDescriptorCollection AddProperty(PropertyDescriptorCollection pdc, string propertyName, Type propertyType, TypeConverter converter,
            Attribute[] attributes, MemberChangeEventHandler eventArgs, Func<object> getMember)
        {
            List<PropertyDescriptor> properties = new List<PropertyDescriptor>(pdc.Count);

            for (int i = 0; i < pdc.Count; i++)
            {
                PropertyDescriptor pd = pdc[i];

                if (pd.Name != propertyName)
                {
                    properties.Add(pd);
                }
            }

            // If it doesn't have
            // events for getting and
            // setting the value, then
            // the variable is part of the
            // type itself.  Objects can't have
            // fields/properties with spaces in them
            // so this is an invalid property.  The name
            // is either wrong or the user should have passed
            // in get and set methods.
            if (propertyName.Contains(' ') && (getMember == null || eventArgs == null))
            {
                throw new ArgumentException("The member cannot have spaces in the name if it doesn't have getters and setters explicitly set");
            }

            ReflectingParameterDescriptor epd = new ReflectingParameterDescriptor(propertyName, attributes);
            epd.SetComponentType(propertyType);
            epd.Instance = CurrentInstance;
            epd.MemberChangeEvent = eventArgs as MemberChangeEventHandler;
            epd.CustomGetMember = getMember;
            epd.TypeConverter = converter;



            properties.Add(epd);

            //PropertyDescriptor propertyDescriptor;

            return new PropertyDescriptorCollection(properties.ToArray());

        }

        public PropertyDescriptorCollection SetPropertyDisplay(PropertyDescriptorCollection pdc, string oldName, string newName)
        {
            PropertyDescriptor pd = GetPropertyDescriptor(pdc, oldName);

            pdc = RemoveProperty(pdc, oldName);


            Attribute[] attributeArray = new Attribute[pd.Attributes.Count];

            for (int i = 0; i < attributeArray.Length; i++)
            {
                attributeArray[i] = pd.Attributes[i];

            }

            pdc = AddProperty(pdc, newName, pd.PropertyType, null, attributeArray);

            return pdc;
        }


        public void Include(object container, string propertyName, ref PropertyDescriptorCollection pdc)
        {
            Type type = null;

            Type containerType = container.GetType();

            FieldInfo fieldInfo = containerType.GetField(propertyName);

            if (fieldInfo != null)
            {
                type = fieldInfo.FieldType;
            }

            if (type == null)
            {
                PropertyInfo propertyInfo = containerType.GetProperty(propertyName);

                if (propertyInfo != null)
                {
                    type = propertyInfo.PropertyType;
                }

            }

            if (type == null)
            {
                throw new Exception("Could not find type for te member " + propertyName);
            }

            pdc = AddProperty(pdc, propertyName, type);


        }


        internal void Include(DataTypes.GumProjectSave gumProjectSave, string p)
        {
            throw new NotImplementedException();
        }
    }
}
