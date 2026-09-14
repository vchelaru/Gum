using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers.Converters;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Tool-only setup of the standard elements' default states: which displayer each variable uses (as
/// neutral keys), the type converters behind drop-downs, and plugin modifications.
/// </summary>
public class StandardElementsManagerGumTool : IStandardElementsManagerGumTool
{
    private readonly IPluginManager _pluginManager;
    private readonly ISelectedState _selectedState;

    public StandardElementsManagerGumTool(IPluginManager pluginManager, ISelectedState selectedState)
    {
        _pluginManager = pluginManager;
        _selectedState = selectedState;
    }

    public void Initialize()
    {
        var defaultStates =
            StandardElementsManager.Self.DefaultStates;
        defaultStates["Container"].Variables.First(item => item.Name == "ContainedType").CustomTypeConverter =
            new AvailableContainedTypeConverter();

        defaultStates["Component"].Variables
            .Add(new VariableSave { SetsValue = true, Type = "State", Value = null, Name = "State", CustomTypeConverter = new AvailableStatesConverter(null, _selectedState) });

        foreach (var state in StandardElementsManager.Self.DefaultStates.Values)
        {
            SetPreferredDisplayers(state);
        }




        RefreshStateVariablesThroughPlugins();

    }

    public void SetPreferredDisplayers(StateSave state)
    {
        foreach (var variable in state.Variables)
        {
            if (variable.Type == "float" && 
                (variable.Name == "Rotation"
                 || variable.Name == "StartAngle"
                 || variable.Name == "SweepAngle"))
            {
                MakeDegreesAngle(variable);
            }
            else if (variable.Type == nameof(TextOverflowVerticalMode))
            {
                variable.PreferredDisplayer = typeof(GumDisplayers.TextOverflowVerticalMode);
            }
            else if (variable.Type == nameof(TextOverflowHorizontalMode))
            {
                variable.PreferredDisplayer = typeof(GumDisplayers.TextOverflowHorizontalMode);
            }
            else if (variable.Type == nameof(ChildrenLayout))
            {
                variable.PreferredDisplayer = typeof(GumDisplayers.ChildrenLayout);
            }
            else if (variable.Type == nameof(DimensionUnitType))
            {
                if (variable.Name == "WidthUnits")
                {
                    variable.PreferredDisplayer = typeof(GumDisplayers.WidthUnits);
                }
                else if (variable.Name == "HeightUnits")
                {
                    variable.PreferredDisplayer = typeof(GumDisplayers.HeightUnits);
                }
            }
            else if (variable.Type == nameof(PositionUnitType))
            {
                if (variable.Name == "XUnits" || variable.Name == "GradientX1Units" || variable.Name == "GradientX2Units")
                {
                    variable.PreferredDisplayer = typeof(GumDisplayers.XUnits);
                }
                else if (variable.Name == "YUnits" || variable.Name == "GradientY1Units" || variable.Name == "GradientY2Units")
                {
                    variable.PreferredDisplayer = typeof(GumDisplayers.YUnits);
                }
            }
            else if (variable.Type == nameof(VerticalAlignment))
            {
                if (variable.Name == "VerticalAlignment")
                {
                    variable.PreferredDisplayer = typeof(GumDisplayers.TextVerticalAlignment);
                }
                else if (variable.Name == "YOrigin")
                {
                    variable.PreferredDisplayer = typeof(GumDisplayers.YOrigin);
                }
            }
            else if (variable.Type == nameof(HorizontalAlignment))
            {
                if (variable.Name == "HorizontalAlignment")
                {
                    variable.PreferredDisplayer = typeof(GumDisplayers.TextHorizontalAlignment);
                }
                else if (variable.Name == "XOrigin")
                {
                    variable.PreferredDisplayer = typeof(GumDisplayers.XOrigin);
                }
            }
            else if (variable.Type == "string" && variable.Name == "Parent")
            {
                variable.CustomTypeConverter = new AvailableParentsTypeConverter(_selectedState);
                variable.PropertiesToSetOnDisplayer["IsEditable"] = true;
            }
            else if (variable.Type == "string" && variable.Name == "RenderTargetTextureSource")
            {
                variable.CustomTypeConverter =
                    new AvailableRenderTargetContainersConverter(_selectedState);
            }
            else if (variable.Type == "State" && variable.Name == "State")
            {
                variable.Category = "States and Visibility";
                variable.CustomTypeConverter = new AvailableStatesConverter(null, _selectedState);
            }
            else if (variable.Name == "Red" ||
                variable.Name == "Green" ||
                variable.Name == "Blue" ||
                variable.Name == "Alpha" ||
                variable.Name == "Red1" ||
                variable.Name == "Green1" ||
                variable.Name == "Blue1" ||
                variable.Name == "Alpha1" ||
                variable.Name == "Red2" ||
                variable.Name == "Green2" ||
                variable.Name == "Blue2" ||
                variable.Name == "Alpha2" ||
                variable.Name == "FillAlpha" ||
                variable.Name == "StrokeAlpha" ||
                variable.Name == "DropshadowAlpha"
                )
            {
                variable.PropertiesToSetOnDisplayer["MinValue"] = 0.0;
                variable.PropertiesToSetOnDisplayer["MaxValue"] = 255.0;
                variable.PreferredDisplayer = typeof(StandardDisplayers.Slider);
            }
            else if (variable.Name == "StrokeWidth" || variable.Name == "DropshadowBlur")
            {
                // Neither has a meaningful negative value but neither has a natural maximum either, so
                // they stay plain numeric fields (not sliders) with only a floor of 0. DropshadowBlur is
                // a radius; the runtime already clamps it, and this stops the tool from storing negatives.
                variable.PropertiesToSetOnDisplayer["MinValue"] = 0.0;
            }
        }
        foreach (var variableList in state.VariableLists)
        {
            if (variableList.Name == "VariableReferences")
            {
                variableList.PreferredDisplayer = typeof(StandardDisplayers.StringList);
            }
        }
    }

    public void FixCustomTypeConverters(GumProjectSave project)
    {
        foreach(var screen in project.Screens)
        {
            FixCustomTypeConverters(screen);
        }
        foreach(var component in project.Components)
        {
            FixCustomTypeConverters(component);
        }
    }

    public void FixCustomTypeConverters(ElementSave elementSave)
    {
        foreach (var stateSaveCategory in elementSave.Categories)
        {
            VariableSave? foundVariable = elementSave.DefaultState.Variables.FirstOrDefault(item => item.Name == stateSaveCategory.Name + "State");

            if (foundVariable != null)
            {
                foundVariable.CustomTypeConverter = new AvailableStatesConverter(stateSaveCategory.Name, _selectedState);
            }
        }
    }

    public static void MakeDegreesAngle(VariableSave variableSave)
    {
        variableSave.PreferredDisplayer = typeof(StandardDisplayers.AngleSelector);
        variableSave.PropertiesToSetOnDisplayer["TypeToPushToInstance"] = AngleType.Degrees;
    }

    public void RefreshStateVariablesThroughPlugins()
    {
        foreach (var kvp in StandardElementsManager.Self.DefaultStates)
        {
            _pluginManager.ModifyDefaultStandardState(kvp.Key, kvp.Value);
        }
    }



}
