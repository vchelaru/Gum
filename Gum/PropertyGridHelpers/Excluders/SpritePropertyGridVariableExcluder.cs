using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using System.Linq;

namespace Gum.PropertyGridHelpers.Excluders
{
    [Export(typeof(PluginBase))]
    public class SpritePropertyGridVariableExcluder : InternalPlugin
    {

        public override void StartUp()
        {
            this.VariableExcluded += HandleVariableExcluded;
        }


        private bool HandleVariableExcluded(DataTypes.Variables.VariableSave variable, DataTypes.RecursiveVariableFinder rvf)
        {
            string rootName = variable.GetRootName();

            bool shouldExclude = false;

            string nameOfInstanceOwningVariable = variable.SourceObject;

            // Victor Chelaru November 26, 2016
            // We need to consider the owner of
            // the variable (the SourceObject) because
            // if this is an exposed variable, we don't
            // want to check the Texture Address of the container
            // if the variable is exposing a child.
            string prefix = "";
            if(!string.IsNullOrEmpty(nameOfInstanceOwningVariable))
            {
                prefix = nameOfInstanceOwningVariable + ".";
            }
            
            if(string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty( rvf.ElementStack.Last().InstanceName ) && rvf.ContainerType != DataTypes.RecursiveVariableFinder.VariableContainerType.InstanceSave)
            {
                prefix = rvf.ElementStack.Last().InstanceName + ".";

            }

            if (rootName == "TextureTop" || rootName == "TextureLeft")
            {
                var addressMode = rvf.GetValue<TextureAddress>($"{prefix}TextureAddress");

                shouldExclude = addressMode == TextureAddress.EntireTexture;
            }

            if (rootName == "TextureWidth" || rootName == "TextureHeight")
            {
                var addressMode = rvf.GetValue<TextureAddress>($"{prefix}TextureAddress");

                shouldExclude = addressMode == TextureAddress.EntireTexture ||
                    addressMode == TextureAddress.DimensionsBased;
            }

            if (rootName == "TextureWidthScale" || rootName == "TextureHeightScale")
            {
                var addressMode = rvf.GetValue<TextureAddress>($"{prefix}TextureAddress");

                shouldExclude = addressMode == TextureAddress.EntireTexture ||
                    addressMode == TextureAddress.Custom;
            }

            return shouldExclude;
        }
    }
}
