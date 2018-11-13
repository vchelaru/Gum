using GluePlugin.Converters;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using GlueScreen = FlatRedBall.Glue.SaveClasses.ScreenSave;
using GumScreen = Gum.DataTypes.ScreenSave;

namespace GluePlugin.Logic
{
    class InstanceAddLogic : Singleton<InstanceAddLogic>
    {
        public void HandleInstanceAdd(ElementSave container, InstanceSave instance)
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
    }
}
