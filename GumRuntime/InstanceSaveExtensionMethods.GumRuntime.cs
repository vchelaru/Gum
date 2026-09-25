using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using System;

namespace GumRuntime
{
    public static class InstanceSaveExtensionMethods
    {

        /// <returns>The created element, or null if the instance BaseType is not in the project.</returns>
        public static GraphicalUiElement? ToGraphicalUiElement(this InstanceSave instanceSave, ISystemManagers systemManagers)
        {
#if FULL_DIAGNOSTICS
            if (ObjectFinder.Self.GumProjectSave == null)
            {
                throw new InvalidOperationException("You need to set the ObjectFinder's GumProjectSave first so it can track references");
            }
#endif
            ElementSave? instanceElement = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);

            GraphicalUiElement? toReturn = null;
            if (instanceElement != null)
            {
                string? genericType = null;


                if(instanceElement.Name == "Container" && instanceElement is StandardElementSave)
                {
                    // Instances being created belong to a loaded element, which has a default state.
                    var containerDefaultState = instanceSave.ParentContainer!.DefaultState!;
                    genericType = containerDefaultState.GetValueOrDefault<string>(instanceSave.Name + "." + "ContainedType") ??
                        containerDefaultState.GetValueOrDefault<string>(instanceSave.Name + "." + "Contained Type");
                }

                toReturn = ElementSaveExtensions.ToGraphicalUiElement(instanceElement, systemManagers, 
                    // don't add to managers, this is going to be added to the owner
                    addToManagers: false, genericType:genericType);
                toReturn.Name = instanceSave.Name;

                toReturn.Tag = instanceSave;
            }

            return toReturn;

        }

    }
}
