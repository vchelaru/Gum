using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.GueDeriving
{
    public class PolygonRuntime : BindableGraphicalUiElement
    {
        #region Fields/Properties

        Polygon mContainedPolygon;
        Polygon ContainedPolygon
        {
            get
            {
                if(mContainedPolygon == null)
                {
                    mContainedPolygon = this.RenderableComponent as Polygon;
                }
                return mContainedPolygon;
            }
        }

        public SKColor Color
        {
            get => ContainedPolygon.Color;
            set => ContainedPolygon.Color = value;
        }

        public List<SKPoint> Points
        {
            get => ContainedPolygon.Points;
            set => ContainedPolygon.Points = value;
        }

        #endregion

        public PolygonRuntime(bool fullInstantiation = true)
        {
            if(fullInstantiation)
            {
                SetContainedObject(new Polygon());
                // If width and height are 0, it won't draw
                Width = 1;
                Height = 1;
            }
        }
    }
}
