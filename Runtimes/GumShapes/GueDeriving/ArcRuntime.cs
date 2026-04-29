using Gum.Forms.DefaultVisuals;
using MonoGameAndGum.Renderables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Runtime that draws a circular arc inscribed in its Width x Height bounds.
/// The arc is always stroked (it is never filled); use Thickness to control its weight.
/// </summary>
public class ArcRuntime : AposShapeRuntime
{
    protected override RenderableShapeBase ContainedRenderable => ContainedArc;



    Arc _containedArc = default!;

    Arc ContainedArc
    {
        get
        {
            if(_containedArc == null)
            {
                _containedArc = (Arc)this.RenderableComponent;
            }
            return _containedArc;
        }
    }

    /// <summary>
    /// Gets or sets the thickness of the arc, in pixels.
    /// </summary>
    public float Thickness
    {
        get => ContainedArc.Thickness;
        set => ContainedArc.Thickness = value;
    }

    /// <summary>
    /// Gets or sets the angle, in degrees, at which the arc begins. A value of 0 points to the right,
    /// and increasing values sweep counter-clockwise.
    /// </summary>
    public float StartAngle
    {
        get => ContainedArc.StartAngle;
        set => ContainedArc.StartAngle = value;
    }

    /// <summary>
    /// Gets or sets how far the arc sweeps from StartAngle, in degrees. A value of 360 produces a full ring.
    /// </summary>
    public float SweepAngle
    {
        get => ContainedArc.SweepAngle;
        set => ContainedArc.SweepAngle = value;
    }

    /// <summary>
    /// Gets or sets whether the ends of the arc are rounded. If true, the arc has rounded caps; if false, the ends are flat.
    /// </summary>
    public bool IsEndRounded
    {
        get => ContainedArc.IsEndRounded;
        set => ContainedArc.IsEndRounded = value;
    }

    /// <summary>
    /// Initializes a new ArcRuntime. When fullInstantiation is true (the default), an underlying
    /// Apos.Shapes Arc renderable is created and default values are applied (Width = Height = 100,
    /// StartAngle = 0, SweepAngle = 90, IsEndRounded = true, Color = White).
    /// Pass false only when the runtime is being constructed by deserialization, which sets up
    /// the renderable separately.
    /// </summary>
    public ArcRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new Arc());
            this.Color = Microsoft.Xna.Framework.Color.White;
            Width = 100;
            Height = 100;

            IsEndRounded = true;

            StartAngle = 0;
            SweepAngle = 90;

            Red1 = 255;
            Green1 = 255;
            Blue1 = 255;

            Red2 = 255;
            Green2 = 255;
            Blue2 = 0;

            GradientX2 = 100;
            GradientY2 = 100;

            DropshadowAlpha = 255;
            DropshadowRed = 0;
            DropshadowGreen = 0;
            DropshadowBlue = 0;

            DropshadowOffsetX = 0;
            DropshadowOffsetY = 3;
            DropshadowBlurX = 0;
            DropshadowBlurY = 3;
        }
    }
}
