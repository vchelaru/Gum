using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using System;

namespace GumRuntime
{
    public static class InstanceSaveExtensionMethods
    {

        public static GraphicalUiElement ToGraphicalUiElement(this InstanceSave instanceSave, ISystemManagers systemManagers)
        {
#if DEBUG
            if(ObjectFinder.Self.GumProjectSave == null)
            {
                throw new InvalidOperationException("You need to set the ObjectFinder's GumProjectSave first so it can track references");
            }
#endif
            ElementSave instanceElement = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);

            GraphicalUiElement toReturn = null;
            if (instanceElement != null)
            {
                string genericType = null;


                if(instanceElement.Name == "Container" && instanceElement is StandardElementSave)
                {
                    genericType = instanceSave.ParentContainer.DefaultState.GetValueOrDefault<string>(instanceSave.Name + "." + "Contained Type");
                }

                toReturn = ElementSaveExtensions.CreateGueForElement(instanceElement, true, genericType);

                // Feb 7, 2024 - why not set the Name first before calling SetGraphicalUiElement? This would
                // help debugging...
                toReturn.Name = instanceSave.Name;

                // If we get here but there's no contained graphical object then that means we don't
                // have a strongly-typed system. Therefore, we'll
                // just fall back to the regular creation of graphical objects, like is done in the Gum tool:
                if(toReturn.RenderableComponent == null)
                {
                    instanceElement.SetGraphicalUiElement(toReturn, systemManagers);
                }

                toReturn.Tag = instanceSave;
            }

            return toReturn;

        }

    }
}
