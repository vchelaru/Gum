using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Gum.DataTypes.Variables;

namespace Gum.DataTypes
{
    public static class VariableListSaveExtensionMethods
    {
        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "typeof(List<string>) is a closed, compile-time-constant BCL type, " +
                "not data-dependent -- its default TypeConverter needs no additional preserved members.")]
        public static TypeConverter GetTypeConverter(this VariableListSave variableListSave)
        {
            return TypeDescriptor.GetConverter(typeof(List<string>));
            //ExpandableObjectConverter eoc = new ExpandableObjectConverter();
            //return eoc;
        }
    }
}
