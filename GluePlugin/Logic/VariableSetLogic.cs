using GluePlugin.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using GlueScreen = FlatRedBall.Glue.SaveClasses.ScreenSave;
using GumScreen = Gum.DataTypes.ScreenSave;

using GumElement = Gum.DataTypes.ElementSave;
using GlueElement = FlatRedBall.Glue.SaveClasses.IElement;


using GlueState = FlatRedBall.Glue.SaveClasses.StateSave;
using GlueStateCategory = FlatRedBall.Glue.SaveClasses.StateSaveCategory;

namespace GluePlugin.Logic
{
    public class VariableSetLogic : Singleton<VariableSetLogic>
    {
        internal void SetVariable(ElementSave gumElement, InstanceSave gumInstance, string variableName, object oldValue, bool save = true)
        {
            var glueProject = GluePluginState.Self.GlueProject;

            ///////////////////////Early Out///////////////////////
            if(glueProject == null || GluePluginState.Self.InitializationState != InitializationState.Initialized)
            {
                return;
            }
            ////////////////////End Early Out/////////////////////


            var shouldApplyValue = GetIfShouldApplyValue(gumElement, gumInstance, variableName);

            if(shouldApplyValue)
            {
                ApplyChangedVariable(gumElement, gumInstance, variableName, glueProject, save);
            }
            else
            {
                var currentState = SelectedState.Self.SelectedStateSave;

                currentState.SetValue($"{gumInstance.Name}.{variableName}", oldValue, "string");
            }
        }

        private static void ApplyChangedVariable(ElementSave gumElement, InstanceSave gumInstance, string variableName, FlatRedBall.Glue.SaveClasses.GlueProjectSave glueProject, bool save)
        {
            var glueElement = GluePluginObjectFinder.Self.GetGlueElementFrom(gumElement);
            

            /////////////////// early out
            if(glueElement == null)
            {
                return;
            }
            ///////////////endn early out

            var fullVariableName = variableName;
            FlatRedBall.Glue.SaveClasses.NamedObjectSave foundNos = null;

            if(gumInstance != null)
            {
                fullVariableName = $"{gumInstance.Name}.{variableName}";
                foundNos = glueElement.AllNamedObjects
                    .FirstOrDefault(item => item.InstanceName == gumInstance.Name);

            }
            var gumValue = gumElement.GetValueFromThisOrBase(fullVariableName);

            if (foundNos != null)
            {
                var handled = TryHandleAssigningMultipleVariables(gumElement, gumInstance, variableName, glueElement, foundNos, gumValue);

                if(!handled)
                {
                    HandleIndividualVariableAssignment(gumElement, gumInstance, variableName, glueElement, foundNos, gumValue);
                }
            }

            if (save)
            {
                FileManager.XmlSerialize(glueProject, GluePluginState.Self.GlueProjectFilePath.StandardizedCaseSensitive);
            }
        }

        private static bool TryHandleAssigningMultipleVariables(ElementSave gumElement, InstanceSave gumInstance, string variableName, GlueElement glueElement, FlatRedBall.Glue.SaveClasses.NamedObjectSave foundNos, object gumValue)
        {
            var isTextureValue = variableName == "Texture Address" ||
                variableName == "Texture Left" ||
                variableName == "Texture Top" ||
                variableName == "Texture Width" ||
                variableName == "Texture Height";

            var handled = false;

            if(isTextureValue)
            {
                string variablePrefix = null;
                if(gumInstance != null)
                {
                    variablePrefix = $"{gumInstance.Name}.";
                }
                var state = SelectedState.Self.SelectedStateSave;
                var addressValueGumCurrent = state.GetValue($"{variablePrefix}Texture Address");
                var textureLeftValueGumCurrent = state.GetValue($"{variablePrefix}Texture Left");
                var textureWidthValueGumCurrent = state.GetValue($"{variablePrefix}Texture Width");
                var textureTopValueGumCurrent = state.GetValue($"{variablePrefix}Texture Top");
                var textureHeightValueGumCurrent = state.GetValue($"{variablePrefix}Texture Height");

                var setsAny = addressValueGumCurrent != null ||
                    textureLeftValueGumCurrent != null ||
                    textureWidthValueGumCurrent != null ||
                    textureTopValueGumCurrent != null ||
                    textureHeightValueGumCurrent != null;

                if(setsAny)
                {
                    var rvf = new RecursiveVariableFinder(state);
                    var addressValueGumRecursive = rvf.GetValue<TextureAddress>($"{variablePrefix}Texture Address");
                    
                    // set them all

                    if (addressValueGumRecursive == TextureAddress.EntireTexture)
                    {
                        // null them out:
                        HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Left", glueElement, foundNos, null);
                        HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Right", glueElement, foundNos, null);
                        HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Top", glueElement, foundNos, null);
                        HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Bottom", glueElement, foundNos, null);
                    }
                    else
                    {
                        // set the values:
                        HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Left", glueElement, foundNos, IntToNullOrFloat(textureLeftValueGumCurrent));
                        HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Top", glueElement, foundNos, IntToNullOrFloat(textureTopValueGumCurrent));

                        // right and bottom depend on left/top plus width/height
                        if(textureLeftValueGumCurrent != null && textureWidthValueGumCurrent != null)
                        {
                            var combined = (int)textureLeftValueGumCurrent + (int)textureWidthValueGumCurrent;
                            HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Right", glueElement, foundNos, (float)combined);
                        }
                        else
                        {
                            HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Right", glueElement, foundNos, IntToNullOrFloat(null));
                        }

                        if(textureTopValueGumCurrent != null && textureHeightValueGumCurrent != null)
                        {
                            var combined = (int)textureTopValueGumCurrent + (int)textureHeightValueGumCurrent;
                            HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Bottom", glueElement, foundNos, (float) combined);
                        }
                        else
                        {
                            HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Bottom", glueElement, foundNos, null);
                        }
                    }

                }
                else
                {
                    // null them all
                    HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Left", glueElement, foundNos, null);
                    HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Right", glueElement, foundNos, null);
                    HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Top", glueElement, foundNos, null);
                    HandleIndividualVariableAssignment(gumElement, gumInstance, "Texture Bottom", glueElement, foundNos, null);
                }



                handled = true;
            }

            return handled;
        }

        private static object IntToNullOrFloat(object value)
        {
            if(value == null)
            {
                return null;
            }
            else
            {
                return (float)((int)value);
            }
        }

        private static void HandleIndividualVariableAssignment(ElementSave gumElement, InstanceSave gumInstance, string variableName, GlueElement glueElement, FlatRedBall.Glue.SaveClasses.NamedObjectSave foundNos, object gumValue)
        {
            var gumToGlueConverter = GumToGlueConverter.Self;
            var glueVariableName = gumToGlueConverter.ConvertVariableName(variableName, gumInstance);
            var glueValue = gumToGlueConverter
                .ConvertVariableValue(variableName, gumValue, gumInstance);
            var type = gumToGlueConverter.ConvertType(variableName, gumValue, gumInstance);

            var handled = gumToGlueConverter.ApplyGumVariableCustom(gumInstance, gumElement, glueVariableName, glueValue);

            if (!handled)
            {
                var selectedGumState = SelectedState.Self.SelectedStateSave;
                if (selectedGumState == SelectedState.Self.SelectedElement?.DefaultState)
                {

                    var glueVariable = foundNos.SetVariableValue(glueVariableName, glueValue);

                    glueVariable.Type = type;
                }
                else
                {
                    GlueState glueState;

                    glueState = GetOrCreateGlueState(glueElement, selectedGumState);

                    if (gumInstance != null)
                    {
                        // The only way Glue states can set this value is to have a tunneled variable, so we gotta look for that
                        var tunneledVariable = glueElement.CustomVariables
                            .FirstOrDefault(item => item.SourceObject == gumInstance.Name && item.SourceObjectProperty == glueVariableName);

                        if (tunneledVariable == null)
                        {
                            tunneledVariable = new FlatRedBall.Glue.SaveClasses.CustomVariable();
                            tunneledVariable.Name = gumInstance.Name + glueVariableName;
                            tunneledVariable.DefaultValue = null;
                            tunneledVariable.SourceObject = gumInstance.Name;
                            tunneledVariable.SourceObjectProperty = glueVariableName;
                            tunneledVariable.Type = type;

                            glueElement.CustomVariables.Add(tunneledVariable);
                        }

                        var stateVariable = glueState.InstructionSaves.FirstOrDefault(item => item.Member == tunneledVariable.Name);

                        if (stateVariable == null)
                        {
                            stateVariable = new FlatRedBall.Content.Instructions.InstructionSave();
                            stateVariable.Member = tunneledVariable.Name;
                            glueState.InstructionSaves.Add(stateVariable);
                        }

                        stateVariable.Value = glueValue;
                    }
                    else
                    {
                        var stateVariable = glueState.InstructionSaves.FirstOrDefault(item => item.Member == glueVariableName);

                        if (stateVariable == null)
                        {
                            stateVariable = new FlatRedBall.Content.Instructions.InstructionSave();
                            stateVariable.Member = glueVariableName;
                            glueState.InstructionSaves.Add(stateVariable);
                        }

                        stateVariable.Value = glueValue;
                    }

                }
            }
        }

        private static GlueState GetOrCreateGlueState(FlatRedBall.Glue.SaveClasses.IElement glueElement, Gum.DataTypes.Variables.StateSave selectedGumState)
        {
            GlueState glueState;
            if (SelectedState.Self.SelectedStateCategorySave != null)
            {
                var glueCategory = glueElement.StateCategoryList
                    .FirstOrDefault(item => item.Name == SelectedState.Self.SelectedStateCategorySave.Name);

                if (glueCategory == null)
                {
                    glueCategory = new FlatRedBall.Glue.SaveClasses.StateSaveCategory
                    {
                        Name = SelectedState.Self.SelectedStateCategorySave.Name
                    };
                    glueElement.StateCategoryList.Add(glueCategory);
                }

                glueState = glueCategory.GetState(selectedGumState.Name);

                if (glueState == null)
                {
                    glueState = new FlatRedBall.Glue.SaveClasses.StateSave()
                    {
                        Name = selectedGumState.Name
                    };

                    glueCategory.States.Add(glueState);
                };
            }
            else
            {
                glueState = glueElement.States.FirstOrDefault(item => item.Name == selectedGumState.Name);

                if (glueState == null)
                {
                    glueState = new FlatRedBall.Glue.SaveClasses.StateSave()
                    {
                        Name = selectedGumState.Name
                    };

                    glueElement.States.Add(glueState);
                }
            }

            return glueState;
        }

        private bool GetIfShouldApplyValue(ElementSave gumElement, InstanceSave gumInstance, string variableName)
        {
            var shouldApply = true;

            if(variableName == "Parent" && gumInstance != null)
            {
                var currentState = SelectedState.Self.SelectedStateSave;

                var newParentName = currentState.GetValueOrDefault<string>($"{gumInstance.Name}.Parent");

                InstanceSave newParent = null;
                if(!string.IsNullOrEmpty(newParentName))
                {
                    newParent = gumElement.GetInstance(newParentName);
                }

                if(newParent != null)
                {
                    var canNewParentBeParent = newParent.BaseType == "Container";

                    shouldApply = canNewParentBeParent;
                }
            }

            return shouldApply;
        }
    }
}
