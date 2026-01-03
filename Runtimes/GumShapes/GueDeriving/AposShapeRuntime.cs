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
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Base class for all shapes, providng common properties like color, gradient, and dropshadow.
/// </summary>
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

        CustomSetPropertyOnRenderable.AdditionalPropertyOnRenderable += 
            MonoGameGumShapes.CustomSetPropertyOnRenderable.SetPropertyOnRenderableFunc;

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
    }

    protected abstract RenderableShapeBase ContainedRenderable { get; }

    #region Solid colors

    /// <summary>
    /// Gets or sets the alpha (opacity) value for the contained renderable object. 
    /// The value range is 0-255. This value
    /// is ignored if a gradient is being used.
    /// </summary>
    public new int Alpha
    {
        get => ContainedRenderable.Alpha;
        set => ContainedRenderable.Alpha = value;
    }

    /// <summary>
    /// Gets or sets the blue component of the color. 
    /// The value range is 0-255. This value is ignored if a gradient is being used.
    /// </summary>
    public int Blue
    {
        get => ContainedRenderable.Blue;
        set => ContainedRenderable.Blue = value;
    }

    /// <summary>
    /// Gets or sets the green component value of the color.
    /// The value range is 0-255. This value is ignored if a gradient is being used.
    /// </summary>
    public int Green
    {
        get => ContainedRenderable.Green;
        set => ContainedRenderable.Green = value;
    }

    /// <summary>
    /// Gets or sets the red component value of the color.
    /// The value range is 0-255. This value is ignored if a gradient is being used.
    /// </summary>
    public int Red
    {
        get => ContainedRenderable.Red;
        set => ContainedRenderable.Red = value;
    }

    /// <summary>
    /// Gets or sets the color used to render the contained object.
    /// This value is ignored if a gradient is being used.
    /// </summary>
    public Color Color
    {
        get => ContainedRenderable.Color;
        set => ContainedRenderable.Color = value;
    }

    #endregion

    #region Gradient Colors

    /// <summary>
    /// Gets or sets the blue component value for the first gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Blue1
    {
        get => ContainedRenderable.Blue1;
        set => ContainedRenderable.Blue1 = value;
    }

    /// <summary>
    /// Gets or sets the green component value for the first gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Green1
    {
        get => ContainedRenderable.Green1;
        set => ContainedRenderable.Green1 = value;
    }

    /// <summary>
    /// Gets or sets the red component value for the first gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Red1
    {
        get => ContainedRenderable.Red1;
        set => ContainedRenderable.Red1 = value;
    }

    /// <summary>
    /// Gets or sets the alpha value used for rendering transparency for the first gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Alpha1
    {
        get => ContainedRenderable.Alpha1;
        set => ContainedRenderable.Alpha1 = value;
    }

    /// <summary>
    /// Gets or sets the first gradient color. This value is only used if a gradient is being used.
    /// </summary>
    public Color Color1
    {
        get => new Color(Red1, Green1, Blue1, Alpha1);
        set
        {
            Red1 = value.R;
            Green1 = value.G;
            Blue1 = value.B;
            Alpha1 = value.A;
        }
    }

    /// <summary>
    /// Gets or sets the blue color component for the second gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Blue2
    {
        get => ContainedRenderable.Blue2;
        set => ContainedRenderable.Blue2 = value;
    }


    /// <summary>
    /// Gets or sets the green component value for the second gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Green2
    {
        get => ContainedRenderable.Green2;
        set => ContainedRenderable.Green2 = value;
    }

    /// <summary>
    /// Gets or sets the red component value for the second gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Red2
    {
        get => ContainedRenderable.Red2;
        set => ContainedRenderable.Red2 = value;
    }

    /// <summary>
    /// Gets or sets the alpha (opacity) value for the second gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Alpha2
    {
        get => ContainedRenderable.Alpha2;
        set => ContainedRenderable.Alpha2 = value;
    }

    /// <summary>
    /// Gets or sets the second gradient color. This value is only used if a gradient is being used.
    /// </summary>
    public Color Color2
    {
        get => new Color(Red2, Green2, Blue2, Alpha2);
        set
        {
            Red2 = value.R;
            Green2 = value.G;
            Blue2 = value.B;
            Alpha2 = value.A;
        }
    }

    /// <summary>
    /// The X coordinate of the start of the gradient. The interpretation of this value depends on the setting of GradientX1Units.
    /// </summary>
    public float GradientX1
    {
        get => ContainedRenderable.GradientX1;
        set => ContainedRenderable.GradientX1 = value;
    }

    /// <summary>
    /// Gets or sets the unit type used to interpret the X1 coordinate of the gradient.
    /// </summary>
    public GeneralUnitType GradientX1Units
    {
        get => ContainedRenderable.GradientX1Units;
        set => ContainedRenderable.GradientX1Units = value;
    }

    /// <summary>
    /// The Y coordinate of the start of the gradient. The interpretation of this value depends on the setting of GradientY1Units.
    /// </summary>
    public float GradientY1
    {
        get => ContainedRenderable.GradientY1;
        set => ContainedRenderable.GradientY1 = value;
    }

    /// <summary>
    /// Gets or sets the unit type used to interpret the Y1 coordinate of the gradient.
    /// </summary>
    public GeneralUnitType GradientY1Units
    {
        get => ContainedRenderable.GradientY1Units;
        set => ContainedRenderable.GradientY1Units = value;
    }

    /// <summary>
    /// The X coordinate of the end of the gradient. The interpretation of this value depends on the setting of GradientX2Units. This is only used for Linear gradients.
    /// </summary>
    public float GradientX2
    {
        get => ContainedRenderable.GradientX2;
        set => ContainedRenderable.GradientX2 = value;
    }

    /// <summary>
    /// Gets or sets the coordinate system used to interpret the X2 value of the gradient vector. This is only used for Linear gradients.
    /// </summary>
    public GeneralUnitType GradientX2Units
    {
        get => ContainedRenderable.GradientX2Units;
        set => ContainedRenderable.GradientX2Units = value;
    }

    /// <summary>
    /// The Y coordinate of the end of the gradient. The interpretation of this value depends on the setting of GradientY2Units. This is only used for Linear gradients.
    /// </summary>
    public float GradientY2
    {
        get => ContainedRenderable.GradientY2;
        set => ContainedRenderable.GradientY2 = value;
    }

    /// <summary>
    /// Gets or sets the coordinate system used to interpret the Y2 value of the gradient vector. This is only used for Linear gradients.
    /// </summary>
    public GeneralUnitType GradientY2Units
    {
        get => ContainedRenderable.GradientY2Units;
        set => ContainedRenderable.GradientY2Units = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether a gradient fill is applied when rendering the contained object.
    /// If false, the solid color properties (Red, Green, Blue, Alpha) are used instead.
    /// If true, the gradient color properties (Color1, Color2, etc.) are used.
    /// </summary>
    public bool UseGradient
    {
        get => ContainedRenderable.UseGradient;
        set => ContainedRenderable.UseGradient = value;
    }

    /// <summary>
    /// Gets or sets the type of gradient used for rendering. Default is Linear.
    /// </summary>
    public GradientType GradientType
    {
        get => ContainedRenderable.GradientType;
        set => ContainedRenderable.GradientType = value;
    }

    /// <summary>
    /// [Planned for future release] The inner radius before the gradient starts to interpolate colors when using Radial gradient.
    /// </summary>
    public float GradientInnerRadius
    {
        get => ContainedRenderable.GradientInnerRadius;
        set => ContainedRenderable.GradientInnerRadius = value;
    }

    /// <summary>
    /// [Planned for future release] The unit type used to interpret the inner radius when using a Radial gradient.
    /// </summary>
    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => ContainedRenderable.GradientInnerRadiusUnits;
        set => ContainedRenderable.GradientInnerRadiusUnits = value;
    }

    /// <summary>
    /// Gets or sets the outer radius at which the gradient has fully blended to Color2. This is only applicable when using a Radial gradient.
    /// </summary>
    public float GradientOuterRadius
    {
        get => ContainedRenderable.GradientOuterRadius;
        set => ContainedRenderable.GradientOuterRadius = value;
    }

    /// <summary>
    /// Gets or sets the unit type used to interpret the outer radius of the gradient. This is only applicable when using a Radial gradient.
    /// </summary>
    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => ContainedRenderable.GradientOuterRadiusUnits;
        set => ContainedRenderable.GradientOuterRadiusUnits = value;
    }

    #endregion

    #region Dropshadow

    /// <summary>
    /// Gets or sets the alpha (opacity) value of the drop shadow effect.
    /// The value range is 0-255.
    /// </summary>
    public int DropshadowAlpha
    {
        get => ContainedRenderable.DropshadowAlpha;
        set => ContainedRenderable.DropshadowAlpha = value;
    }

    /// <summary>
    /// Gets or sets the blue component of the drop shadow color.
    /// The value range is 0-255.
    /// </summary>
    public int DropshadowBlue
    {
        get => ContainedRenderable.DropshadowBlue;
        set => ContainedRenderable.DropshadowBlue = value;
    }

    /// <summary>
    /// Gets or sets the green component of the drop shadow color.
    /// The value range is 0-255.
    /// </summary>
    public int DropshadowGreen
    {
        get => ContainedRenderable.DropshadowGreen;
        set => ContainedRenderable.DropshadowGreen = value;
    }

    /// <summary>
    /// Gets or sets the red component of the drop shadow color.
    /// The value range is 0-255.
    /// </summary>
    public int DropshadowRed
    {
        get => ContainedRenderable.DropshadowRed;
        set => ContainedRenderable.DropshadowRed = value;
    }

    /// <summary>
    /// Gets or sets the color used for the drop shadow effect.
    /// </summary>
    public Color DropshadowColor
    {
        get => new Color(DropshadowRed, DropshadowGreen, DropshadowBlue, DropshadowAlpha);
        set
        {
            DropshadowRed = value.R;
            DropshadowGreen = value.G;
            DropshadowBlue = value.B;
            DropshadowAlpha = value.A;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether a drop shadow is visibile.
    /// </summary>
    public bool HasDropshadow
    {
        get => ContainedRenderable.HasDropshadow;
        set => ContainedRenderable.HasDropshadow = value;
    }

    /// <summary>
    /// Gets or sets the horizontal offset, in pixels, of the drop shadow.
    /// </summary>
    public float DropshadowOffsetX
    {
        get => ContainedRenderable.DropshadowOffsetX;
        set => ContainedRenderable.DropshadowOffsetX = value;
    }

    /// <summary>
    /// Gets or sets the vertical offset, in pixels, of the drop shadow.
    /// </summary>
    public float DropshadowOffsetY
    {
        get => ContainedRenderable.DropshadowOffsetY;
        set => ContainedRenderable.DropshadowOffsetY = value;
    }

    /// <summary>
    /// The amount of horizontal blur applied to the drop shadow. A value of 0 means no blur (sharp shadow).
    /// </summary>
    public float DropshadowBlurX
    {
        get => ContainedRenderable.DropshadowBlurX;
        set => ContainedRenderable.DropshadowBlurX = value;
    }

    /// <summary>
    /// The amount of vertical blur applied to the drop shadow. A value of 0 means no blur (sharp shadow).
    /// </summary>
    public float DropshadowBlurY
    {
        get => ContainedRenderable.DropshadowBlurY;
        set => ContainedRenderable.DropshadowBlurY = value;
    }

    #endregion

    #region Filled/Stroke

    /// <summary>
    /// Whether the shape is filled (true) or just an outline (false).
    /// </summary>
    public bool IsFilled
    {
        get => ContainedRenderable.IsFilled;
        set => ContainedRenderable.IsFilled = value;
    }

    /// <summary>
    /// Gets or sets the width of the stroke used to draw shapes or lines.
    /// This is only applicable when IsFilled is false.
    /// </summary>
    public float StrokeWidth
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the unit of measurement used for the stroke width.
    /// </summary>
    public DimensionUnitType StrokeWidthUnits
    {
        get;
        set;
    }

    #endregion



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
