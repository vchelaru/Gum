using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// A visual polygon element which can display any arbitrary convex or concave shape.
/// </summary>
public class PolygonRuntime : InteractiveGue
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

    /// <summary>
    /// The red component of the polygon color. Ranges from 0 to 255.
    /// </summary>
    public int Red
    {
        get
        {
            return ContainedPolygon.Color.R;
        }
        set
        {
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithRed(ContainedPolygon.Color, (byte)value);
            ContainedPolygon.Color = color;
        }
    }

    /// <summary>
    /// The green component of the polygon color. Ranges from 0 to 255.
    /// </summary>
    public int Green
    {
        get
        {
            return ContainedPolygon.Color.G;
        }
        set
        {
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithGreen(ContainedPolygon.Color, (byte)value);
            ContainedPolygon.Color = color;
        }
    }

    /// <summary>
    /// The blue component of the polygon color. Ranges from 0 to 255.
    /// </summary>
    public int Blue
    {
        get
        {
            return ContainedPolygon.Color.B;
        }
        set
        {
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithBlue(ContainedPolygon.Color, (byte)value);
            ContainedPolygon.Color = color;
        }
    }

    /// <summary>
    /// The alpha (opacity) component of the polygon color. Ranges from 0 (fully transparent) to 255 (fully opaque).
    /// </summary>
    public int Alpha
    {
        get
        {
            return ContainedPolygon.Color.A;
        }
        set
        {
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithAlpha(ContainedPolygon.Color, (byte)value);
            ContainedPolygon.Color = color;
        }
    }

    /// <summary>
    /// Gets or sets the color used to render the polygon. This includes color and alpha (opacity) components.
    /// </summary>
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

    public PolygonRuntime(bool fullInstantiation = true, SystemManagers? systemManagers = null)
    {
        if (fullInstantiation)
        {
            var polygon = new RenderingLibrary.Math.Geometry.LinePolygon(systemManagers ?? SystemManagers.Default);
            SetContainedObject(polygon);
            containedPolygon = polygon;

            polygon.Color = System.Drawing.Color.White;
            polygon.SetPoints(new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(0, 32),
                new Vector2(32, 32),
                new Vector2(32, 0),
                new Vector2(0, 0),
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
    public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

    public override bool IsPointInside(float worldX, float worldY) =>
        ContainedPolygon.IsPointInside(worldX, worldY);

}
