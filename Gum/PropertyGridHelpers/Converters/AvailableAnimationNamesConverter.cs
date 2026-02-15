using Gum.Content.AnimationChain;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Services;
using Gum.ToolStates;
using System.Collections.Generic;
using System.ComponentModel;
using ToolsUtilities;

namespace Gum.PropertyGridHelpers.Converters
{
    internal class AvailableAnimationNamesConverter : TypeConverter
    {
        private readonly ISelectedState _selectedState;
        
        ElementSave container;
        
        public AvailableAnimationNamesConverter(ElementSave container)
        {
            this.container = container;
            _selectedState = Locator.GetRequiredService<ISelectedState>();
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
            var stateSave = _selectedState.SelectedStateSave;
            var instance = _selectedState.SelectedInstance;
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
                    var projectState = Locator.GetRequiredService<ProjectState>();
                    sourceFile = projectState.ProjectDirectory + sourceFile;
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
