using GluePlugin.Converters;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

using GumElement = Gum.DataTypes.ElementSave;
using GlueScreen = FlatRedBall.Glue.SaveClasses.ScreenSave;
using GumScreen = Gum.DataTypes.ScreenSave;
using Gum.DataTypes.Variables;
using Gum;

namespace GluePlugin.Logic
{
    class InstanceAddLogic : Singleton<InstanceAddLogic>
    {
        public void HandleInstanceAdd(GumElement container, InstanceSave instance)
        {
            var glueProject = GluePluginState.Self.GlueProject;

            ///////////////////////Early Out///////////////////////
            if (glueProject == null || GluePluginState.Self.InitializationState != InitializationState.Initialized)
            {
                return;
            }
            ////////////////////End Early Out/////////////////////

            var newNamedObjectSave = GumToGlueConverter.Self.ConvertInstance(instance);

            if (container is GumScreen)
            {
                var glueScreen = glueProject.GetScreenSave("Screens/" + container.Name);

                glueScreen.NamedObjects.Add(newNamedObjectSave);
            }
            else if(container is ComponentSave)
            {
                var glueEntity = glueProject.GetEntitySave("Entities/" + container.Name);

                glueEntity.NamedObjects.Add(newNamedObjectSave);
            }

            AdjustNewlyCreatedGumInstance(container, instance);

            // this may be a copied object, so it may already have variables. Need to loop through and apply them

            // Do we need to look at all states not just the default? Doing just default for now:
            var state = container.DefaultState;
            var variablesToHandle = state.Variables.Where(item => item.Name.StartsWith($"{instance.Name}."));

            foreach(var variable in variablesToHandle)
            {
                VariableSetLogic.Self.SetVariable(container, instance, variable.GetRootName(), null);
            }

            FileManager.XmlSerialize(glueProject, GluePluginState.Self.GlueProjectFilePath.StandardizedCaseSensitive);
        }

        private void AdjustNewlyCreatedGumInstance(GumElement container, InstanceSave instance)
        {
            var state = container.DefaultState;

            if(instance.BaseType == "Circle")
            {
                // See if there is already radius, width, and height values
                var instancePrefix = instance.Name + ".";

                var defaultState = container.DefaultState;

                var width = defaultState.GetVariableSave($"{instancePrefix}Width");
                var height = defaultState.GetVariableSave($"{instancePrefix}Height");
                var radius = defaultState.GetVariableSave($"{instancePrefix}Radius");

                if(width == null && height == null && radius == null)
                {
                    // circles in Glue default to a radius of 16, so match that
                    defaultState.Variables.Add(new VariableSave
                    {
                        Name = $"{instancePrefix}Width",
                        Type = "float",
                        Value = 32.0f,
                        SetsValue = true
                    });
                    defaultState.Variables.Add(new VariableSave
                    {
                        Name = $"{instancePrefix}Height",
                        Type = "float",
                        Value = 32.0f,
                        SetsValue = true

                    });
                    defaultState.Variables.Add(new VariableSave
                    {
                        Name = $"{instancePrefix}Radius",
                        Type = "float",
                        Value = 16.0f,
                        SetsValue = true

                    });
                }

                GumCommands.Self.WireframeCommands.Refresh();
                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);
            }
        }
    }
}
