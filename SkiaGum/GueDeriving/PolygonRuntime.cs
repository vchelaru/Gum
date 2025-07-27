using Gum.Converters;
using Gum.Wireframe;
using SkiaGum.Renderables;
using SkiaSharp;
using System.Collections.Generic;

namespace SkiaGum.GueDeriving;

public class PolygonRuntime : SkiaShapeRuntime
{
    #region Fields/Properties

    protected override RenderableBase ContainedRenderable => ContainedPolygon;


    Polygon mContainedPolygon;
    Polygon ContainedPolygon
    {
        get
        {
            if (mContainedPolygon == null)
            {
                mContainedPolygon = this.RenderableComponent as Polygon;
            }
            return mContainedPolygon;
        }
    }

    public bool IsClosed
    {
        get => ContainedPolygon.IsClosed;
        set => ContainedPolygon.IsClosed = false;
    }

    public List<SKPoint> Points
    {
        get => ContainedPolygon.Points;
        set => ContainedPolygon.Points = value;
    }


    public GeneralUnitType PointXUnits
    {
        get => ContainedPolygon.PointXUnits;
        set => ContainedPolygon.PointXUnits = value;
    }
    public GeneralUnitType PointYUnits
    {
        get => ContainedPolygon.PointYUnits;
        set => ContainedPolygon.PointYUnits = value;
    }

    #endregion

    public PolygonRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new Polygon());
            ContainedPolygon.IsFilled = false;
            ContainedPolygon.StrokeWidth = 1;
            // If width and height are 0, it won't draw
            Width = 1;
            Height = 1;
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (PolygonRuntime)base.Clone();

        toReturn.mContainedPolygon = null;

        return toReturn;
    }
}
