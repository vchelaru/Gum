using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.Managers;
using System.ComponentModel;

namespace Gum.DataTypes
{
    public static class ComponentSaveExtensionMethods
    {
        public static bool CanContainInstanceOfType(this ComponentSave componentSave, string typeToCheck)
        {
            return !componentSave.IsOfType(typeToCheck);

        }

        public static bool IsOfType(this ComponentSave componentSave, string typeToCheck)
        {
            if (componentSave.Name == typeToCheck || componentSave.BaseType == typeToCheck)
            {
                return true;
            }
            else if (!string.IsNullOrEmpty(componentSave.BaseType))
            {
                ComponentSave baseComponentSave = ObjectFinder.Self.GetComponent(componentSave.BaseType);
                if (baseComponentSave == null)
                {
                    return false;
                }
                else
                {
                    return baseComponentSave.IsOfType(typeToCheck);
                }
            }
            else
            {
                return false;
            }

        }
    }
}
