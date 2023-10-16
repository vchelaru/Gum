using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;

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

            if (rootName == "Texture Top" || rootName == "Texture Left")
            {
                var addressMode = rvf.GetValue<TextureAddress>($"{prefix}Texture Address");

                shouldExclude = addressMode == TextureAddress.EntireTexture;
            }

            if (rootName == "Texture Width" || rootName == "Texture Height")
            {
                var addressMode = rvf.GetValue<TextureAddress>($"{prefix}Texture Address");

                shouldExclude = addressMode == TextureAddress.EntireTexture ||
                    addressMode == TextureAddress.DimensionsBased;
            }

            if (rootName == "Texture Width Scale" || rootName == "Texture Height Scale")
            {
                var addressMode = rvf.GetValue<TextureAddress>($"{prefix}Texture Address");

                shouldExclude = addressMode == TextureAddress.EntireTexture ||
                    addressMode == TextureAddress.Custom;
            }

            return shouldExclude;
        }
    }
}
