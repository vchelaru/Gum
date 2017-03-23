using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Gum.DataTypes.ComponentModel
{
    public class MemberChangeArgs : EventArgs
    {
        public object Owner;
        public string Member;
        public object Value;
    }

    public delegate void MemberChangeEventHandler(object sender, MemberChangeArgs args);

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

        public void AddProperty(List<PropertyDescriptor> pdc, string propertyName, Type propertyType)
        {
            AddProperty(pdc, propertyName, propertyType, null, new Attribute[0]);
        }

        public PropertyDescriptorCollection AddProperty(PropertyDescriptorCollection pdc, string propertyName, Type propertyType)
        {
            return AddProperty(pdc, propertyName, propertyType, null, new Attribute[0]);
        }

        public void AddProperty(List<PropertyDescriptor> pdc, string propertyName, Type propertyType, TypeConverter converter,
            Attribute[] attributes)
        {
            InstanceSavePropertyDescriptor ppd = new InstanceSavePropertyDescriptor(
                propertyName, propertyType, attributes);

            ppd.TypeConverter = converter;

            pdc.Add(ppd);
        }

        public PropertyDescriptorCollection AddProperty(PropertyDescriptorCollection pdc, string propertyName, Type propertyType, TypeConverter converter,
            Attribute[] attributes)
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

            InstanceSavePropertyDescriptor ppd = new InstanceSavePropertyDescriptor(
                propertyName, propertyType, attributes);

            ppd.TypeConverter = converter;

            properties.Add(ppd);

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
    }
}
