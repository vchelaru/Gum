using Gum.Converters;
using MonoGameAndGum.Renderables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Runtime that draws a rectangle with rounded corners, sized by its Width and Height.
/// Use CornerRadius to control how rounded the corners are; a value of 0 produces a sharp-cornered rectangle.
/// </summary>
public class RoundedRectangleRuntime : AposShapeRuntime
{
    protected override RenderableShapeBase ContainedRenderable => ContainedRectangle;

    RoundedRectangle _containedRectangle = default!;
    RoundedRectangle ContainedRectangle
    {
        get
        {
            if (_containedRectangle == null)
            {
                _containedRectangle = (RoundedRectangle)this.RenderableComponent;
            }
            return _containedRectangle;
        }
    }

    /// <summary>
    /// Gets or sets the radius, in pixels, of each rounded corner. A value of 0 produces a sharp-cornered rectangle.
    /// </summary>
    public float CornerRadius
    {
        get => ContainedRectangle.CornerRadius;
        set => ContainedRectangle.CornerRadius = value;
    }

    /// <summary>
    /// Initializes a new RoundedRectangleRuntime. When fullInstantiation is true (the default), an underlying
    /// Apos.Shapes RoundedRectangle renderable is created and default values are applied (Width = Height = 100,
    /// CornerRadius = 5, Color = White).
    /// Pass false only when the runtime is being constructed by deserialization, which sets up
    /// the renderable separately.
    /// </summary>
    public RoundedRectangleRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedShape(new RoundedRectangle());

            // Make defaults 100 to match Glue
            Width = 100;
            Height = 100;

            CornerRadius = 5;

            DropshadowAlpha = 255;
            DropshadowRed = 0;
            DropshadowGreen = 0;
            DropshadowBlue = 0;

            DropshadowOffsetX = 0;
            DropshadowOffsetY = 3;
            DropshadowBlurX = 0;
            DropshadowBlurY = 3;

            //GradientType = GradientType.Linear;

            GradientX1 = 0;
            GradientX1Units = GeneralUnitType.PixelsFromSmall;

            GradientY1 = 0;
            GradientY1Units = GeneralUnitType.PixelsFromSmall;

            Red = 255;
            Green = 255;
            Blue = 255;

            Red1 = 255;
            Green1 = 255;
            Blue1 = 255;

            GradientX2 = 100;
            GradientX2Units = GeneralUnitType.PixelsFromSmall;

            GradientY2 = 100;
            GradientY2Units = GeneralUnitType.PixelsFromSmall;

            Red2 = 255;
            Green2 = 255;
            Blue2 = 0;
        }
    }
}
