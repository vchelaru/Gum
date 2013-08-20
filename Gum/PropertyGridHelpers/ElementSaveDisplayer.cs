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


namespace Gum.PropertyGridHelpers
{
    #region AmountToDisplay Enum

    public enum AmountToDisplay
    {
        AllVariables,
        ElementAndExposedOnly
    }
    #endregion

    public class ElementSaveDisplayer : PropertyGridDisplayer
    {
        #region Fields

        PropertyDescriptorHelper mHelper = new PropertyDescriptorHelper();

        #endregion


        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection pdc =
                TypeDescriptor.GetProperties(this, true);


            StateSave stateSave = SelectedState.Self.SelectedStateSave;
            ElementSave elementSave = SelectedState.Self.SelectedElement;
            InstanceSave instanceSave = SelectedState.Self.SelectedInstance;

            if (instanceSave != null && stateSave != null)
            {
                pdc = DisplayCurrentInstance(pdc, instanceSave);

            }
            else if (elementSave != null && stateSave != null)
            {
                pdc = DisplayCurrentElement(pdc, elementSave, null, elementSave.States[0], null);


            }

            return pdc;
        }

        private PropertyDescriptorCollection DisplayCurrentInstance(PropertyDescriptorCollection pdc, InstanceSave instanceSave)
        {
            ElementSave elementSave = instanceSave.GetBaseElementSave();

            StateSave stateToDisplay;

            if (elementSave != null)
            {
                stateToDisplay = elementSave.States[0];
            }
            else
            {
                stateToDisplay = new StateSave();
            }

            pdc = DisplayCurrentElement(pdc, elementSave, instanceSave, stateToDisplay, instanceSave.Name, AmountToDisplay.ElementAndExposedOnly);

            return pdc;

        }

        private PropertyDescriptorCollection DisplayCurrentElement(PropertyDescriptorCollection pdc, ElementSave elementSave, 
            InstanceSave instanceSave, StateSave defaultState, string prependedVariable, AmountToDisplay amountToDisplay = AmountToDisplay.AllVariables)
        {
            bool isDefault = SelectedState.Self.SelectedStateSave == SelectedState.Self.SelectedElement.DefaultState;

            if (!string.IsNullOrEmpty(prependedVariable))
            {
                prependedVariable += ".";
            }

            bool isCustomType = (elementSave is StandardElementSave) == false;
            if (isDefault && ( isCustomType || instanceSave != null))
            {
                pdc = AddNameAndBaseTypeProperties(pdc);
            }

            if (instanceSave != null)
            {
                if (isDefault)
                {
                    pdc = mHelper.AddProperty(pdc, "Locked", typeof(bool));
                }
            }

            // if component
            if (instanceSave == null && elementSave as ComponentSave != null)
            {
                foreach(var item in StandardElementsManager.Self.GetDefaultStateFor("Component").Variables)
                {

                    pdc = TryDisplayVariableSave(pdc, elementSave, instanceSave, amountToDisplay, null, item);
                }
            }
            // else if screen
            else if (instanceSave == null && elementSave as ScreenSave != null)
            {
                foreach (var item in StandardElementsManager.Self.GetDefaultStateFor("Screen").Variables)
                {

                    pdc = TryDisplayVariableSave(pdc, elementSave, instanceSave, amountToDisplay, null, item);
                }
            }



            #region Get the StandardElementSave for the instance/element (depending on what's selected)

            StandardElementSave ses = null;

            if (instanceSave != null)
            {
                ses = ObjectFinder.Self.GetRootStandardElementSave(instanceSave);
            }
            else if ((elementSave is ScreenSave) == false)
            {
                ses = ObjectFinder.Self.GetRootStandardElementSave(elementSave);
            }

            #endregion

            #region Loop through all variables

            // We want to use the default state to get all possible
            // variables because the default state will always set all
            // variables.  We then look at the current state to get the
            // actual value
            for (int i = 0; i < defaultState.Variables.Count; i++)
            {
                VariableSave defaultVariable = defaultState.Variables[i];

                pdc = TryDisplayVariableSave(pdc, elementSave, instanceSave, amountToDisplay, ses, defaultVariable);
            }

            #endregion


            #region Loop through all list variables

            for (int i = 0; i < defaultState.VariableLists.Count; i++)
            {
                VariableListSave variableList = defaultState.VariableLists[i];

                bool shouldInclude = true;
                // Eventually will want to implement this:
                shouldInclude = (string.IsNullOrEmpty(variableList.SourceObject)
                    // Not sure what this amountToDisplay is all about...
                    //|| amountToDisplay == AmountToDisplay.AllVariables
                    //|| !string.IsNullOrEmpty(variableList.ExposedAsName))
                );

                if (shouldInclude)
                {

                    TypeConverter typeConverter = variableList.GetTypeConverter();

                    Attribute[] customAttributes = GetAttributesForVariable(variableList);


                    Type type = typeof(List<string>);



                    pdc = mHelper.AddProperty(pdc,
                        variableList.Name,
                        type,
                        typeConverter,
                        //    //,
                        customAttributes
                        );
                }
            }




            #endregion



            return pdc;
        }

        private PropertyDescriptorCollection TryDisplayVariableSave(PropertyDescriptorCollection pdc, ElementSave elementSave, InstanceSave instanceSave, 
            AmountToDisplay amountToDisplay, StandardElementSave ses, VariableSave defaultVariable)
        {
            bool shouldInclude = GetIfShouldInclude(defaultVariable, elementSave, instanceSave, ses);

            shouldInclude &= (
                string.IsNullOrEmpty(defaultVariable.SourceObject) || 
                amountToDisplay == AmountToDisplay.AllVariables || 
                !string.IsNullOrEmpty(defaultVariable.ExposedAsName));

            if (shouldInclude)
            {

                TypeConverter typeConverter = defaultVariable.GetTypeConverter(elementSave);

                Attribute[] customAttributes = GetAttributesForVariable(defaultVariable);


                Type type = typeof(string);

                string name = defaultVariable.Name;

                if (!string.IsNullOrEmpty(defaultVariable.ExposedAsName))
                {
                    name = defaultVariable.ExposedAsName;
                }

                pdc = mHelper.AddProperty(pdc,
                    name,
                    type,
                    typeConverter,
                    //,
                    customAttributes
                    );
            }
            return pdc;
        }

        private PropertyDescriptorCollection AddNameAndBaseTypeProperties(PropertyDescriptorCollection pdc)
        {
            pdc = mHelper.AddProperty(pdc,
                "Name", typeof(string), TypeDescriptor.GetConverter(typeof(string)), new Attribute[]
                        { 
                            new CategoryAttribute("\tObject") // \t isn't rendered, but it is sorted on.  Hack to get this property to appear first
                        });

            bool isDisplayingScreen =
                SelectedState.Self.SelectedInstance == null &&
                SelectedState.Self.SelectedScreen != null;

            if (!isDisplayingScreen)
            {
                // We may want to support Screens inheriting from other Screens in the future, but for now we won't allow it
                pdc = mHelper.AddProperty(pdc,
                    "Base Type", typeof(string), new AvailableBaseTypeConverter(), new Attribute[]
                        { 
                            new CategoryAttribute("\tObject") // \t isn't rendered, but it is sorted on.  Hack to get this property to appear first
                        });
            }
            return pdc;
        }

        private static bool GetIfShouldInclude(VariableSave defaultVariable, ElementSave container, InstanceSave currentInstance, StandardElementSave rootElementSave)
        {
            bool canOnlyBeSetInDefaultState = defaultVariable.CanOnlyBeSetInDefaultState;
            if (currentInstance != null)
            {
                var root = ObjectFinder.Self.GetRootStandardElementSave(currentInstance);
                if (root != null && root.GetVariableFromThisOrBase(defaultVariable.Name, true) != null)
                {
                    var foundVariable = root.GetVariableFromThisOrBase(defaultVariable.Name, true);
                    canOnlyBeSetInDefaultState = foundVariable.CanOnlyBeSetInDefaultState;
                }
            }
            else if (container != null)
            {
                var root = ObjectFinder.Self.GetRootStandardElementSave(container);
                if (root != null && root.GetVariableFromThisOrBase(defaultVariable.Name, true) != null)
                {
                    canOnlyBeSetInDefaultState = root.GetVariableFromThisOrBase(defaultVariable.Name, true).CanOnlyBeSetInDefaultState;
                }
            }
            bool shouldInclude = true;

            bool isDefault = SelectedState.Self.SelectedStateSave == SelectedState.Self.SelectedElement.DefaultState;

            if (!isDefault && canOnlyBeSetInDefaultState)
            {
                shouldInclude = false;
            }

            if (shouldInclude)
            {
                shouldInclude = GetShouldIncludeBasedOnAttachments(defaultVariable, container, currentInstance);
            }

            if (shouldInclude)
            {
                shouldInclude = GetShouldIncludeBasedOnBaseType(defaultVariable, container, rootElementSave);
            }

            return shouldInclude;
        }

        private static bool GetShouldIncludeBasedOnAttachments(VariableSave variableSave, ElementSave container, InstanceSave currentInstance)
        {
            bool toReturn = true;
            if (variableSave.Name == "Guide")
            {
                if(currentInstance != null && SelectedState.Self.SelectedScreen == null)
                {
                    toReturn = false;
                }
            }

            return toReturn;
        }

        private static bool GetShouldIncludeBasedOnBaseType(VariableSave defaultVariable, ElementSave container, StandardElementSave rootElementSave)
        {
            bool shouldInclude = false;

            if (string.IsNullOrEmpty(defaultVariable.SourceObject))
            {
                if (container is ScreenSave )
                {
                    // If it's a Screen, then the answer is "yes" because
                    // Screens don't have a base type that they can switch,
                    // so any variable that's part of the Screen is always part
                    // of the Screen.
                    // do nothing to shouldInclude

                    shouldInclude = true;
                }
                else
                {
                    if (container is ComponentSave)
                    {
                        // See if it's defined in the standards list
                        var foundInstance = StandardElementsManager.Self.GetDefaultStateFor("Component").Variables.FirstOrDefault(
                            item => item.Name == defaultVariable.Name);

                        shouldInclude = foundInstance != null;
                    }
                    // If the defaultVariable's
                    // source object is null then
                    // that means that the variable
                    // is being set on "this".  However,
                    // variables that are set on "this" may
                    // not actually be valid for the type, but
                    // they may still exist because the object type
                    // was switched.  Therefore, we want to make sure
                    // that the variable is valid given the type of object
                    // that "this" currently is by checking the default state
                    // on the rootElementSave
                    if (!shouldInclude && rootElementSave != null)
                    {
                        shouldInclude = rootElementSave.DefaultState.GetVariableSave(defaultVariable.Name) != null;
                    }
                }
            }

            else
            {

                shouldInclude = SelectedState.Self.SelectedInstance != null || !string.IsNullOrEmpty(defaultVariable.ExposedAsName);
            }
            return shouldInclude;
        }

        private Attribute[] GetAttributesForVariable(VariableSave defaultVariable)
        {
            Attribute[] customAttributes = null;

            mListOfAttributes.Clear();

            if (!string.IsNullOrEmpty(defaultVariable.Category))
            {
                mListOfAttributes.Add(new CategoryAttribute(defaultVariable.Category));
            }
            else if (!string.IsNullOrEmpty(defaultVariable.ExposedAsName))
            {
                mListOfAttributes.Add(new CategoryAttribute("Exposed"));
            }
            if (defaultVariable.IsFile)
            {
                mListOfAttributes.Add(mFileWindowAttribute);
            }

            if (defaultVariable.IsHiddenInPropertyGrid)
            {
                mListOfAttributes.Add(new BrowsableAttribute(false));
            }

            List<Attribute> attributesFromPlugins = PluginManager.Self.GetAttributesFor(defaultVariable);

            if(attributesFromPlugins.Count != 0)
            {
                mListOfAttributes.AddRange(attributesFromPlugins);
            }


            if (mListOfAttributes.Count != 0)
            {
                customAttributes = mListOfAttributes.ToArray();
            }
            else
            {
                customAttributes = mEmptyList;
            }
            return customAttributes;
        }


        private Attribute[] GetAttributesForVariable(VariableListSave variableList)
        {

            mListOfAttributes.Clear();

            EditorAttribute editorAttribute = new EditorAttribute(
                //"System.Windows.Forms.Design.ListControlStringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", 
                typeof(VariableListConverter),
                typeof(UITypeEditor));
            mListOfAttributes.Add(editorAttribute);

            if (!string.IsNullOrEmpty(variableList.Category))
            {
                mListOfAttributes.Add(new CategoryAttribute(variableList.Category));
            }

            if (variableList.IsHiddenInPropertyGrid)
            {
                mListOfAttributes.Add(new BrowsableAttribute(false));
            }


            return mListOfAttributes.ToArray();
        }
    }
}
