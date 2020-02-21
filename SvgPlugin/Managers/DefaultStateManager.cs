using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaPlugin.Managers
{
    public static class DefaultStateManager
    {
        static StateSave svgState;
        static StateSave filledCircleState;


        public static StateSave GetSvgState()
        {
            if(svgState == null)
            {
                svgState = new StateSave();
                svgState.Name = "Default";
                svgState.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible" });
                StandardElementsManager.AddPositioningVariables(svgState);
                StandardElementsManager.AddDimensionsVariables(svgState, 100, 100, 
                    Gum.Managers.StandardElementsManager.DimensionVariableAction.AllowFileOptions);

                foreach(var variableSave in svgState.Variables.Where(item => item.Type == typeof(DimensionUnitType).Name))
                {
                    variableSave.Value = DimensionUnitType.Absolute;
                    variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.PercentageOfSourceFile);
                    //variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.MaintainFileAspectRatio);

                }
                svgState.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "SourceFile", IsFile = true });

                svgState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation" });

            }
            return svgState;
        }

        public static StateSave GetColoredCircleState()
        {
            if(filledCircleState == null)
            {
                filledCircleState = new StateSave();
                filledCircleState.Name = "Default";
                filledCircleState.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible" });
                StandardElementsManager.AddPositioningVariables(filledCircleState);
                StandardElementsManager.AddDimensionsVariables(filledCircleState, 64, 64, 
                    StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
                StandardElementsManager.AddColorVariables(filledCircleState);
            }

            return filledCircleState;
        }
    }
}
