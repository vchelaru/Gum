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
    public class VariableSetLogic : Singleton<VariableSetLogic>
    {
        internal void SetVariable(ElementSave gumElement, InstanceSave gumInstance, string variableName, object oldValue)
        {
            var glueProject = GluePluginState.Self.GlueProject;

            ///////////////////////Early Out///////////////////////
            if(glueProject == null)
            {
                return;
            }
            ////////////////////End Early Out/////////////////////

            var screensOrEntities = gumElement is GumScreen ?
                "Screens" :
                "Entities";

            var glueElement = glueProject.GetElement(
                $"{screensOrEntities}/{gumElement.Name}");



            if(glueElement != null)
            {
                var gumValue = gumElement.GetValueFromThisOrBase($"{gumInstance.Name}.{variableName}");

                var foundNos = glueElement.AllNamedObjects
                    .FirstOrDefault(item => item.InstanceName == gumInstance.Name); 

                if(foundNos != null)
                {
                    var gumToGlueConverter = GumToGlueConverter.Self;
                    var glueVariableName = gumToGlueConverter.ConvertVariableName(variableName);
                    var glueValue = gumToGlueConverter
                        .ConvertVariableValue(variableName, gumValue);

                    foundNos.SetVariableValue(variableName, glueValue);
                }
            }

            FileManager.XmlSerialize(glueProject, GluePluginState.Self.GlueProjectFilePath.StandardizedCaseSensitive);

        }
    }
}
