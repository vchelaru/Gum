#if MONOGAME || FNA || KNI
#define XNALIKE
#endif

using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Numerics;

#if RAYLIB
using Gum.Renderables;
using Color = Raylib_cs.Color;
using ColorExtensions = RaylibGum.Helpers.ColorExtensions;
using ContainedPolygonType = Gum.Renderables.LinePolygon;
#elif SOKOL
using Gum.Renderables;
using Color = SokolGum.Color;
using ContainedPolygonType = Gum.Renderables.LinePolygon;
#else
using Color = Microsoft.Xna.Framework.Color;
using ColorExtensions = ToolsUtilitiesStandard.Helpers.ColorExtensions;
using ContainedPolygonType = global::RenderingLibrary.Math.Geometry.LinePolygon;
using global::RenderingLibrary.Math.Geometry;
#endif

#if FRB
namespace MonoGameGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

/// <summary>
/// A visual polygon element which can display any arbitrary convex or concave shape.
/// </summary>
public class PolygonRuntime : InteractiveGue
{
    ContainedPolygonType containedPolygon;
    ContainedPolygonType ContainedPolygon
    {
        get
        {
            if (containedPolygon == null)
            {
                containedPolygon = this.RenderableComponent as ContainedPolygonType;
            }
            return containedPolygon;
        }
    }

    /// <summary>
    /// The red component of the polygon color. Ranges from 0 to 255.
    /// </summary>
    public int Red
    {
        get => ContainedPolygon.Color.R;
        set
        {
            ContainedPolygon.Color = ColorExtensions.WithRed(ContainedPolygon.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The green component of the polygon color. Ranges from 0 to 255.
    /// </summary>
    public int Green
    {
        get => ContainedPolygon.Color.G;
        set
        {
            ContainedPolygon.Color = ColorExtensions.WithGreen(ContainedPolygon.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The blue component of the polygon color. Ranges from 0 to 255.
    /// </summary>
    public int Blue
    {
        get => ContainedPolygon.Color.B;
        set
        {
            ContainedPolygon.Color = ColorExtensions.WithBlue(ContainedPolygon.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The alpha (opacity) component of the polygon color. Ranges from 0 (fully transparent) to 255 (fully opaque).
    /// </summary>
    public int Alpha
    {
        get => ContainedPolygon.Color.A;
        set
        {
            ContainedPolygon.Color = ColorExtensions.WithAlpha(ContainedPolygon.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the color used to render the polygon. This includes color and alpha (opacity) components.
    /// </summary>
    public Color Color
    {
#if XNALIKE
        get => global::RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedPolygon.Color);
        set
        {
            ContainedPolygon.Color = global::RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
            NotifyPropertyChanged();
        }
#else
        get => ContainedPolygon.Color;
        set
        {
            ContainedPolygon.Color = value;
            NotifyPropertyChanged();
        }
#endif
    }

    public float LineWidth
    {
        get => ContainedPolygon.LinePixelWidth;
        set
        {
            ContainedPolygon.LinePixelWidth = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsDotted
    {
        get => ContainedPolygon.IsDotted;
        set
        {
            ContainedPolygon.IsDotted = value;
            NotifyPropertyChanged();
        }
    }

    public PolygonRuntime(bool fullInstantiation = true, SystemManagers? systemManagers = null)
    {
        if (fullInstantiation)
        {
            var polygon = new ContainedPolygonType(systemManagers ?? SystemManagers.Default);
            SetContainedObject(polygon);
            containedPolygon = polygon;

            polygon.Color = ColorExtensions.White;

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

    public void InsertPointAt(Vector2 point, int index) => ContainedPolygon.InsertPointAt(point, index);
    public void RemovePointAtIndex(int index) => ContainedPolygon.RemovePointAtIndex(index);
    public void SetPointAt(Vector2 point, int index) => ContainedPolygon.SetPointAt(point, index);

    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    [Obsolete("Use the AddToRoot extension method instead (e.g. myPolygon.AddToRoot()).")]
    public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

    public override bool IsPointInside(float worldX, float worldY) =>
        ContainedPolygon.IsPointInside(worldX, worldY);
}
