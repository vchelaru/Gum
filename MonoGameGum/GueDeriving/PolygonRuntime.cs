using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Numerics;

#if RAYLIB
using Gum.DataTypes;
using Gum.Renderables;
using RaylibGum.Helpers;
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
using global::RenderingLibrary.Graphics;
using global::RenderingLibrary.Math.Geometry;
using Color = Microsoft.Xna.Framework.Color;
using ColorExtensions = ToolsUtilitiesStandard.Helpers.ColorExtensions;
using ContainedPolygonType = global::RenderingLibrary.Math.Geometry.LinePolygon;
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
        get => ContainedPolygon.Color.ToUserColor();
        set
        {
            ContainedPolygon.Color = value.ToContainerColor();
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Obsolete: renamed to <see cref="StrokeWidth"/> in #2757 for cross-backend naming
    /// parity with <see cref="CircleRuntime"/> and <see cref="RectangleRuntime"/>. Functional
    /// behavior is identical (still writes the contained <c>LinePolygon</c>'s pixel width),
    /// but <see cref="LineWidth"/> bypasses <see cref="StrokeWidthUnits"/> so ScreenPixel
    /// scaling against the camera zoom does not engage.
    /// </summary>
    [Obsolete("Renamed to StrokeWidth in #2757 for cross-backend naming parity. Functional behavior is unchanged; switch to StrokeWidth to also pick up StrokeWidthUnits scaling.")]
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
    /// Obsolete: superseded in #2757 by the <see cref="StrokeDashLength"/> /
    /// <see cref="StrokeGapLength"/> pair for cross-backend naming parity with
    /// <see cref="CircleRuntime"/> and <see cref="RectangleRuntime"/>. The MG/Raylib
    /// <c>LinePolygon</c> renderable only exposes a binary dotted toggle (a fixed-pattern
    /// texture), so user-set dash/gap lengths cannot drive per-segment rendering on these
    /// backends — assigning either of the new properties to a positive value engages the
    /// same binary dot pattern that this property used to control.
    /// </summary>
    [Obsolete("Renamed to StrokeDashLength + StrokeGapLength in #2757 for cross-backend parity with CircleRuntime/RectangleRuntime. raylib (post-#2757) and Skia honor the lengths verbatim via a perimeter walk / SKPathEffect.CreateDash; MG still falls back to its fixed-pattern dotted texture (LinePolygon has no per-segment dash control). Set both new properties to non-zero values to engage dashing on any backend.")]
    public bool IsDotted
    {
        get => ContainedPolygon.IsDotted;
        set
        {
            ContainedPolygon.IsDotted = value;
            NotifyPropertyChanged();
        }
    }

    float _strokeDashLength;

    /// <summary>
    /// Length of each dash segment, in pixels. Dashing engages when both this and
    /// <see cref="StrokeGapLength"/> are greater than zero. On MG/Raylib the backing
    /// <c>LinePolygon</c> only supports a fixed-pattern dotted texture, so the lengths drive
    /// a binary on/off rather than true per-segment dashing; on Skia the lengths flow through
    /// <see cref="SkiaShapeRuntime"/> to <c>SKPathEffect.CreateDash</c> verbatim.
    /// </summary>
    public float StrokeDashLength
    {
        get => _strokeDashLength;
        set
        {
            _strokeDashLength = value;
            ApplyDashState();
            NotifyPropertyChanged();
        }
    }

    float _strokeGapLength;

    /// <summary>
    /// Length of the gap between dash segments, in pixels. See <see cref="StrokeDashLength"/>
    /// for the engage condition and per-backend fidelity notes.
    /// </summary>
    public float StrokeGapLength
    {
        get => _strokeGapLength;
        set
        {
            _strokeGapLength = value;
            ApplyDashState();
            NotifyPropertyChanged();
        }
    }

    // Drives the contained LinePolygon's dash state from the unified dash/gap pair. On MG the
    // renderable only exposes a binary IsDotted toggle (fixed-pattern texture), so dashing
    // engages when both lengths are positive — same engage rule as Skia's RenderableShapeBase
    // (`dash > 0 && gap > 0` guard on SKPathEffect.CreateDash). On raylib the renderable
    // honors the actual lengths via a perimeter walk (#2757), so push them through directly.
    void ApplyDashState()
    {
        ContainedPolygon.IsDotted = _strokeDashLength > 0 && _strokeGapLength > 0;
#if RAYLIB
        ContainedPolygon.StrokeDashLength = _strokeDashLength;
        ContainedPolygon.StrokeGapLength = _strokeGapLength;
#endif
    }

    float _strokeWidth = 1;

    /// <inheritdoc cref="CircleRuntime.StrokeWidth"/>
    public float StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            _strokeWidth = value;
#if RAYLIB
            // Push immediately — PreRender does the same with ScreenPixel zoom scaling, but
            // whatever code path the gallery hit pre-fix didn't run PreRender before the first
            // draw, so the renderable stayed at its default 1 px. Same bug RectangleRuntime
            // had (#2827); fix mirrors the one already on CircleRuntime / RectangleRuntime's
            // RAYLIB setter.
            ContainedPolygon.LinePixelWidth = value;
#endif
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

#if RAYLIB
    Color? _strokeColor;

    /// <summary>
    /// Explicit stroke-pass color. When <c>null</c> the renderable falls back to
    /// <see cref="Color"/>. Mirrors the cross-backend API exposed by
    /// <see cref="CircleRuntime"/> and <see cref="RectangleRuntime"/> (#2757) so the shared
    /// PolygonsScreen sample sets <c>polygon.StrokeColor = ...</c> uniformly.
    /// </summary>
    public Color? StrokeColor
    {
        get => _strokeColor;
        set
        {
            _strokeColor = value;
            ContainedPolygon.StrokeColor = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// When <c>true</c>, draws an extra segment from the last point back to the first so the
    /// polygon outline closes. Default <c>true</c> (matches Skia's <c>Polygon.IsClosed</c>);
    /// set <c>false</c> for open polylines.
    /// </summary>
    public bool IsClosed
    {
        get => ContainedPolygon.IsClosed;
        set
        {
            ContainedPolygon.IsClosed = value;
            NotifyPropertyChanged();
        }
    }
#endif

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
#endif

    /// <inheritdoc/>
    /// <remarks>
    /// Resets the cached <c>containedPolygon</c> reference so the clone re-resolves against
    /// its own <c>RenderableComponent</c> on next access. <c>MemberwiseClone</c> shallow-copies
    /// the field, leaving it pointing at the source's renderable; the base <see cref="GraphicalUiElement.Clone"/>
    /// then deep-clones the renderable (via <see cref="System.ICloneable"/>) and stores it as
    /// the clone's contained object, so the cached field is the only stale link to drop.
    /// </remarks>
    public override GraphicalUiElement Clone()
    {
        var toReturn = (PolygonRuntime)base.Clone();
        toReturn.containedPolygon = null;
        return toReturn;
    }

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
