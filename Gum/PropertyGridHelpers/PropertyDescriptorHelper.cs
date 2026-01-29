using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace Gum.DataTypes.ComponentModel
{
    #region MemberChangeArgs

    public class MemberChangeArgs : EventArgs
    {
        public object Owner;
        public string Member;
        public object Value;
    }

    #endregion

    public delegate void MemberChangeEventHandler(object? sender, MemberChangeArgs args);

    // EVentually we want to move over to the ReflectingPropertyDescriptorHelper
    public class PropertyDescriptorHelper
    {
        PropertyDescriptor GetPropertyDescriptor(PropertyDescriptorCollection pdc, string name)
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

        public PropertyDescriptorCollection GetEmpty()
        {
            List<PropertyDescriptor> properties = new List<PropertyDescriptor>(0);

            return new PropertyDescriptorCollection(properties.ToArray());
        }

        public PropertyDescriptorCollection RemoveProperty(PropertyDescriptorCollection pdc, string propertyName)
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

        public InstanceSavePropertyDescriptor AddProperty(List<InstanceSavePropertyDescriptor> pdc, string propertyName, Type propertyType)
        {
            return AddProperty(pdc, propertyName, propertyType, null, new Attribute[0]);
        }

        public InstanceSavePropertyDescriptor AddProperty(List<InstanceSavePropertyDescriptor> pdc, string propertyName, Type propertyType, TypeConverter converter,
            Attribute[] attributes = null)
        {
            InstanceSavePropertyDescriptor newProperty = new InstanceSavePropertyDescriptor(
                propertyName, propertyType, attributes);

            newProperty.TypeConverter = converter;

            pdc.Add(newProperty);

            return newProperty;

        }

        public void SetPropertyDisplay(List<InstanceSavePropertyDescriptor> pdc, string oldName, string newName)
        {
            var property = pdc.FirstOrDefault(item => item.Name == oldName);
            if(property != null)
            {
                property.Name = newName;
            }
        }
    }
}
