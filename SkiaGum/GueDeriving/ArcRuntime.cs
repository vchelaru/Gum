using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.GueDeriving;

public class ArcRuntime : SkiaShapeRuntime
{
    protected override Renderables.RenderableShapeBase ContainedRenderable => ContainedArc;

    Arc mContainedArc;
    Arc ContainedArc
    {
        get
        {
            if(mContainedArc == null)
            {
                mContainedArc = this.RenderableComponent as Arc;
            }
            return mContainedArc;
        }
    }
    public bool IsEndRounded
    {
        get => ContainedArc.IsEndRounded;
        set => ContainedArc.IsEndRounded = value;
    }

    public float Thickness
    {
        get => base.StrokeWidth;
        set => base.StrokeWidth = value;
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

    public ArcRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new Arc());
            this.Color = SKColors.White;
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
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (ArcRuntime)base.Clone();

        toReturn.mContainedArc = null;

        return toReturn;
    }
}
