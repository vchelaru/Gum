using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.Managers;
using System.ComponentModel;
using Gum.DataTypes.Variables;

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


        public static void InitializeDefaultAndComponentVariables(this ComponentSave componentSave)
        {
            // June 27, 2012
            // We used to pass
            // null here because
            // passing a non-null
            // variable meant replacing
            // the existing StateSave with
            // the argument StateSave.  However,
            // now when the type of a Component is
            // changed, old values are not removed, but
            // are rather preserved so that changing the
            // type doesn't wipe out old values.
            //componentSave.Initialize(null);

            StateSave defaultStateSave = null;
            StandardElementSave ses = ObjectFinder.Self.GetRootStandardElementSave(componentSave);
            if (ses != null)
            {
                defaultStateSave = ses.DefaultState;
            }

            componentSave.Initialize(defaultStateSave);

            componentSave.Initialize(StandardElementsManager.Self.DefaultStates["Component"]);
        }

    }
}
