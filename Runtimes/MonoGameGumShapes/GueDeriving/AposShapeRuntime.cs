using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving;

public abstract class AposShapeRuntime : BindableGue
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeTypes()
    {

        ElementSaveExtensions.RegisterGueInstantiation(
            "Arc",
            () => new ArcRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "ColoredCircle",
            () => new ColoredCircleRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "RoundedRectangle",
            () => new RoundedRectangleRuntime());

        StandardElementsManager.Self.CustomGetDefaultState += HandleCustomGetDefaultState;

    }

    private static StateSave HandleCustomGetDefaultState(string arg)
    {
        switch (arg)
        {
            case "Arc":
                return StandardElementsManager.GetArcState();
            case "ColoredCircle":
                return StandardElementsManager.GetColoredCircleState();
            case "RoundedRectangle":
                return StandardElementsManager.GetRoundedRectangleState();

            // temp?
            default:
                return StandardElementsManager.Self.DefaultStates["Container"];
        }
        return null;
    }

    protected abstract AposShapeBase ContainedRenderable { get; }

    #region Solid colors

    public new int Alpha
    {
        get => ContainedRenderable.Alpha;
        set => ContainedRenderable.Alpha = value;
    }

    public int Blue
    {
        get => ContainedRenderable.Blue;
        set => ContainedRenderable.Blue = value;
    }

    public int Green
    {
        get => ContainedRenderable.Green;
        set => ContainedRenderable.Green = value;
    }

    public int Red
    {
        get => ContainedRenderable.Red;
        set => ContainedRenderable.Red = value;
    }

    #endregion

    #region Gradient Colors

    public int Blue1
    {
        get => ContainedRenderable.Blue1;
        set => ContainedRenderable.Blue1 = value;
    }

    public int Green1
    {
        get => ContainedRenderable.Green1;
        set => ContainedRenderable.Green1 = value;
    }

    public int Red1
    {
        get => ContainedRenderable.Red1;
        set => ContainedRenderable.Red1 = value;
    }


    public int Blue2
    {
        get => ContainedRenderable.Blue2;
        set => ContainedRenderable.Blue2 = value;
    }

    public int Green2
    {
        get => ContainedRenderable.Green2;
        set => ContainedRenderable.Green2 = value;
    }

    public int Red2
    {
        get => ContainedRenderable.Red2;
        set => ContainedRenderable.Red2 = value;
    }

    public float GradientX1
    {
        get => ContainedRenderable.GradientX1;
        set => ContainedRenderable.GradientX1 = value;
    }
    public GeneralUnitType GradientX1Units
    {
        get => ContainedRenderable.GradientX1Units;
        set => ContainedRenderable.GradientX1Units = value;
    }
    public float GradientY1
    {
        get => ContainedRenderable.GradientY1;
        set => ContainedRenderable.GradientY1 = value;
    }
    public GeneralUnitType GradientY1Units
    {
        get => ContainedRenderable.GradientY1Units;
        set => ContainedRenderable.GradientY1Units = value;
    }

    public float GradientX2
    {
        get => ContainedRenderable.GradientX2;
        set => ContainedRenderable.GradientX2 = value;
    }
    public GeneralUnitType GradientX2Units
    {
        get => ContainedRenderable.GradientX2Units;
        set => ContainedRenderable.GradientX2Units = value;
    }
    public float GradientY2
    {
        get => ContainedRenderable.GradientY2;
        set => ContainedRenderable.GradientY2 = value;
    }
    public GeneralUnitType GradientY2Units
    {
        get => ContainedRenderable.GradientY2Units;
        set => ContainedRenderable.GradientY2Units = value;
    }

    public bool UseGradient
    {
        get => ContainedRenderable.UseGradient;
        set => ContainedRenderable.UseGradient = value;
    }

    public GradientType GradientType
    {
        get => ContainedRenderable.GradientType;
        set => ContainedRenderable.GradientType = value;
    }

    public float GradientInnerRadius
    {
        get => ContainedRenderable.GradientInnerRadius;
        set => ContainedRenderable.GradientInnerRadius = value;
    }

    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => ContainedRenderable.GradientInnerRadiusUnits;
        set => ContainedRenderable.GradientInnerRadiusUnits = value;
    }

    public float GradientOuterRadius
    {
        get => ContainedRenderable.GradientOuterRadius;
        set => ContainedRenderable.GradientOuterRadius = value;
    }

    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => ContainedRenderable.GradientOuterRadiusUnits;
        set => ContainedRenderable.GradientOuterRadiusUnits = value;
    }

    #endregion

    #region Dropshadow

    public int DropshadowAlpha
    {
        get => ContainedRenderable.DropshadowAlpha;
        set => ContainedRenderable.DropshadowAlpha = value;
    }

    public int DropshadowBlue
    {
        get => ContainedRenderable.DropshadowBlue;
        set => ContainedRenderable.DropshadowBlue = value;
    }

    public int DropshadowGreen
    {
        get => ContainedRenderable.DropshadowGreen;
        set => ContainedRenderable.DropshadowGreen = value;
    }

    public int DropshadowRed
    {
        get => ContainedRenderable.DropshadowRed;
        set => ContainedRenderable.DropshadowRed = value;
    }


    public bool HasDropshadow
    {
        get => ContainedRenderable.HasDropshadow;
        set => ContainedRenderable.HasDropshadow = value;
    }

    public float DropshadowOffsetX
    {
        get => ContainedRenderable.DropshadowOffsetX;
        set => ContainedRenderable.DropshadowOffsetX = value;
    }
    public float DropshadowOffsetY
    {
        get => ContainedRenderable.DropshadowOffsetY;
        set => ContainedRenderable.DropshadowOffsetY = value;
    }

    public float DropshadowBlurX
    {
        get => ContainedRenderable.DropshadowBlurX;
        set => ContainedRenderable.DropshadowBlurX = value;
    }
    public float DropshadowBlurY
    {
        get => ContainedRenderable.DropshadowBlurY;
        set => ContainedRenderable.DropshadowBlurY = value;
    }

    #endregion

    #region Filled/Stroke

    public bool IsFilled
    {
        get => ContainedRenderable.IsFilled;
        set => ContainedRenderable.IsFilled = value;
    }

    public float StrokeWidth
    {
        get;
        set;
    }

    public DimensionUnitType StrokeWidthUnits
    {
        get;
        set;
    }

    #endregion


    public Color Color
    {
        get => ContainedRenderable.Color;
        set => ContainedRenderable.Color = value;
    }

    public override void PreRender()
    {
        if (this.EffectiveManagers != null)
        {
            var camera = this.EffectiveManagers.Renderer.Camera;
            var strokeWidth = StrokeWidth;

            switch (StrokeWidthUnits)
            {
                case DimensionUnitType.Absolute:
                    // do nothing
                    break;
                case DimensionUnitType.ScreenPixel:
                    strokeWidth /= camera.Zoom;
                    break;
            }

            ContainedRenderable.StrokeWidth = strokeWidth;
        }
        base.PreRender();
    }
}
