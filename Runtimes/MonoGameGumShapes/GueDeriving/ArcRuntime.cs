using Gum.Forms.DefaultVisuals;
using MonoGameAndGum.Renderables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving;

public class ArcRuntime : AposShapeRuntime
{
    protected override AposShapeBase ContainedRenderable => ContainedArc;



    Arc _containedArc;

    Arc ContainedArc
    {
        get
        {
            if(_containedArc == null)
            {
                _containedArc = this.RenderableComponent as Arc;
            }
            return _containedArc;
        }
    }

    public float Thickness
    {
        get => ContainedArc.Thickness;
        set => ContainedArc.Thickness = value;
    }

    public float StartAngle
    {
        get => ContainedArc.StartAngle;
        set => ContainedArc.StartAngle = value;
    }

    public float SweepAngle
    {
        get => ContainedArc.SweepAngle;
        set => ContainedArc.SweepAngle = value;
    }

    [Obsolete("Not currently functional, added to match SkiaGum syntax")]
    public bool IsEndRounded
    {
        get;
        set;
    }

    public ArcRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new Arc());
            this.Color = Microsoft.Xna.Framework.Color.White;
            Width = 100;
            Height = 100;

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
