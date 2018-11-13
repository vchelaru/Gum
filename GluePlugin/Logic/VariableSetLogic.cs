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

                        if(gumInstance != null)
                        {
                            // The only way Glue states can set this value is to have a tunneled variable, so we gotta look for that
                            var tunneledVariable = glueElement.CustomVariables
                                .FirstOrDefault(item => item.SourceObject == gumInstance.Name && item.SourceObjectProperty == glueVariableName);

                            if(tunneledVariable == null)
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

                            if(stateVariable == null)
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

            if(save)
            {
                FileManager.XmlSerialize(glueProject, GluePluginState.Self.GlueProjectFilePath.StandardizedCaseSensitive);
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
