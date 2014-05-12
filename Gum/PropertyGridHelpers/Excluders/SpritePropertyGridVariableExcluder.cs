using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            if (rootName == "Texture Top" || rootName == "Texture Left")
            {
                var addressMode = rvf.GetValue<TextureAddress>("Texture Address");

                shouldExclude = addressMode == TextureAddress.EntireTexture;
            }

            if (rootName == "Texture Width" || rootName == "Texture Height")
            {
                var addressMode = rvf.GetValue<TextureAddress>("Texture Address");

                shouldExclude = addressMode == TextureAddress.EntireTexture ||
                    addressMode == TextureAddress.DimensionsBased;
            }

            if (rootName == "Texture Width Scale" || rootName == "Texture Height Scale")
            {
                var addressMode = rvf.GetValue<TextureAddress>("Texture Address");

                shouldExclude = addressMode == TextureAddress.EntireTexture ||
                    addressMode == TextureAddress.Custom;
            }

            return shouldExclude;
        }
    }
}
