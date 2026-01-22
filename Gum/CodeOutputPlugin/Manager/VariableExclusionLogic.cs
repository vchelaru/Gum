using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin.Manager
{
    internal static class VariableExclusionLogic
    {
        internal static bool GetIfVariableIsExcluded(VariableSave variable, RecursiveVariableFinder rvf)
        {
            InstanceSave instance = rvf.InstanceSave;

            if(instance != null)
            {
                var visualApi = CodeGenerator.GetVisualApiForInstance(instance, rvf.ElementStack.Last().Element);

                var variableName = variable.Name.Replace(" ", "");

                if(visualApi == VisualApi.XamarinForms)
                {
                    // remove things that aren't supported in Xamarin Forms
                    switch(variableName)
                    {
                        case "Alpha":
                        case "Blend":
                        case "ClipsChildren":
                        case "ContainedType":
                        case "FlipHorizontal":
                        case "Guide":
                        case "Rotation":
                        case "TextureAddress":
                        case "WrapsChildren":
                        case "SourceFile":
                            return true;
                    }


                    if(!IsStackLayout(instance))
                    {
                        switch(variableName)
                        {
                            case "ChildrenLayout":
                                return true;
                        }
                    }
                }
            }


            return false;
        }

        static bool IsStackLayout(InstanceSave instance) => instance.BaseType?.EndsWith("/StackLayout") == true;
        static bool IsAbsoluteLayout(InstanceSave instance) => instance.BaseType?.EndsWith("/AbsoluteLayout") == true;
        static bool IsSkiaCanvas(InstanceSave instance) => instance.BaseType?.EndsWith("/SkiaGumCanvasView") == true;
    }
}
