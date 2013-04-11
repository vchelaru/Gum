using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Gum.DataTypes.Variables;

namespace Gum.DataTypes
{
    public static class VariableListSaveExtensionMethods
    {
        public static TypeConverter GetTypeConverter(this VariableListSave variableListSave)
        {
            return TypeDescriptor.GetConverter(typeof(List<string>));
            //ExpandableObjectConverter eoc = new ExpandableObjectConverter();
            //return eoc;
        }
    }
}
