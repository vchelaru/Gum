using Gum.Managers;
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

            // We used to call initialize with the default state for the given component base type (which is usually a container)
            // But this copies all the variables from the container to this, which seems redundant...why do we do this if it inherits
            // from a container which has its own state? Most of the time the user won't change the defaults and it just adds bloat.
            //StandardElementSave ses = ObjectFinder.Self.GetRootStandardElementSave(componentSave);
            //if (ses != null)
            //{
            //    defaultStateSave = ses.DefaultState;
            //}

            componentSave.Initialize(new StateSave { Name = "Default" });

            componentSave.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Component"));
        }

    }
}
