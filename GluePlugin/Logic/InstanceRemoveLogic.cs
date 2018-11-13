using FlatRedBall.IO;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GluePlugin.Logic
{
    public class InstanceRemoveLogic : Singleton<InstanceRemoveLogic>
    {
        internal void HandleInstanceDelete(ElementSave gumElement, InstanceSave gumInstance)
        {
            var glueProject = GluePluginState.Self.GlueProject;

            ///////////////////////Early Out///////////////////////
            if (glueProject == null || GluePluginState.Self.InitializationState != InitializationState.Initialized)
            {
                return;
            }
            ////////////////////End Early Out/////////////////////

            var glueElement = GluePluginObjectFinder.Self.GetGlueElementFrom(gumElement);
            var glueNos = GluePluginObjectFinder.Self.GetNamedObjectSave(gumInstance, glueElement);

            if(glueElement.NamedObjects.Contains(glueNos))
            {
                glueElement.NamedObjects.Remove(glueNos);
            }
            else
            {
                var listContainingNos = glueElement.NamedObjects
                    .FirstOrDefault(item => item.ContainedObjects.Contains(glueNos));

                if(listContainingNos != null)
                {
                    listContainingNos.ContainedObjects.Remove(glueNos);
                }
            }

            FileManager.XmlSerialize(glueProject, GluePluginState.Self.GlueProjectFilePath.StandardizedCaseSensitive);
        }
    }
}
