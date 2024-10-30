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

                bool byElement = true;
                if(byElement)
                {

                    toReturn = ElementSaveExtensions.ToGraphicalUiElement(instanceElement, systemManagers, 
                        // don't add to managers, this is going to be added to the owner
                        addToManagers: false);
                    toReturn.Name = instanceSave.Name;

                }
                else
                {
                    // 10/29/2024 - we now do by-element above to unify the code, but keeping this here justin case
                    // old code, not sure if we need this:
                    //toReturn = ElementSaveExtensions.CreateGueForElement(instanceElement, true, genericType);

                    //// Feb 7, 2024 - why not set the Name first before calling SetGraphicalUiElement? This would
                    //// help debugging...
                    //toReturn.Name = instanceSave.Name;

                    //// If we get here but there's no contained graphical object then that means we don't
                    //// have a strongly-typed system. Therefore, we'll
                    //// just fall back to the regular creation of graphical objects, like is done in the Gum tool:
                    //if (toReturn.RenderableComponent == null)
                    //{
                    //    instanceElement.SetGraphicalUiElement(toReturn, systemManagers);
                    //}
                    //else
                    //{
                    //    // Do most of the things that would happen in the SetGraphicalUiElement, but don't actually
                    //    // call SetGraphicalUiElement because that would potentially re-create children
                    //    toReturn.SetStatesAndCategoriesRecursively(instanceElement);

                    //    toReturn.AddExposedVariablesRecursively(instanceElement);

                    //    toReturn.Tag = instanceElement;

                    //    toReturn.SetInitialState();

                    //    toReturn.AfterFullCreation();
                    //}
                }

                toReturn.Tag = instanceSave;
            }

            return toReturn;

        }

    }
}
