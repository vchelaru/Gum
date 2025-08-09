using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Managers
{
    public class NameVerifier
    {
        #region Fields/Properties

        public static char[] InvalidCharacters =
                new char[] 
            { 
                '~', '`', '!', '@', '#', '$', '%', '^', '&', '*', 
                '(', ')', '-', '=', '+', ';', '\'', ':', '"', '<', 
                ',', '>', '.', '/', '\\', '?', '[', '{', ']', '}', 
                '|', 
                // Spaces are handled separately
            //    ' ' 
            };


        public static HashSet<string> InvalidWindowsFileNames = new HashSet<string>
        {
            "con",
            "prn",
            "aux",
            "nul",
            "com0",
            "com1",
            "com2",
            "com3",
            "com4",
            "com5",
            "com6",
            "com7",
            "com8",
            "com9",
            "lpt0",
            "lpt1",
            "lpt2",
            "lpt3",
            "lpt4",
            "lpt5",
            "lpt6",
            "lpt7",
            "lpt8",
            "lpt9",
        };
        
        private readonly StandardElementsManager _standardElementsManager;

        #endregion

        #region Folder

        public bool IsFolderNameValid(string folderName, out string whyNotValid)
        {

            IsNameValidCommon(folderName, out whyNotValid);

            return string.IsNullOrEmpty(whyNotValid);

        }

        #endregion

        public NameVerifier()
        {
            _standardElementsManager = StandardElementsManager.Self;
        }

        public bool IsElementNameValid(string componentNameWithoutFolder, string folderName, ElementSave elementSave, out string whyNotValid)
        {
            IsNameValidCommon(componentNameWithoutFolder, out whyNotValid);

            if (string.IsNullOrEmpty(whyNotValid))
            {
                IsFileNameWindowsReserved(componentNameWithoutFolder, out whyNotValid);
            }
            //if (string.IsNullOrEmpty(whyNotValid))
            //{
            //    IsNameValidVariable(componentName, out whyNotValid);
            //}

            if (string.IsNullOrEmpty(whyNotValid))
            {
                IsNameAnExistingElement(componentNameWithoutFolder, folderName, elementSave, out whyNotValid);
            }

            return string.IsNullOrEmpty(whyNotValid);
        }


        internal bool IsStateNameValid(string name, StateSaveCategory category, StateSave stateSave, out string whyNotValid)
        {
            IsNameValidCommon(name, out whyNotValid);

            if(string.IsNullOrEmpty(whyNotValid))
            {
                var existing = category?.States.Find(item => item.Name == name && item != stateSave);

                if (existing != null)
                {
                    whyNotValid = $"The category {category.Name} already has a state named {name}";
                }
            }
            return string.IsNullOrEmpty(whyNotValid);
        }

        public bool IsInstanceNameValid(string instanceName, InstanceSave instanceSave, IInstanceContainer instanceContainer, out string whyNotValid)
        {
            IsNameValidCommon(instanceName, out whyNotValid);

            //if (string.IsNullOrEmpty(whyNotValid))
            //{
            //    IsNameValidVariable(instanceName, out whyNotValid);
            //}

            // See if this is a variable used by any state in the StandardElementsManager:
            if(string.IsNullOrEmpty(whyNotValid))
            {
                IsNameUsedByStandardVariables(instanceName, out whyNotValid);
            }

            if (string.IsNullOrEmpty(whyNotValid))
            {
                IsNameAlreadyUsed(instanceName, instanceSave, instanceContainer, out whyNotValid);
            }
            return string.IsNullOrEmpty(whyNotValid);
        }

        private void IsNameUsedByStandardVariables(string nameToCheck, out string whyNotValid)
        {
            var variables = _standardElementsManager.DefaultStates.SelectMany(item => item.Value.Variables);

            var names = variables.Select(item => item.Name).ToHashSet();

            whyNotValid = null;
            if(names.Contains(nameToCheck))
            {
                whyNotValid = $"The name {nameToCheck} cannot be used because it is a reserved variable name";
            }
            else if(nameToCheck == "Name")
            {
                whyNotValid = $"\"Name\" is a reserved keyword so it cannot be used as a name";
            }
        }

        public bool IsVariableNameValid(string variableName, ElementSave elementSave, VariableSave variableSave, out string whyNotValid)
        {
            whyNotValid = null;

            IsNameValidCommon(variableName, out whyNotValid);

            if (string.IsNullOrEmpty(whyNotValid) && elementSave != null)
            {
                IsNameAlreadyUsed(variableName, variableSave, elementSave, out whyNotValid);
            }

            if (string.IsNullOrEmpty(whyNotValid))
            {
                var existingVariable = elementSave?.GetVariableFromThisOrBase(variableName);

                // there's a variable but we shouldn't consider it
                // unless it's "Active" - inactive variables may be
                // leftovers from a type change


                if(existingVariable != null && elementSave != null)
                {
                    var isActive = VariableSaveLogic.GetIfVariableIsActive(existingVariable,
                        elementSave, null);
                    if(isActive)
                    {
                        whyNotValid = $"The variable name {variableName} is already used";
                    }
                }
            }

            return string.IsNullOrEmpty(whyNotValid);
        }

        public bool IsBehaviorNameValid(string behaviorName, BehaviorSave behaviorSave, out string whyNotValid)
        {
            IsNameValidCommon(behaviorName, out whyNotValid);

            if (string.IsNullOrEmpty(whyNotValid))
            {
                IsFileNameWindowsReserved(behaviorName, out whyNotValid);
            }

            if (string.IsNullOrEmpty(whyNotValid))
            {
                // need to check for duplicate names eventually
            }
            return string.IsNullOrEmpty(whyNotValid);
        }

        private void IsNameValidCommon(string name, out string whyNotValid)
        {
            whyNotValid = null;
            if (string.IsNullOrEmpty(name))
            {
                whyNotValid = "Empty names are not valid";
            }
            else if (name.IndexOfAny(InvalidCharacters) != -1)
            {
                whyNotValid = "The name can't contain invalid character " + name[name.IndexOfAny(InvalidCharacters)];
            }

            if(string.IsNullOrEmpty(whyNotValid) && name.StartsWith(" "))
            {
                whyNotValid = "The name can't begin with a space";
            }


            if (string.IsNullOrEmpty(whyNotValid) && name.EndsWith(" "))
            {
                whyNotValid = "The name can't end with a space";
            }
        }

        private void IsNameValidVariable(string name, out string whyNotValid)
        {
            whyNotValid = null;

            CodeDomProvider provider = CodeDomProvider.CreateProvider("C#");
            if (provider.IsValidIdentifier(name) == false)
            {
                whyNotValid = "The name uses an invalid character";
            }
        }

        private void IsFileNameWindowsReserved(string name, out string whyNotValid)
        {
            whyNotValid = null;
            if (InvalidWindowsFileNames.Contains(name?.ToLower()))
            {
                whyNotValid = $"The name {name} is a reserved file name in Windows";
            }
        }

        public bool IsComponentNameAlreadyUsed(string name)
        {
            return ObjectFinder.Self.GetComponent(name) != null;
        }

        private void IsNameAlreadyUsed(string name, object objectToIgnore, IInstanceContainer instanceContainer, out string whyNotValid)
        {
            whyNotValid = null;

            if (objectToIgnore != instanceContainer && Standardize(name) == Standardize(instanceContainer.Name))
            {
                whyNotValid = $"The element is named '{instanceContainer.Name}'";
            }

            var instance = instanceContainer.Instances.FirstOrDefault(item => item != objectToIgnore && Standardize(item.Name) == Standardize(name));
            if (instance != null)
            {
                whyNotValid = $"There is already an instance named '{instance.Name}'";
            }

            var stateContainer = instanceContainer as IStateContainer;

            var state = stateContainer?.AllStates.FirstOrDefault(item => item != objectToIgnore && Standardize(item.Name) == Standardize(name));
            if (state != null)
            {
                whyNotValid = $"There is already a state named '{state.Name}'";
            }
            
            var variable = stateContainer?.AllStates
                                          .SelectMany(item => item.Variables)
                                          .FirstOrDefault(item => item != objectToIgnore && Standardize(item.ExposedAsName) == Standardize(name));
            if (variable != null)
            {
                whyNotValid = $"There is already a variable named '{variable.Name}'";
            }

            //element = ObjectFinder.Self.GumProjectSave.StandardElements.FirstOrDefault(item=>item != objectToIgnore && item.Name == name)
            //{
            //    whyNotValid = "There is a standard element named " + element.Name + " so this name can't be used.";
            //}
        }

        private void IsNameAnExistingElement(string name, string folderName, object objectToIgnore, out string whyNotValid)
        {
            whyNotValid = null;

            string newStandardizedNameWithFolder = Standardize(folderName + name);

            var standardElement = ObjectFinder.Self.GumProjectSave.StandardElements.FirstOrDefault(item =>
                item != objectToIgnore && Standardize(item.Name) == newStandardizedNameWithFolder);
            if (standardElement != null)
            {
                whyNotValid = "There is a standard element named " + standardElement.Name + " so this name can't be used.";
            }

            var component = ObjectFinder.Self.GumProjectSave.Components.FirstOrDefault(item =>
                item != objectToIgnore && Standardize(item.Name) == newStandardizedNameWithFolder);
            if (component != null)
            {
                whyNotValid = "There is a component named " + component.Name + " so this name can't be used.";
            }

            var screen = ObjectFinder.Self.GumProjectSave.Screens.FirstOrDefault(item =>
                item != objectToIgnore && Standardize(item.Name) == newStandardizedNameWithFolder);
            if (screen != null)
            {
                whyNotValid = "There is a screen named " + screen.Name + " so this name can't be used.";
            }
        }

        private string Standardize(string name)
        {
            if (name == null) return null;
            
            string formatted = name.Replace(" ", "_");
            return formatted.ToLowerInvariant();
        }

        public bool IsNameValidAndroidFile(string name, out string whyNotValid)
        {
            whyNotValid = null;
            for(int i = 0; i < name.Length; i++)
            {
                if(!IsValidAndroidFileCharacter(name[i]))
                {
                    whyNotValid = $"The character {name[i]} is not supported on Android.";
                    break;
                }
            }

            return string.IsNullOrEmpty(whyNotValid);
        }

        private bool IsValidAndroidFileCharacter(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                //(c >= 'A' && c <= 'Z' && allowUpperCase) ||
                (c == '_') ||
                (c >= '0' && c <= '9');
        }

    }
}
