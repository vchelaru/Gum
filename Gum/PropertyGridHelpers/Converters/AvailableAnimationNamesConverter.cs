using Gum.Content.AnimationChain;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.PropertyGridHelpers.Converters
{
    internal class AvailableAnimationNamesConverter : TypeConverter
    {
        ElementSave container;
        public AvailableAnimationNamesConverter(ElementSave container)
        {
            this.container = container;
        }
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var stateSave = SelectedState.Self.SelectedStateSave;
            var instance = SelectedState.Self.SelectedInstance;
            List<string> values = GetAvailableValues(container, instance, stateSave);

            return new StandardValuesCollection(values);
        }

        public static List<string> GetAvailableValues(ElementSave container, InstanceSave instance, StateSave stateSave)
        {
            List<string> toReturn = new List<string>();

            var sourceFileVariableName = "SourceFile";

            if(instance != null)
            {
                sourceFileVariableName = instance.Name + "." + sourceFileVariableName;
            }

            var rfv = new RecursiveVariableFinder(stateSave);

            var sourceFile = rfv.GetValue<string>(sourceFileVariableName);

            if(sourceFile?.EndsWith(".achx") == true)
            {
                if(FileManager.IsRelative(sourceFile))
                {
                    sourceFile = GumState.Self.ProjectState.ProjectDirectory + sourceFile;
                }

                if(System.IO.File.Exists(sourceFile))
                {
                    // cache?
                    var animationChainListSave = AnimationChainListSave.FromFile(sourceFile);

                    foreach(var animation in animationChainListSave.AnimationChains)
                    {
                        toReturn.Add(animation.Name);
                    }
                }
            }

            
            return toReturn;
        }
    }
}
