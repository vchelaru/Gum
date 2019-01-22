using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GumRuntime
{
    public static class ElementSaveExtensions
    {
        static Dictionary<string, Type> mElementToGueTypes = new Dictionary<string, Type>();

        public static void RegisterGueInstantiationType(string elementName, Type gueInheritingType)
        {
            mElementToGueTypes[elementName] = gueInheritingType;
        }

        public static GraphicalUiElement CreateGueForElement(ElementSave elementSave, bool fullInstantiation = false)
        {
            GraphicalUiElement toReturn = null;


            if (mElementToGueTypes.ContainsKey(elementSave.Name))
            {
                // This code allows sytems (like games that use Gum) to assign types
                // to their GraphicalUiElements so that users of the code can work with
                // strongly-typed Gum objects.
                var type = mElementToGueTypes[elementSave.Name];
                var constructor = type.GetConstructor(new Type[] { typeof(bool), typeof(bool)});


                toReturn = constructor.Invoke(new object[] { fullInstantiation, true}) as GraphicalUiElement;
            }
            else
            {
                toReturn = new GraphicalUiElement();
            }
            toReturn.ElementSave = elementSave;
            return toReturn;
        }


        public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave)
        {
            var toReturn = ToGraphicalUiElement(elementSave, SystemManagers.Default, addToManagers: true);

#if DEBUG
            if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
            {
                ThrowMissingFileExceptionsRecursively(toReturn);
            }
#endif

            return toReturn;
        }

        private static void ThrowMissingFileExceptionsRecursively(GraphicalUiElement toReturn)
        {

            // We can't throw exceptions when assigning values on fonts because the font values get set one-by-one
            // and the end result of all values determines which file to load. For example, an object may set the following
            // variables one-by-one:
            // * FontSize
            // * Font
            // * OutlineThickness
            // Let's say the Font gets set to Arial. The FontSize may not have been set yet, so whatever value happens
            // to be there will be used to load the font (like 12). But the user may not have Arial12 in their project,
            // and if we threw an exception on-the-spot, the user would see a message about missing Arial12, even though
            // the project doesn't actually use Arial12.
            // We need to wait until the graphical UI element is fully created before we try to throw an exception, so
            // that's what we're going to do here:
            if (toReturn != null && toReturn.RenderableComponent is Text)
            {
                // check it
                var asText = toReturn.RenderableComponent as Text;
                if (asText.BitmapFont == null)
                {
                    if (toReturn.UseCustomFont)
                    {
                        var fontName = ToolsUtilities.FileManager.Standardize(toReturn.CustomFontFile, preserveCase:true, makeAbsolute:true);

                        throw new System.IO.FileNotFoundException($"Missing:{fontName}");
                    }
                    else
                    {
                        if (toReturn.FontSize > 0 && !string.IsNullOrEmpty(toReturn.Font))
                        {
                            string fontName = global::RenderingLibrary.Graphics.Fonts.BmfcSave.GetFontCacheFileNameFor(
                                toReturn.FontSize,
                                toReturn.Font,
                                toReturn.OutlineThickness,
                                toReturn.UseFontSmoothing);

                            var standardized = ToolsUtilities.FileManager.Standardize(fontName, preserveCase:true, makeAbsolute:true);

                            throw new System.IO.FileNotFoundException($"Missing:{standardized}");
                        }

                    }

                }
            }

            
            foreach (var element in toReturn.ContainedElements)
            {
                ThrowMissingFileExceptionsRecursively(element);
            }
        }

        public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers systemManagers, 
            bool addToManagers)
        {
            GraphicalUiElement toReturn = CreateGueForElement(elementSave);

            elementSave.SetGraphicalUiElement(toReturn, systemManagers);

            //no layering support yet
            if (addToManagers)
            {
                toReturn.AddToManagers(systemManagers, null);
            }

            return toReturn;
        }

        public static void SetStatesAndCategoriesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        {

            if(!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if(baseElementSave != null)
                {
                    graphicalElement.SetStatesAndCategoriesRecursively(baseElementSave);
                }
            }

            // We need to set categories and states before calling SetGraphicalUiElement so that the states can be used
            foreach (var category in elementSave.Categories)
            {
                graphicalElement.AddCategory(category);
            }

            graphicalElement.AddStates(elementSave.States);
        }

        public static void CreateGraphicalComponent(this GraphicalUiElement graphicalElement, ElementSave elementSave, SystemManagers systemManagers)
        {
            IRenderable containedObject = null;

            bool handled = InstanceSaveExtensionMethods.TryHandleAsBaseType(elementSave.Name, systemManagers, out containedObject);

            if (handled)
            {
                graphicalElement.SetContainedObject(containedObject);
            }
            else
            {
                if (elementSave != null && elementSave is ComponentSave)
                {
                    var baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);

                    if (baseElement != null)
                    {
                        graphicalElement.CreateGraphicalComponent(baseElement, systemManagers);
                    }
                }
            }
        }

        static void AddExposedVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        {
            if (!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if (baseElementSave != null)
                {
                    graphicalElement.AddExposedVariablesRecursively(baseElementSave);
                }
            }


            if (elementSave != null)
            {
                foreach (var variable in elementSave.DefaultState.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)))
                {
                    graphicalElement.AddExposedVariable(variable.ExposedAsName, variable.Name);
                }
            }

        }


        // Replaced with ApplyDefaultState
        //static void SetVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        //{
        //    graphicalElement.SetVariablesRecursively(elementSave, elementSave.DefaultState);
        //}
        
        public static void SetVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave, Gum.DataTypes.Variables.StateSave stateSave)
        {
            if (!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if (baseElementSave != null)
                {

                    graphicalElement.SetVariablesRecursively(baseElementSave, baseElementSave.DefaultState);
                }
            }

            graphicalElement.ApplyState(stateSave);
        }

        public static void SetGraphicalUiElement(this ElementSave elementSave, GraphicalUiElement toReturn, SystemManagers systemManagers)
        {
            // We need to set categories and states first since those are used below;
            toReturn.SetStatesAndCategoriesRecursively(elementSave);

            toReturn.CreateGraphicalComponent(elementSave, systemManagers);

            toReturn.AddExposedVariablesRecursively(elementSave);

            toReturn.CreateChildrenRecursively(elementSave, systemManagers);

            toReturn.Tag = elementSave;

            toReturn.SetInitialState();
        }



    }
}
