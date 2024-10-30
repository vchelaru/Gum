using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving
{
    public class PolygonRuntime : GraphicalUiElement
    {
        RenderingLibrary.Math.Geometry.LinePolygon containedPolygon;
        RenderingLibrary.Math.Geometry.LinePolygon ContainedPolygon
        {
            get
            {
                if (containedPolygon == null)
                {
                    containedPolygon = this.RenderableComponent as RenderingLibrary.Math.Geometry.LinePolygon;
                }
                return containedPolygon;
            }
        }

        public Microsoft.Xna.Framework.Color Color
        {
            get
            {
                return RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedPolygon.Color);
            }
            set
            {
                ContainedPolygon.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
                NotifyPropertyChanged();
            }
        }

        public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

        public PolygonRuntime(bool fullInstantiation = true)
        {
            if (fullInstantiation)
            {
                var polygon = new RenderingLibrary.Math.Geometry.LinePolygon();
                SetContainedObject(polygon);
                containedPolygon = polygon;

                polygon.Color = System.Drawing.Color.White;
                polygon.SetPoints(new Vector2[]
                {
                    new Vector2(0, 0),
                    new Vector2(0, 10),
                    new Vector2(10, 10),
                    new Vector2(10, 0)
                });
            }
        }

        public void SetPoints(ICollection<Vector2> points) => ContainedPolygon.SetPoints(points);

        public float LineWidth
        {
            get => ContainedPolygon.LinePixelWidth;
            set => ContainedPolygon.LinePixelWidth = value;
        }

        public bool IsDotted
        {
            get => ContainedPolygon.IsDotted;
            set => ContainedPolygon.IsDotted = value;
        }

        public void InsertPointAt(Vector2 point, int index) => ContainedPolygon.InsertPointAt(point, index);
        public void RemovePointAtIndex(int index) => ContainedPolygon.RemovePointAtIndex(index);
        public void SetPointAt(Vector2 point, int index) => ContainedPolygon.SetPointAt(point, index);



    }
}
