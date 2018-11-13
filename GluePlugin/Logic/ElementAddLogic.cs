using GluePlugin.Converters;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GluePlugin.Logic
{
    public class ElementAddLogic : Singleton<ElementAddLogic>
    {
        internal void HandleElementAdd(ElementSave gumElement)
        {
            var glueProject = GluePluginState.Self.GlueProject;

            ///////////////////////Early Out///////////////////////
            if (glueProject == null || GluePluginState.Self.InitializationState != InitializationState.Initialized)
            {
                return;
            }
            ////////////////////End Early Out/////////////////////

            var glueElement = GumToGlueConverter.Self.ConvertElement(gumElement);

            if(glueElement is FlatRedBall.Glue.SaveClasses.ScreenSave)
            {
                glueProject.Screens.Add(glueElement as FlatRedBall.Glue.SaveClasses.ScreenSave);
            }
            else if(glueElement is FlatRedBall.Glue.SaveClasses.EntitySave)
            {
                glueProject.Entities.Add(glueElement as FlatRedBall.Glue.SaveClasses.EntitySave);
            }

            // Create the code file before saving the glue project so that it's already there and Glue
            // doesn't complain about a missing file:
            CodeCreationLogic.Self.TrySaveCustomCodeFileFor(glueElement);

            FileManager.XmlSerialize(glueProject, GluePluginState.Self.GlueProjectFilePath.StandardizedCaseSensitive);
        }
    }
}
