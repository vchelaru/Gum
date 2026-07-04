using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if FRB
namespace MonoGameGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

/// <summary>
/// Base class for all shapes, providng common properties like color, gradient, and dropshadow.
/// </summary>
public abstract class AposShapeRuntime : GraphicalUiElement
{
    private static bool _registered;

    /// <summary>
    /// Registers the Arc, ColoredCircle, Line, and RoundedRectangle runtime types with Gum so that
    /// instances loaded from .gumx project files are instantiated as the corresponding *Runtime classes.
    /// Called automatically by the runtime via the [ModuleInitializer] attribute - applications do not
    /// need to call this directly.
    ///
    /// On Mono/WASM (Blazor) the [ModuleInitializer] does not fire reliably until a type from this
    /// assembly is touched, which is too late if the .gumx loads before any shape type is referenced.
    /// To handle that, GumService.Initialize and ShapeRenderer.Initialize also call this method via
    /// reflection / direct call. The guard below makes those redundant calls cheap no-ops.
    /// </summary>
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    public static void RegisterRuntimeTypes()
    {
        // All four factory registrations live OUTSIDE the _registered guard on purpose.
        // ElementSave + event registrations are idempotent and guarded; registry factories
        // must be re-applied every call because GumService.Uninitialize and
        // BaseTestClass.Dispose both reset RenderableRegistry between Initialize cycles
        // (issue #2761 "load-order contract"). Each factory is context-bearing so it can wire
        // OnPreRender — an internal member of RenderableShapeBase that core MonoGameGum can't
        // reach — pointing back at the calling runtime's PreRender override.
        //
        // Issue #2768 two-slot model: each shape runtime (CircleRuntime, RectangleRuntime)
        // resolves a fill renderable AND a stroke renderable at construction. On the Apos
        // backend both slots are an Apos shape — the fill factory sets IsFilled=true on its
        // instance, the stroke factory sets IsFilled=false on its instance. The runtime holds
        // both and they draw simultaneously.
        // Issue #3112 — each slot factory only hands out an Apos shape once ShapeRenderer is
        // actually initialized. Until then it returns null, which RenderableRegistry treats as
        // graceful degradation: CircleRuntime / RectangleRuntime fall back to core's no-shapes
        // default renderable (DefaultStrokedCircleRenderable / DefaultFilledRectangleRenderable).
        // Without this gate, merely having the shapes assembly loaded — e.g. force-registered by
        // GumService.Initialize's reflection scan on WASM, where every assembly loads up front —
        // would route a plain Rectangle/Circle the user never opted into to an Apos renderable
        // that throws "ShapeRenderer is null" at draw. Returning null! (not null) keeps the
        // nullable-enabled Func<GraphicalUiElement, T> signature quiet; the registry and the
        // runtime constructors both expect and handle null here.
        RenderableRegistry.RegisterFactory<Gum.GueDeriving.IFilledCircleRenderable>(gue =>
        {
            if (!ShapeRenderer.Self.IsInitialized) return null!;
            var shape = new Circle { IsFilled = true };
            WireOnPreRender(shape, gue);
            return shape;
        });
        RenderableRegistry.RegisterFactory<Gum.GueDeriving.IStrokedCircleRenderable>(gue =>
        {
            if (!ShapeRenderer.Self.IsInitialized) return null!; // #3112 gate (see above)
            var shape = new Circle { IsFilled = false };
            WireOnPreRender(shape, gue);
            return shape;
        });
        RenderableRegistry.RegisterFactory<Gum.GueDeriving.IFilledRectangleRenderable>(gue =>
        {
            if (!ShapeRenderer.Self.IsInitialized) return null!; // #3112 gate (see above)
            var shape = new RoundedRectangle { IsFilled = true };
            WireOnPreRender(shape, gue);
            return shape;
        });
        RenderableRegistry.RegisterFactory<Gum.GueDeriving.IStrokedRectangleRenderable>(gue =>
        {
            if (!ShapeRenderer.Self.IsInitialized) return null!; // #3112 gate (see above)
            var shape = new RoundedRectangle { IsFilled = false };
            WireOnPreRender(shape, gue);
            return shape;
        });

        if (_registered) return;
        _registered = true;

        // Construct the MonoGameGum.GueDeriving shim subclasses (not the new Gum.GueDeriving
        // base types). This file's namespace is Gum.GueDeriving on non-FRB builds, so an
        // unqualified `new RoundedRectangleRuntime()` would resolve to the base type — but
        // already-generated user code casts the loaded instance to the deprecated shim namespace
        // (`... as global::MonoGameGum.GueDeriving.RoundedRectangleRuntime`), and that cast only
        // succeeds when the instance is the most-derived shim. Instantiating the base yields null
        // and a NullReferenceException downstream (issue #3380). Mirrors the same fix in
        // RenderingLibrary.SystemManagers.RegisterComponentRuntimeInstantiations for the standard
        // (non-Apos) runtime types. On FRB builds these qualified names resolve to FRB's own
        // MonoGameGum.GueDeriving types, matching the prior behavior. Drop the qualification once
        // the MonoGameGum.GueDeriving shims are removed.
#pragma warning disable CS0618 // Type or member is obsolete
        ElementSaveExtensions.RegisterGueInstantiation(
            "Arc",
            () => new global::MonoGameGum.GueDeriving.ArcRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "ColoredCircle",
            () => new global::MonoGameGum.GueDeriving.ColoredCircleRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "Line",
            () => new global::MonoGameGum.GueDeriving.LineRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "RoundedRectangle",
            () => new global::MonoGameGum.GueDeriving.RoundedRectangleRuntime());
#pragma warning restore CS0618

        StandardElementsManager.Self.CustomGetDefaultState =
            CombineGetDefaultState(StandardElementsManager.Self.CustomGetDefaultState);

        CustomSetPropertyOnRenderable.AdditionalPropertyOnRenderable +=
            MonoGameGumShapes.CustomSetPropertyOnRenderable.SetPropertyOnRenderableFunc;
    }

    /// <summary>
    /// Wires the factory-built Apos shape's <c>OnPreRender</c> hook back at the calling
    /// runtime's <c>PreRender</c>. Each runtime type the shape can back gets its own arm —
    /// the call has to land on the GUE that actually owns the shape so ScreenPixel scaling
    /// resolves against the right camera/zoom. Both fill and stroke instances get this
    /// wiring; the runtime's PreRender is idempotent (just pushes the stroke width once),
    /// so being reached twice per frame from a two-slot runtime is fine.
    /// </summary>
    private static void WireOnPreRender(RenderableShapeBase shape, GraphicalUiElement gue)
    {
        switch (gue)
        {
            case AposShapeRuntime apos:
                shape.OnPreRender = apos.PreRender;
                break;
            case Gum.GueDeriving.CircleRuntime circle:
                shape.OnPreRender = circle.PreRender;
                break;
            case Gum.GueDeriving.RectangleRuntime rect:
                shape.OnPreRender = rect.PreRender;
                break;
        }
    }

    private static StateSave? HandleCustomGetDefaultState(string arg)
    {
        switch (arg)
        {
            case "Arc":
                return StandardElementsManager.GetArcState();
            case "ColoredCircle":
                return StandardElementsManager.GetColoredCircleState();
            case "Line":
                return StandardElementsManager.GetLineState();
            case "RoundedRectangle":
                return StandardElementsManager.GetRoundedRectangleState();

            // Not a shape this runtime knows about - null lets CombineGetDefaultState fall back
            // to whatever resolver was already registered (e.g. the Skia plugin, for
            // Svg/LottieAnimation/Canvas) instead of guessing Container's default state.
            default:
                return null;
        }
    }

    /// <summary>
    /// Combines this runtime's own default-state resolution with <paramref name="existing"/>, so
    /// registering this runtime's types never discards another resolver's answer for a type this
    /// runtime doesn't recognize.
    /// </summary>
    /// <remarks>
    /// <see cref="StandardElementsManager.CustomGetDefaultState"/> is invoked with a single
    /// <c>Invoke()</c> call, and a multicast delegate's <c>Invoke()</c> only returns its LAST
    /// subscriber's result - every earlier subscriber still runs, but its return value is thrown
    /// away. Chaining onto it with <c>+=</c> (as this runtime used to) silently discarded whatever
    /// an earlier-registered resolver had already answered for any type this runtime doesn't
    /// recognize. In the Gum tool process, where both this runtime (KniGumShapes) and the Skia
    /// plugin register a resolver, that meant Svg/LottieAnimation/Canvas silently fell back to
    /// Container's default state - injecting Container-only variables (e.g. IsRenderTarget) into
    /// those standards' .gutx files on every project load.
    /// </remarks>
    internal static Func<string, StateSave?> CombineGetDefaultState(Func<string, StateSave?>? existing) =>
        type => HandleCustomGetDefaultState(type) ?? existing?.Invoke(type);

    /// <summary>
    /// The underlying Apos.Shapes renderable that draws this shape. Derived runtime classes return
    /// the concrete renderable they wrap (for example, ArcRuntime returns its Arc, ColoredCircleRuntime
    /// returns its Circle).
    /// </summary>
    protected abstract RenderableShapeBase ContainedRenderable { get; }

    #region Solid colors

    /// <summary>
    /// Gets or sets the alpha (opacity) value for the contained renderable object. 
    /// The value range is 0-255. This value
    /// is ignored if a gradient is being used.
    /// </summary>
    public int Alpha
    {
        get => ContainedRenderable.Alpha;
        set => ContainedRenderable.Alpha = value;
    }

    /// <summary>
    /// Gets or sets the blue component of the color. 
    /// The value range is 0-255. This value is ignored if a gradient is being used.
    /// </summary>
    public int Blue
    {
        get => ContainedRenderable.Blue;
        set => ContainedRenderable.Blue = value;
    }

    /// <summary>
    /// Gets or sets the green component value of the color.
    /// The value range is 0-255. This value is ignored if a gradient is being used.
    /// </summary>
    public int Green
    {
        get => ContainedRenderable.Green;
        set => ContainedRenderable.Green = value;
    }

    /// <summary>
    /// Gets or sets the red component value of the color.
    /// The value range is 0-255. This value is ignored if a gradient is being used.
    /// </summary>
    public int Red
    {
        get => ContainedRenderable.Red;
        set => ContainedRenderable.Red = value;
    }

    /// <summary>
    /// Gets or sets the color used to render the contained object.
    /// This value is ignored if a gradient is being used.
    /// </summary>
    public Color Color
    {
        get => ContainedRenderable.Color;
        set => ContainedRenderable.Color = value;
    }

    #endregion

    #region Gradient Colors

    /// <summary>
    /// Gets or sets the blue component value for the first gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Blue1
    {
        get => ContainedRenderable.Blue1;
        set => ContainedRenderable.Blue1 = value;
    }

    /// <summary>
    /// Gets or sets the green component value for the first gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Green1
    {
        get => ContainedRenderable.Green1;
        set => ContainedRenderable.Green1 = value;
    }

    /// <summary>
    /// Gets or sets the red component value for the first gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Red1
    {
        get => ContainedRenderable.Red1;
        set => ContainedRenderable.Red1 = value;
    }

    /// <summary>
    /// Gets or sets the alpha value used for rendering transparency for the first gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Alpha1
    {
        get => ContainedRenderable.Alpha1;
        set => ContainedRenderable.Alpha1 = value;
    }

    /// <summary>
    /// Gets or sets the first gradient color. This value is only used if a gradient is being used.
    /// </summary>
    public Color Color1
    {
        get => new Color(Red1, Green1, Blue1, Alpha1);
        set
        {
            Red1 = value.R;
            Green1 = value.G;
            Blue1 = value.B;
            Alpha1 = value.A;
        }
    }

    /// <summary>
    /// Gets or sets the blue color component for the second gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Blue2
    {
        get => ContainedRenderable.Blue2;
        set => ContainedRenderable.Blue2 = value;
    }


    /// <summary>
    /// Gets or sets the green component value for the second gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Green2
    {
        get => ContainedRenderable.Green2;
        set => ContainedRenderable.Green2 = value;
    }

    /// <summary>
    /// Gets or sets the red component value for the second gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Red2
    {
        get => ContainedRenderable.Red2;
        set => ContainedRenderable.Red2 = value;
    }

    /// <summary>
    /// Gets or sets the alpha (opacity) value for the second gradient color.
    /// The value range is 0-255. This value is only used if a gradient is being used.
    /// </summary>
    public int Alpha2
    {
        get => ContainedRenderable.Alpha2;
        set => ContainedRenderable.Alpha2 = value;
    }

    /// <summary>
    /// Gets or sets the second gradient color. This value is only used if a gradient is being used.
    /// </summary>
    public Color Color2
    {
        get => new Color(Red2, Green2, Blue2, Alpha2);
        set
        {
            Red2 = value.R;
            Green2 = value.G;
            Blue2 = value.B;
            Alpha2 = value.A;
        }
    }

    /// <summary>
    /// The X coordinate of the start of the gradient. The interpretation of this value depends on the setting of GradientX1Units.
    /// </summary>
    public float GradientX1
    {
        get => ContainedRenderable.GradientX1;
        set => ContainedRenderable.GradientX1 = value;
    }

    /// <summary>
    /// Gets or sets the unit type used to interpret the X1 coordinate of the gradient.
    /// </summary>
    public GeneralUnitType GradientX1Units
    {
        get => ContainedRenderable.GradientX1Units;
        set => ContainedRenderable.GradientX1Units = value;
    }

    /// <summary>
    /// The Y coordinate of the start of the gradient. The interpretation of this value depends on the setting of GradientY1Units.
    /// </summary>
    public float GradientY1
    {
        get => ContainedRenderable.GradientY1;
        set => ContainedRenderable.GradientY1 = value;
    }

    /// <summary>
    /// Gets or sets the unit type used to interpret the Y1 coordinate of the gradient.
    /// </summary>
    public GeneralUnitType GradientY1Units
    {
        get => ContainedRenderable.GradientY1Units;
        set => ContainedRenderable.GradientY1Units = value;
    }

    /// <summary>
    /// The X coordinate of the end of the gradient. The interpretation of this value depends on the setting of GradientX2Units. This is only used for Linear gradients.
    /// </summary>
    public float GradientX2
    {
        get => ContainedRenderable.GradientX2;
        set => ContainedRenderable.GradientX2 = value;
    }

    /// <summary>
    /// Gets or sets the coordinate system used to interpret the X2 value of the gradient vector. This is only used for Linear gradients.
    /// </summary>
    public GeneralUnitType GradientX2Units
    {
        get => ContainedRenderable.GradientX2Units;
        set => ContainedRenderable.GradientX2Units = value;
    }

    /// <summary>
    /// The Y coordinate of the end of the gradient. The interpretation of this value depends on the setting of GradientY2Units. This is only used for Linear gradients.
    /// </summary>
    public float GradientY2
    {
        get => ContainedRenderable.GradientY2;
        set => ContainedRenderable.GradientY2 = value;
    }

    /// <summary>
    /// Gets or sets the coordinate system used to interpret the Y2 value of the gradient vector. This is only used for Linear gradients.
    /// </summary>
    public GeneralUnitType GradientY2Units
    {
        get => ContainedRenderable.GradientY2Units;
        set => ContainedRenderable.GradientY2Units = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether a gradient fill is applied when rendering the contained object.
    /// If false, the solid color properties (Red, Green, Blue, Alpha) are used instead.
    /// If true, the gradient color properties (Color1, Color2, etc.) are used.
    /// </summary>
    public bool UseGradient
    {
        get => ContainedRenderable.UseGradient;
        set => ContainedRenderable.UseGradient = value;
    }

    /// <summary>
    /// Gets or sets the type of gradient used for rendering. Default is Linear.
    /// </summary>
    public GradientType GradientType
    {
        get => ContainedRenderable.GradientType;
        set => ContainedRenderable.GradientType = value;
    }

    /// <summary>
    /// The inner radius before the gradient starts to interpolate colors when using a Radial gradient.
    /// Inside this radius the shape is filled with Color1; the gradient interpolates from Color1 to Color2
    /// between this value and GradientOuterRadius.
    /// </summary>
    public float GradientInnerRadius
    {
        get => ContainedRenderable.GradientInnerRadius;
        set => ContainedRenderable.GradientInnerRadius = value;
    }

    /// <summary>
    /// Gets or sets the unit type used to interpret the inner radius when using a Radial gradient.
    /// Supported values are Absolute (pixels), PercentageOfParent (percentage of the shape's Width, so
    /// 100 = Width), and RelativeToParent (pixels offset from the shape's Width, so 0 = Width and
    /// -10 = Width - 10). Note that the shape's natural inscribed radius is Width / 2, so a value of
    /// 50 (PercentageOfParent) or -Width/2 (RelativeToParent) corresponds to that.
    /// </summary>
    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => ContainedRenderable.GradientInnerRadiusUnits;
        set => ContainedRenderable.GradientInnerRadiusUnits = value;
    }

    /// <summary>
    /// Gets or sets the outer radius at which the gradient has fully blended to Color2. This is only applicable when using a Radial gradient.
    /// </summary>
    public float GradientOuterRadius
    {
        get => ContainedRenderable.GradientOuterRadius;
        set => ContainedRenderable.GradientOuterRadius = value;
    }

    /// <summary>
    /// Gets or sets the unit type used to interpret the outer radius of the gradient. This is only applicable when using a Radial gradient.
    /// Supported values are Absolute (pixels), PercentageOfParent (percentage of the shape's Width, so
    /// 100 = Width), and RelativeToParent (pixels offset from the shape's Width, so 0 = Width and
    /// -10 = Width - 10). Note that the shape's natural inscribed radius is Width / 2, so a value of
    /// 50 (PercentageOfParent) or -Width/2 (RelativeToParent) corresponds to that.
    /// </summary>
    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => ContainedRenderable.GradientOuterRadiusUnits;
        set => ContainedRenderable.GradientOuterRadiusUnits = value;
    }

    #endregion

    #region Dropshadow

    /// <summary>
    /// Gets or sets the alpha (opacity) value of the drop shadow effect.
    /// The value range is 0-255.
    /// </summary>
    public int DropshadowAlpha
    {
        get => ContainedRenderable.DropshadowAlpha;
        set => ContainedRenderable.DropshadowAlpha = value;
    }

    /// <summary>
    /// Gets or sets the blue component of the drop shadow color.
    /// The value range is 0-255.
    /// </summary>
    public int DropshadowBlue
    {
        get => ContainedRenderable.DropshadowBlue;
        set => ContainedRenderable.DropshadowBlue = value;
    }

    /// <summary>
    /// Gets or sets the green component of the drop shadow color.
    /// The value range is 0-255.
    /// </summary>
    public int DropshadowGreen
    {
        get => ContainedRenderable.DropshadowGreen;
        set => ContainedRenderable.DropshadowGreen = value;
    }

    /// <summary>
    /// Gets or sets the red component of the drop shadow color.
    /// The value range is 0-255.
    /// </summary>
    public int DropshadowRed
    {
        get => ContainedRenderable.DropshadowRed;
        set => ContainedRenderable.DropshadowRed = value;
    }

    /// <summary>
    /// Gets or sets the color used for the drop shadow effect.
    /// </summary>
    public Color DropshadowColor
    {
        get => new Color(DropshadowRed, DropshadowGreen, DropshadowBlue, DropshadowAlpha);
        set
        {
            DropshadowRed = value.R;
            DropshadowGreen = value.G;
            DropshadowBlue = value.B;
            DropshadowAlpha = value.A;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether a drop shadow is visibile.
    /// </summary>
    public bool HasDropshadow
    {
        get => ContainedRenderable.HasDropshadow;
        set => ContainedRenderable.HasDropshadow = value;
    }

    /// <summary>
    /// Gets or sets the horizontal offset, in pixels, of the drop shadow.
    /// </summary>
    public float DropshadowOffsetX
    {
        get => ContainedRenderable.DropshadowOffsetX;
        set => ContainedRenderable.DropshadowOffsetX = value;
    }

    /// <summary>
    /// Gets or sets the vertical offset, in pixels, of the drop shadow.
    /// </summary>
    public float DropshadowOffsetY
    {
        get => ContainedRenderable.DropshadowOffsetY;
        set => ContainedRenderable.DropshadowOffsetY = value;
    }

    /// <inheritdoc cref="SkiaGum.GueDeriving.SkiaShapeRuntime.DropshadowBlurX"/>
    public float DropshadowBlurX
    {
        get => ContainedRenderable.DropshadowBlurX;
        set => ContainedRenderable.DropshadowBlurX = value;
    }

    /// <inheritdoc cref="SkiaGum.GueDeriving.SkiaShapeRuntime.DropshadowBlurX"/>
    public float DropshadowBlurY
    {
        get => ContainedRenderable.DropshadowBlurY;
        set => ContainedRenderable.DropshadowBlurY = value;
    }

    #endregion

    #region Filled/Stroke

    /// <summary>
    /// Whether the shape is filled (true) or just an outline (false).
    /// </summary>
    public bool IsFilled
    {
        get => ContainedRenderable.IsFilled;
        set => ContainedRenderable.IsFilled = value;
    }

    /// <summary>
    /// Gets or sets the width of the stroke used to draw shapes or lines.
    /// This is only applicable when IsFilled is false.
    /// </summary>
    public float StrokeWidth
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the unit of measurement used for the stroke width. Supported values are
    /// Absolute (StrokeWidth is interpreted as world-space pixels) and ScreenPixel (StrokeWidth
    /// is divided by the camera's zoom each frame so the stroke appears the same size on screen
    /// regardless of camera zoom). When ScreenPixel is selected the same factor is also applied
    /// to <see cref="StrokeDashLength"/> and <see cref="StrokeGapLength"/> so dash patterns scale
    /// with the stroke instead of going out of proportion at non-1.0 zoom.
    /// </summary>
    public DimensionUnitType StrokeWidthUnits
    {
        get;
        set;
    }

    /// <summary>
    /// Length of each dash segment in pixels when stroking a dashed outline. A value of 0 (the
    /// default) produces a solid stroke. Has no effect when <see cref="IsFilled"/> is true.
    /// Like <see cref="StrokeWidth"/> this value is held on the runtime and pushed to the
    /// renderable each frame in <see cref="PreRender"/> so ScreenPixel scaling stays in sync.
    /// </summary>
    public float StrokeDashLength
    {
        get;
        set;
    }

    /// <summary>
    /// Length of the gap between dashes in pixels when stroking a dashed outline. Ignored when
    /// <see cref="StrokeDashLength"/> is 0 or when <see cref="IsFilled"/> is true.
    /// </summary>
    public float StrokeGapLength
    {
        get;
        set;
    }

    /// <summary>
    /// When <c>true</c> (the default) the shape's edges are rendered with 1 px of anti-aliasing,
    /// producing the smooth-edged look Apos.Shapes is normally used for. When <c>false</c> the
    /// AA radius is dropped to 0 and edges rasterize crisply — useful for retro / pixel-art /
    /// 1 px dotted patterns (Win95 focus rectangle, marching ants, hairline borders) where the
    /// AA "bloom" makes a nominal 1 px stroke read as 2-3 px and erodes a 1 px dash/gap pattern.
    /// <para>
    /// This only affects the main shape body's edge. Drop-shadow rendering on the shape (via
    /// <see cref="HasDropshadow"/>) continues to use the larger AA radius derived from
    /// <see cref="DropshadowBlurX"/> — disabling AA on the main edge doesn't kill your shadow.
    /// </para>
    /// </summary>
    public bool IsAntialiased
    {
        get;
        set;
    } = true;

    #endregion



    /// <summary>
    /// Wires <paramref name="shape"/> into this runtime as the contained renderable AND hooks its
    /// PreRender so that <see cref="PreRender"/> on this runtime is actually invoked each frame.
    ///
    /// IMPORTANT: derived ctors must call this instead of <c>SetContainedObject</c> directly. The
    /// renderer adds the renderable (the shape) to the layer, not this runtime, so the renderer's
    /// PreRender walk only reaches the shape's PreRender - never the runtime's. Without this hook,
    /// values that the runtime resolves in PreRender (like unit-bearing StrokeWidth) never reach
    /// the renderable. See the long comment on <see cref="StrokeWidth"/> below.
    /// </summary>
    protected void SetContainedShape(RenderableShapeBase shape)
    {
        SetContainedObject(shape);
        shape.OnPreRender = PreRender;
    }

    /// <summary>
    /// Applies StrokeWidthUnits to the contained renderable's StrokeWidth before rendering.
    ///
    /// DO NOT "simplify" this by turning <see cref="StrokeWidth"/> into a passthrough property
    /// that writes straight to <c>ContainedRenderable.StrokeWidth</c>. The runtime needs to keep
    /// the user-supplied value AND the units (Absolute vs ScreenPixel) so that ScreenPixel can be
    /// re-resolved against the current camera zoom every frame - the camera zoom can change between
    /// frames, so a one-shot push at set time would go stale.
    ///
    /// This override is reached because <see cref="SetContainedShape"/> hooks the renderable's
    /// PreRender to call back into here. (The renderer's PreRender walk only visits renderables in
    /// the layer, not GraphicalUiElement wrappers, so without that hook this method would be dead
    /// code and StrokeWidth would never propagate.)
    /// </summary>
    public override void PreRender()
    {
        var strokeWidth = StrokeWidth;
        var strokeDashLength = StrokeDashLength;
        var strokeGapLength = StrokeGapLength;

        if (StrokeWidthUnits == DimensionUnitType.ScreenPixel)
        {
            // Only ScreenPixel needs the camera zoom; if managers are not yet available
            // (e.g. unit tests, or before the runtime is added to the system) fall back to
            // the unscaled value rather than skipping the push entirely.
            var camera = this.EffectiveManagers?.Renderer?.Camera;
            if (camera != null)
            {
                var zoom = camera.Zoom;
                strokeWidth /= zoom;
                strokeDashLength /= zoom;
                strokeGapLength /= zoom;
            }
        }

        ContainedRenderable.StrokeWidth = strokeWidth;
        ContainedRenderable.StrokeDashLength = strokeDashLength;
        ContainedRenderable.StrokeGapLength = strokeGapLength;
        ContainedRenderable.IsAntialiased = IsAntialiased;

        // NOTE: do NOT call base.PreRender() here. base.PreRender() forwards to
        // mContainedObjectAsIpso.PreRender() (the shape) - and the shape's PreRender is what just
        // called us via OnPreRender. Calling it again would recurse infinitely.
    }
}
