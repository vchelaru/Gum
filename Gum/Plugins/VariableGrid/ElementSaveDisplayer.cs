using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Gum.DataTypes.Variables;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.DataTypes.ComponentModel;
using Gum.Managers;
using Gum.PropertyGridHelpers.Converters;
using System.Drawing.Design;
using Gum.Plugins;
using Gum.Logic;

namespace Gum.PropertyGridHelpers
{
    #region AmountToDisplay Enum

    public enum AmountToDisplay
    {
        AllVariables,
        ElementAndExposedOnly
    }
    #endregion

    public class ElementSaveDisplayer
    {
        #region Fields
        static EditorAttribute mFileWindowAttribute = new EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor));

        static PropertyDescriptorHelper mHelper = new PropertyDescriptorHelper();

        #endregion

        public List<InstanceSavePropertyDescriptor> GetProperties(Attribute[] attributes)
        {
            // search terms: display properties, display variables, show variables, variable display, variable displayer
            List<InstanceSavePropertyDescriptor> pdc = new List<InstanceSavePropertyDescriptor>();


            StateSave stateSave = SelectedState.Self.SelectedStateSave;
            ElementSave elementSave = SelectedState.Self.SelectedElement;
            InstanceSave instanceSave = SelectedState.Self.SelectedInstance;

            if (instanceSave != null && stateSave != null)
            {
                DisplayCurrentInstance(pdc, instanceSave);

            }
            else if (elementSave != null && stateSave != null)
            {
                StateSave defaultState = GetRecursiveStateFor(elementSave);

                DisplayCurrentElement(pdc, elementSave, null, defaultState, null);


            }

            return pdc;
        }

        private static StateSave GetRecursiveStateFor(ElementSave elementSave, StateSave stateToAddTo = null)
        {
            // go bottom up
            var baseElement = ObjectFinder.Self.GetElementSave(elementSave.BaseType);
            if (baseElement != null)
            {
                stateToAddTo = GetRecursiveStateFor(baseElement, stateToAddTo);
            }
            if(stateToAddTo == null)
            {
                stateToAddTo = new StateSave();
            }


            var existingVariableNames = stateToAddTo.Variables.Select(item => item.Name);

            if(elementSave is StandardElementSave)
            {
                var defaultStates = StandardElementsManager.Self.GetDefaultStateFor(elementSave.Name);
                var variablesToAdd = defaultStates.Variables
                    .Select(item => item.Clone())
                    .Where(item => existingVariableNames.Contains(item.Name) == false);

                stateToAddTo.Variables.AddRange(variablesToAdd);

            }
            else
            {
                var variablesToAdd = elementSave.DefaultState.Variables
                    .Select(item => item.Clone())
                    .Where(item => existingVariableNames.Contains(item.Name) == false);

                stateToAddTo.Variables.AddRange(variablesToAdd);
            }



            return stateToAddTo;
        }

        private void DisplayCurrentInstance(List<InstanceSavePropertyDescriptor> pdc, InstanceSave instanceSave)
        {
            ElementSave elementSave = instanceSave.GetBaseElementSave();

            StateSave stateToDisplay;

            if (elementSave != null)
            {
                if (elementSave is StandardElementSave)
                {
                    // if we use the standard elements manager, we don't get any custom categories, so we need to add those:
                    stateToDisplay = StandardElementsManager.Self.GetDefaultStateFor(elementSave.Name).Clone();
                    foreach(var category in elementSave.Categories)
                    {
                        var expectedName = category.Name + "State";

                        var variable = elementSave.GetVariableFromThisOrBase(expectedName);
                        if(variable != null)
                        {
                            stateToDisplay.Variables.Add(variable);
                        }
                    }
                }
                else
                {
                    stateToDisplay = GetRecursiveStateFor(elementSave);
                }
            }
            else
            {
                stateToDisplay = new StateSave();
            }

            DisplayCurrentElement(pdc, elementSave, instanceSave, stateToDisplay, instanceSave.Name, AmountToDisplay.ElementAndExposedOnly);
            
        }

        private static void DisplayCurrentElement(List<InstanceSavePropertyDescriptor> pdc, ElementSave elementSave, 
            InstanceSave instanceSave, StateSave defaultState, string prependedVariable, AmountToDisplay amountToDisplay = AmountToDisplay.AllVariables)
        {
            bool isDefault = SelectedState.Self.SelectedStateSave == SelectedState.Self.SelectedElement.DefaultState;
            if(instanceSave?.DefinedByBase == true)
            {
                isDefault = false;
            }
            if (!string.IsNullOrEmpty(prependedVariable))
            {
                prependedVariable += ".";
            }

            bool isCustomType = (elementSave is StandardElementSave) == false;
            if ( isCustomType || instanceSave != null)
            {
                AddNameAndBaseTypeProperties(pdc, elementSave, instanceSave, isReadOnly:isDefault==false);
            }

            if (instanceSave != null)
            {
                mHelper.AddProperty(pdc, "Locked", typeof(bool)).IsReadOnly = !isDefault;
            }

            // if component
            if (instanceSave == null && elementSave as ComponentSave != null)
            {
                var variables = StandardElementsManager.Self.GetDefaultStateFor("Component").Variables;
                foreach (var item in variables)
                {
                    // Don't add states here, because they're handled below from this object's Default:
                    if(item.IsState(elementSave) == false)
                    {
                        TryDisplayVariableSave(pdc, elementSave, instanceSave, amountToDisplay, item);
                    }
                }
            }
            // else if screen
            else if (instanceSave == null && elementSave as ScreenSave != null)
            {
                foreach (var item in StandardElementsManager.Self.GetDefaultStateFor("Screen").Variables)
                {

                    TryDisplayVariableSave(pdc, elementSave, instanceSave, amountToDisplay, item);
                }
            }



            #region Loop through all variables

            // We want to use the default state to get all possible
            // variables because the default state will always set all
            // variables.  We then look at the current state to get the
            // actual value
            for (int i = 0; i < defaultState.Variables.Count; i++)
            {
                VariableSave defaultVariable = defaultState.Variables[i];

                TryDisplayVariableSave(pdc, elementSave, instanceSave, amountToDisplay, defaultVariable);
            }

            #endregion


            #region Loop through all list variables

            for (int i = 0; i < defaultState.VariableLists.Count; i++)
            {
                VariableListSave variableList = defaultState.VariableLists[i];

                bool shouldInclude = GetIfShouldInclude(variableList, elementSave, instanceSave);

                if (shouldInclude)
                {

                    TypeConverter typeConverter = variableList.GetTypeConverter();

                    Attribute[] customAttributes = GetAttributesForVariable(variableList);


                    Type type = typeof(List<string>);



                    mHelper.AddProperty(pdc,
                        variableList.Name,
                        type,
                        typeConverter,
                        //    //,
                        customAttributes
                        );
                }
            }




            #endregion
            
        }

        private static void TryDisplayVariableSave(List<InstanceSavePropertyDescriptor> pdc, ElementSave elementSave, InstanceSave instanceSave, 
            AmountToDisplay amountToDisplay, VariableSave defaultVariable)
        {
            ElementSave container = elementSave;
            if (instanceSave != null)
            {
                container = instanceSave.ParentContainer;
            }

            // Not sure why we were passing elementSave to this function:
            // I added a container object
            //bool shouldInclude = GetIfShouldInclude(defaultVariable, elementSave, instanceSave, ses);
            bool shouldInclude = Gum.Logic.VariableSaveLogic.GetIfVariableIsActive(defaultVariable, container, instanceSave);

            shouldInclude &= (
                string.IsNullOrEmpty(defaultVariable.SourceObject) || 
                amountToDisplay == AmountToDisplay.AllVariables || 
                !string.IsNullOrEmpty(defaultVariable.ExposedAsName));

            if (shouldInclude)
            {
                TypeConverter typeConverter = defaultVariable.GetTypeConverter(elementSave);

                Attribute[] customAttributes = GetAttributesForVariable(defaultVariable);

                string category = null;
                if (!string.IsNullOrEmpty(defaultVariable.Category))
                {
                    category = defaultVariable.Category;
                }
                else if (!string.IsNullOrEmpty(defaultVariable.ExposedAsName))
                {
                    category = "Exposed";
                }

                //Type type = typeof(string);
                Type type = Gum.Reflection.TypeManager.Self.GetTypeFromString(defaultVariable.Type);

                string name = defaultVariable.Name;

                if (!string.IsNullOrEmpty(defaultVariable.ExposedAsName))
                {
                    name = defaultVariable.ExposedAsName;
                }

                var property = mHelper.AddProperty(pdc,
                    name,
                    type,
                    typeConverter,
                    //,
                    customAttributes
                    );
                property.Category = category;
            }
        }

        private static void AddNameAndBaseTypeProperties(List<InstanceSavePropertyDescriptor> pdc, ElementSave elementSave, InstanceSave instance, bool isReadOnly)
        {
            var nameProperty = mHelper.AddProperty(
                pdc,
                "Name", 
                typeof(string), 
                TypeDescriptor.GetConverter(typeof(string)), 
                new Attribute[]
                        { 
                            new CategoryAttribute("\tObject") // \t isn't rendered, but it is sorted on.  Hack to get this property to appear first
                        });

            nameProperty.IsReadOnly = isReadOnly;


            var baseTypeConverter = new AvailableBaseTypeConverter(elementSave, instance);

                // We may want to support Screens inheriting from other Screens in the future, but for now we won't allow it
            var baseTypeProperty = mHelper.AddProperty(pdc,
                "Base Type", typeof(string), baseTypeConverter, new Attribute[]
                    { 
                        new CategoryAttribute("\tObject") // \t isn't rendered, but it is sorted on.  Hack to get this property to appear first
                    });

            baseTypeProperty.IsReadOnly = isReadOnly;
        }

        private static bool GetIfShouldInclude(VariableListSave variableList, ElementSave container, InstanceSave currentInstance)
        {
            bool toReturn = (string.IsNullOrEmpty(variableList.SourceObject));

            if (toReturn)
            {
                StandardElementSave rootElementSave = null;

                if (currentInstance != null)
                {
                    rootElementSave = ObjectFinder.Self.GetRootStandardElementSave(currentInstance);
                }
                else if ((container is ScreenSave) == false)
                {
                    rootElementSave = ObjectFinder.Self.GetRootStandardElementSave(container);
                }

                toReturn = VariableSaveLogic.GetShouldIncludeBasedOnBaseType(variableList, container, rootElementSave);
            }

            return toReturn;
        }
            
        private static Attribute[] GetAttributesForVariable(VariableSave defaultVariable)
        {
            List<Attribute> attributes = new List<Attribute>();

            if (defaultVariable.IsFile)
            {
                attributes.Add(mFileWindowAttribute);
            }

            if (defaultVariable.IsHiddenInPropertyGrid)
            {
                attributes.Add(new BrowsableAttribute(false));
            }

            List<Attribute> attributesFromPlugins = PluginManager.Self.GetAttributesFor(defaultVariable);

            if(attributesFromPlugins.Count != 0)
            {
                attributes.AddRange(attributesFromPlugins);
            }

            return attributes.ToArray();
        }


        private static Attribute[] GetAttributesForVariable(VariableListSave variableList)
        {
            List<Attribute> attributes = new List<Attribute>();

            EditorAttribute editorAttribute = new EditorAttribute(
                //"System.Windows.Forms.Design.ListControlStringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", 
                typeof(VariableListConverter),
                typeof(UITypeEditor));
            attributes.Add(editorAttribute);

            if (!string.IsNullOrEmpty(variableList.Category))
            {
                attributes.Add(new CategoryAttribute(variableList.Category));
            }

            if (variableList.IsHiddenInPropertyGrid)
            {
                attributes.Add(new BrowsableAttribute(false));
            }


            return attributes.ToArray();
        }
    }
}
