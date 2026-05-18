using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Numerics;

#if RAYLIB
using Gum.DataTypes;
using Gum.Renderables;
using Color = Raylib_cs.Color;
using ColorExtensions = RaylibGum.Helpers.ColorExtensions;
using ContainedPolygonType = Gum.Renderables.LinePolygon;
#elif SOKOL
using Gum.DataTypes;
using Gum.Renderables;
using Color = SokolGum.Color;
using ContainedPolygonType = Gum.Renderables.LinePolygon;
#elif SKIA
using Gum.DataTypes;
using SkiaGum.Renderables;
using SkiaSharp;
using Color = SkiaSharp.SKColor;
using ContainedPolygonType = SkiaGum.Renderables.Polygon;
#else
using Color = Microsoft.Xna.Framework.Color;
using ColorExtensions = ToolsUtilitiesStandard.Helpers.ColorExtensions;
using ContainedPolygonType = global::RenderingLibrary.Math.Geometry.LinePolygon;
using global::RenderingLibrary.Math.Geometry;
using Gum.DataTypes;
#endif

#if FRB
namespace MonoGameGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

/// <summary>
/// A visual polygon element which can display any arbitrary convex or concave shape.
/// </summary>
/// <remarks>
/// Unified across MonoGame / Raylib / Skia in issue #2757. On Skia the runtime inherits
/// <c>SkiaShapeRuntime</c> so it picks up the shared fill / gradient / dropshadow API
/// surface; on MonoGame and Raylib the runtime inherits <see cref="InteractiveGue"/>
/// directly and exposes color properties inline against the contained <c>LinePolygon</c>.
/// </remarks>
#if SKIA
public class PolygonRuntime : SkiaShapeRuntime
#else
public class PolygonRuntime : InteractiveGue
#endif
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

#if SKIA
    /// <summary>
    /// Routes <see cref="SkiaShapeRuntime"/>'s solid/gradient/stroke/dropshadow accessors
    /// to the contained <see cref="Polygon"/>.
    /// </summary>
    protected override RenderableShapeBase ContainedRenderable => ContainedPolygon;
#endif

#if !SKIA
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

    /// <summary>
    /// Obsolete: use <see cref="StrokeWidth"/>. Legacy pre-#2757 setter that writes the
    /// contained <c>LinePolygon</c>'s pixel width directly, bypassing
    /// <see cref="StrokeWidthUnits"/>.
    /// </summary>
    [Obsolete("Use StrokeWidth instead. Bypasses unit handling — preserves pre-#2757 semantics.")]
    public float LineWidth
    {
        get => ContainedPolygon.LinePixelWidth;
        set
        {
            ContainedPolygon.LinePixelWidth = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Obsolete: cross-backend dashed strokes are not currently supported on
    /// <see cref="PolygonRuntime"/>. Preserved on MonoGame/Raylib for back-compat — writes
    /// directly to the contained <c>LinePolygon</c>.
    /// </summary>
    [Obsolete("Dashed polygon strokes are not part of the unified Skia/MG/Raylib API surface in #2757. Preserved on MG/Raylib only.")]
    public bool IsDotted
    {
        get => ContainedPolygon.IsDotted;
        set
        {
            ContainedPolygon.IsDotted = value;
            NotifyPropertyChanged();
        }
    }

    float _strokeWidth = 1;

    /// <inheritdoc cref="CircleRuntime.StrokeWidth"/>
    public float StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            _strokeWidth = value;
            NotifyPropertyChanged();
        }
    }

    DimensionUnitType _strokeWidthUnits;

    /// <inheritdoc cref="CircleRuntime.StrokeWidthUnits"/>
    public DimensionUnitType StrokeWidthUnits
    {
        get => _strokeWidthUnits;
        set
        {
            _strokeWidthUnits = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Pushes the stroke width to the contained <c>LinePolygon</c> each frame, resolving
    /// <see cref="StrokeWidthUnits"/> against the current camera zoom so a ScreenPixel
    /// stroke holds its on-screen pixel width regardless of zoom.
    /// </summary>
    public override void PreRender()
    {
        float strokeWidth = _strokeWidth;
        if (_strokeWidthUnits == DimensionUnitType.ScreenPixel)
        {
            var camera = this.EffectiveManagers?.Renderer?.Camera;
            if (camera != null)
            {
                strokeWidth /= camera.Zoom;
            }
        }
        ContainedPolygon.LinePixelWidth = strokeWidth;
    }
#endif

#if SKIA
    /// <summary>
    /// When <c>true</c>, the polygon path is closed (a line is drawn from the last point
    /// back to the first). When <c>false</c>, the path renders as an open polyline.
    /// Skia-only — MonoGame/Raylib <c>LinePolygon</c> always closes when the caller's
    /// point list repeats the first point at the end.
    /// </summary>
    public bool IsClosed
    {
        get => ContainedPolygon.IsClosed;
        // Pre-unification bug (#2757): the old SkiaGum PolygonRuntime.IsClosed setter
        // wrote `false` literally instead of `value`. Fixed on convergence.
        set => ContainedPolygon.IsClosed = value;
    }

    /// <summary>
    /// Direct access to the contained <see cref="Polygon"/>'s SKPoint list. Skia-only —
    /// the cross-backend API is <see cref="SetPoints(ICollection{Vector2})"/>.
    /// </summary>
    public List<SKPoint> Points
    {
        get => ContainedPolygon.Points;
        set => ContainedPolygon.Points = value;
    }

    /// <summary>
    /// Unit of measurement for point X coordinates. Skia-only.
    /// </summary>
    public Gum.Converters.GeneralUnitType PointXUnits
    {
        get => ContainedPolygon.PointXUnits;
        set => ContainedPolygon.PointXUnits = value;
    }

    /// <summary>
    /// Unit of measurement for point Y coordinates. Skia-only.
    /// </summary>
    public Gum.Converters.GeneralUnitType PointYUnits
    {
        get => ContainedPolygon.PointYUnits;
        set => ContainedPolygon.PointYUnits = value;
    }

    /// <inheritdoc/>
    public override GraphicalUiElement Clone()
    {
        var toReturn = (PolygonRuntime)base.Clone();
        toReturn.containedPolygon = null;
        return toReturn;
    }
#endif

    public PolygonRuntime(bool fullInstantiation = true, SystemManagers? systemManagers = null)
    {
        if (fullInstantiation)
        {
#if SKIA
            var polygon = new ContainedPolygonType();
            SetContainedObject(polygon);
            containedPolygon = polygon;

            // Defaults match the pre-unification SkiaGum PolygonRuntime: white outline,
            // 1 px screen-pixel stroke, IsFilled = false. Width / Height = 1 ensures the
            // renderer's bounding rect is non-empty so the polygon actually draws even
            // before the user sets a size (the bounding rect is only used to translate
            // and Percentage-scale points; PixelsFromSmall points render at face value).
            StrokeColor = SKColors.White;
            StrokeWidth = 1;
            StrokeWidthUnits = DimensionUnitType.ScreenPixel;
            Width = 1;
            Height = 1;
#else
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
#endif
        }
    }

    public void SetPoints(ICollection<Vector2> points) => ContainedPolygon.SetPoints(points);
    public void InsertPointAt(Vector2 point, int index) => ContainedPolygon.InsertPointAt(point, index);
    public void RemovePointAtIndex(int index) => ContainedPolygon.RemovePointAtIndex(index);
    public void SetPointAt(Vector2 point, int index) => ContainedPolygon.SetPointAt(point, index);

    public override bool IsPointInside(float worldX, float worldY) =>
        ContainedPolygon.IsPointInside(worldX, worldY);

    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    [Obsolete("Use the AddToRoot extension method instead (e.g. myPolygon.AddToRoot()).")]
    public new void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);
}
