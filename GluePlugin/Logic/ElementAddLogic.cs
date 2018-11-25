using GluePlugin.Converters;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

using GlueElement = FlatRedBall.Glue.SaveClasses.IElement;
using GumElement = Gum.DataTypes.ElementSave;
using GlueScreen = FlatRedBall.Glue.SaveClasses.ScreenSave;
using GlueEntity = FlatRedBall.Glue.SaveClasses.EntitySave;


namespace GluePlugin.Logic
{
    public class ElementAddLogic : Singleton<ElementAddLogic>
    {
        internal void HandleElementAdd(GumElement gumElement)
        {
            var glueProject = GluePluginState.Self.GlueProject;

            ///////////////////////Early Out///////////////////////
            if (glueProject == null || GluePluginState.Self.InitializationState != InitializationState.Initialized)
            {
                return;
            }
            ////////////////////End Early Out/////////////////////

            var glueElement = GumToGlueConverter.Self.ConvertElement(gumElement);

            if(glueElement is GlueScreen)
            {
                glueProject.Screens.Add(glueElement as GlueScreen);
            }
            else if(glueElement is GlueEntity)
            {
                glueProject.Entities.Add(glueElement as GlueEntity);
            }


            // Create the code file before saving the glue project so that it's already there and Glue
            // doesn't complain about a missing file:
            CodeCreationLogic.Self.TrySaveCustomCodeFileFor(glueElement);

            FileManager.XmlSerialize(glueProject, GluePluginState.Self.GlueProjectFilePath.StandardizedCaseSensitive);
        }
    }
}
