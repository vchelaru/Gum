using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GumScreen = Gum.DataTypes.ScreenSave;

namespace GluePlugin.Logic
{
    public class GluePluginObjectFinder : Singleton<GluePluginObjectFinder>
    {
        public IElement GetGlueElementFrom(ElementSave gumElement)
        {
            var glueProject = GluePluginState.Self.GlueProject;

            var screensOrEntities = gumElement is GumScreen ?
                "Screens" :
                "Entities";

            var glueElement = glueProject.GetElement(
                $"{screensOrEntities}/{gumElement.Name}");

            return glueElement;
        }

        public NamedObjectSave GetNamedObjectSave(InstanceSave gumInstance, IElement glueElement)
        {
            var name = gumInstance.Name;

            return glueElement.AllNamedObjects.FirstOrDefault(item => item.InstanceName == name);
        }
    }
}
