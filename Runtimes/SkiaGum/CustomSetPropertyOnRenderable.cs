using Gum.Converters;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.RenderingLibrary;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#if SKIA
using HarfBuzzSharp;
using SkiaGum.Content;
using SkiaGum.Helpers;
using Gum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
using RenderableShapeBase = SkiaGum.Renderables.RenderableShapeBase;
namespace SkiaGum;
#else
using MonoGameAndGum.Renderables;
using Gum.GueDeriving;
namespace MonoGameGumShapes;

#endif


// partial so the Gum.SilkNet host can supply the localization holder its shared GumService needs
// (Runtimes/SilkNetGum/CustomSetPropertyOnRenderable.Localization.cs) without that state landing in
// the render-only SkiaGum / SkiaGum.Wpf / SkiaGum.Maui / SkiaGum.Standalone consumers (issue #3608).
public partial class CustomSetPropertyOnRenderable
{
    #region Localization

    // Localization holder mirroring the MonoGame/Raylib copies (Gum/Wireframe/CustomSetPropertyOnRenderable.cs
    // and Runtimes/RaylibGum/Renderables/CustomSetPropertyOnRenderable.cs). SkiaGum's copy never grew these,
    // so runtime localization was a no-op in SkiaGum-rendered hosts (SkiaGum.Standalone / WPF / MAUI,
    // Gum.SilkNet) until #3621. Supersedes the inert SilkNet-local holder that #3619 added to satisfy the
    // shared GumService's compile. Nullable-oblivious to match those siblings' un-annotated static fields and
    // to stay valid in consumers with no <Nullable> setting (SkiaGum.Wpf); the shared GumService consumes
    // these through its own nullable-enable context with the appropriate '!' / 'string?' handling.
#nullable disable
    private static ILocalizationService _localizationService;

    /// <summary>
    /// The active localization service used by the runtime. Assigning a new instance fires
    /// <see cref="LocalizationServiceChanged"/> so consumers (e.g. <c>GumService</c>) can re-wire
    /// <see cref="ILocalizationService.CurrentLanguageChanged"/> subscriptions for language switching.
    /// </summary>
    public static ILocalizationService LocalizationService
    {
        get => _localizationService;
        set
        {
            if (ReferenceEquals(_localizationService, value))
            {
                return;
            }
            ILocalizationService previous = _localizationService;
            _localizationService = value;
            LocalizationServiceChanged?.Invoke(previous, value);
        }
    }

    /// <summary>
    /// Raised when <see cref="LocalizationService"/> is replaced. Arguments are
    /// (previousService, newService) — either may be null.
    /// </summary>
    public static event Action<ILocalizationService, ILocalizationService> LocalizationServiceChanged;

    private static readonly ConditionalWeakTable<GraphicalUiElement, string> _localizationKeys = new();

    /// <summary>
    /// Returns the original (pre-translation) string assigned via the localization path on the given
    /// element, or null if no localizable text has been assigned (or it was overwritten via TextNoTranslate).
    /// </summary>
    public static string TryGetLocalizationKey(GraphicalUiElement element)
    {
        return _localizationKeys.TryGetValue(element, out string key) ? key : null;
    }
#nullable restore

    #endregion

    /// <summary>
    /// Additional logic to perform before falling back to reflection.
    /// This can be added by libraries adding additional runtime types.
    /// Mirrors the unified MonoGame/Raylib copy so SkiaGum consumers assigning this hook
    /// (e.g. the Apos.Shapes runtimes) are honored before the reflection fallback.
    /// </summary>
    public static Func<IRenderableIpso, GraphicalUiElement, string, object, bool>? AdditionalPropertyOnRenderable = null;

    // Issue #2956 follow-up — two-slot CircleRuntime / RectangleRuntime own a fill renderable
    // AND a stroke renderable. The runtime's typed setters (UseGradient, IsFilled,
    // gradient color channels, gradient endpoints, etc.) forward to BOTH slots; reflection
    // writes on the contained renderable hit only the fill slot. The dispatcher branches
    // below special-case the properties they know about, but every new runtime property is
    // a chance to forget — and forgetting silently leaves the stroke slot at its default
    // (typically white opaque, which renders as a solid outline regardless of what the user
    // configured).
    //
    // This helper closes the gap: when the GUE is the strongly-typed runtime and the
    // property name resolves to a runtime-declared setter, route through it. If not, return
    // false and let the renderable-side fallback take over. Cost is one PropertyInfo lookup
    // per unhandled property (reflection on the runtime's type, not the renderable's).
    //
    // Visible bug this fixes: UseGradient = true + IsFilled = false in the tool produced a
    // solid stroke instead of a gradient one, because `_stroke.UseGradient` never flipped.
    private static bool TrySetPropertyOnRuntime(GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        System.Reflection.PropertyInfo? pi = graphicalUiElement.GetType().GetProperty(propertyName);
        if (pi == null || !pi.CanWrite)
        {
            return false;
        }
        // Skip properties declared on GraphicalUiElement itself — those are size/position/
        // rotation/etc., already routed via TrySetValueOnThis earlier in the pipeline. We
        // only want to intercept properties added by the typed runtime subclasses (CircleRuntime
        // / RectangleRuntime / SkiaShapeRuntime / etc.).
        if (pi.DeclaringType == typeof(GraphicalUiElement))
        {
            return false;
        }
        Type valueType = value.GetType();
        if (valueType != pi.PropertyType)
        {
            if (valueType == typeof(PositionUnitType) && pi.PropertyType == typeof(GeneralUnitType))
            {
                value = UnitConverter.ConvertToGeneralUnit((PositionUnitType)value);
            }
            else
            {
                try
                {
                    value = System.Convert.ChangeType(value, pi.PropertyType);
                }
                catch
                {
                    return false;
                }
            }
        }
        try
        {
            pi.SetValue(graphicalUiElement, value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TrySetPropertiesOnRenderableBase(RenderableShapeBase renderableBase, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch (propertyName)
        {
            case nameof(RenderableShapeBase.Alpha):
                renderableBase.Alpha = (int)value;
                return true;
            // Skia's Blend? is now handled by core SetPropertyThroughReflection, which converts
            // enum -> Nullable<enum> (issue #2924), so the former SKIA Blend arm here was removed.
            // The Apos.Shapes arm (MonoGameGumShapes / KniGumShapes, issue #2937) is kept: it's a
            // non-nullable Blend on a different renderable surface whose runtime does extra
            // forwarding/translation, so it's left explicit pending a separate audit.
#if !SKIA
            case nameof(RenderableShapeBase.Blend):
                renderableBase.Blend = (Gum.RenderingLibrary.Blend)value;
                return true;
#endif
            case nameof(RenderableShapeBase.Alpha1):
                renderableBase.Alpha1 = (int)value;
                return true;
            case nameof(RenderableShapeBase.Alpha2):
                renderableBase.Alpha2 = (int)value;
                return true;
            case nameof(RenderableShapeBase.Blue):
                renderableBase.Blue = (int)value;
                return true;
            case nameof(RenderableShapeBase.Blue1):
                renderableBase.Blue1 = (int)value;
                return true;
            case nameof(RenderableShapeBase.Blue2):
                renderableBase.Blue2 = (int)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowAlpha):
                renderableBase.DropshadowAlpha = (int)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowBlue):
                renderableBase.DropshadowBlue = (int)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowBlurX):
                renderableBase.DropshadowBlurX = (float)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowBlurY):
                renderableBase.DropshadowBlurY = (float)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowGreen):
                renderableBase.DropshadowGreen = (int)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowOffsetX):
                renderableBase.DropshadowOffsetX = (float)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowOffsetY):
                renderableBase.DropshadowOffsetY = (float)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowRed):
                renderableBase.DropshadowRed = (int)value;
                return true;
            case nameof(RenderableShapeBase.GradientInnerRadius):
                renderableBase.GradientInnerRadius = (float)value;
                return true;
            case nameof(RenderableShapeBase.GradientInnerRadiusUnits):
                renderableBase.GradientInnerRadiusUnits = (DimensionUnitType)value;
                return true;
            case nameof(RenderableShapeBase.GradientOuterRadius):
                renderableBase.GradientOuterRadius = (float)value;
                return true;
            case nameof(RenderableShapeBase.GradientOuterRadiusUnits):
                renderableBase.GradientOuterRadiusUnits = (DimensionUnitType)value;
                return true;
            case nameof(RenderableShapeBase.GradientType):
                renderableBase.GradientType = (GradientType)value;
                return true;
            case nameof(RenderableShapeBase.GradientX1):
                renderableBase.GradientX1 = (float)value;
                return true;
            case nameof(RenderableShapeBase.GradientX1Units):
                {
                    if (value is PositionUnitType positionUnitType)
                    {
                        renderableBase.GradientX1Units = UnitConverter.ConvertToGeneralUnit(positionUnitType);
                    }
                    else
                    {
                        renderableBase.GradientX1Units = (GeneralUnitType)value;
                    }
                }
                return true;
            case nameof(RenderableShapeBase.GradientX2):
                renderableBase.GradientX2 = (float)value;
                return true;
            case nameof(RenderableShapeBase.GradientX2Units):
                {
                    if (value is PositionUnitType positionUnitType)
                    {
                        renderableBase.GradientX2Units = UnitConverter.ConvertToGeneralUnit(positionUnitType);
                    }
                    else
                    {
                        renderableBase.GradientX2Units = (GeneralUnitType)value;
                    }
                }
                return true;
            case nameof(RenderableShapeBase.GradientY1):
                renderableBase.GradientY1 = (float)value;
                return true;
            case nameof(RenderableShapeBase.GradientY1Units):
                {
                    if(value is PositionUnitType positionUnitType)
                    {
                        renderableBase.GradientY1Units = UnitConverter.ConvertToGeneralUnit(positionUnitType);
                    }
                    else
                    {
                        renderableBase.GradientY1Units = (GeneralUnitType)value;
                    }
                }
                return true;
            case nameof(RenderableShapeBase.GradientY2):
                renderableBase.GradientY2 = (float)value;
                return true;
            case nameof(RenderableShapeBase.GradientY2Units):
                {
                    if(value is PositionUnitType positionUnitType)
                    {
                        renderableBase.GradientY2Units = UnitConverter.ConvertToGeneralUnit(positionUnitType);
                    }
                    else
                    {
                        renderableBase.GradientY2Units = (GeneralUnitType)value;
                    }
                }

                return true;
            case nameof(RenderableShapeBase.Green):
                renderableBase.Green = (int)value;
                return true;
            case nameof(RenderableShapeBase.Green1):
                renderableBase.Green1 = (int)value;
                return true;
            case nameof(RenderableShapeBase.Green2):
                renderableBase.Green2 = (int)value;
                return true;
            case nameof(RenderableShapeBase.HasDropshadow):
                renderableBase.HasDropshadow = (bool)value;
                return true;
            case nameof(RenderableShapeBase.IsFilled):
                renderableBase.IsFilled = (bool)value;
                return true;
            case nameof(RenderableShapeBase.Red):
                renderableBase.Red = (int)value;
                return true;
            case nameof(RenderableShapeBase.Red1):
                renderableBase.Red1 = (int)value;
                return true;
            case nameof(RenderableShapeBase.Red2):
                renderableBase.Red2 = (int)value;
                return true;
            case nameof(RenderableShapeBase.StrokeWidth):
                renderableBase.StrokeWidth = (float)value;
                return true;
            case nameof(RenderableShapeBase.StrokeDashLength):
                renderableBase.StrokeDashLength = (float)value;
                return true;
            case nameof(RenderableShapeBase.StrokeGapLength):
                renderableBase.StrokeGapLength = (float)value;
                return true;
            case nameof(RenderableShapeBase.UseGradient):
                renderableBase.UseGradient = (bool)value;
                return true;
                // todo - more here...
        }
        return false;
    }

    private static bool TrySetPropertyOnArc(Arc asArc, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch(propertyName)
        {
            case nameof(Arc.Thickness):
                if (graphicalUiElement is ArcRuntime arcRuntime)
                {
                    arcRuntime.Thickness = (float)value;
                }
                else
                {
                    asArc.Thickness = (float)value;
                }
                return true;
        }
        return false;
    }

    private static bool TrySetPropertyOnLine(Line asLine, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch (propertyName)
        {
            case nameof(Line.IsRounded):
                asLine.IsRounded = (bool)value;
                return true;
        }
        return false;
    }

    private static bool TrySetPropertyOnRoundedRectangle(RoundedRectangle asRoundedRectangle, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch (propertyName) 
        {
            case nameof(RoundedRectangle.CornerRadius):
                asRoundedRectangle.CornerRadius = (float)value;
                return true;
        }

        return false;
    }

    // todo - fill this out for the sake of performance...
    // If it's not filled out, then properties will be set by reflection
    // It would be nice to share this with MonoGame at some point too so 
    // we don't have to keep everything in duplicate
    // RoundedRectangleRuntime is obsolete and slated for removal (superseded by RectangleRuntime).
    // Its property dispatch is isolated here so the removal is a clean delete of this method and its
    // single call site in the RoundedRectangle branch, rather than unpicking arms interleaved with
    // RectangleRuntime inside that branch's switch.
    private static bool TrySetPropertyOnRoundedRectangleRuntime(GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        if (graphicalUiElement is not RoundedRectangleRuntime asRoundedRectangleRuntime)
        {
            return false;
        }

        switch (propertyName)
        {
            case nameof(RoundedRectangleRuntime.StrokeWidth):
                asRoundedRectangleRuntime.StrokeWidth = (float)value;
                return true;
            case nameof(RoundedRectangleRuntime.StrokeDashLength):
                asRoundedRectangleRuntime.StrokeDashLength = (float)value;
                return true;
            case nameof(RoundedRectangleRuntime.StrokeGapLength):
                asRoundedRectangleRuntime.StrokeGapLength = (float)value;
                return true;
        }

        return false;
    }

    // ColoredCircleRuntime is obsolete and slated for removal (superseded by CircleRuntime).
    // Its property dispatch is isolated here so the removal is a clean delete of this method and its
    // single call site in the Circle branch, rather than unpicking arms interleaved with
    // CircleRuntime inside that branch's switch.
    private static bool TrySetPropertyOnColoredCircleRuntime(GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        if (graphicalUiElement is not ColoredCircleRuntime asColoredCircleRuntime)
        {
            return false;
        }

        switch (propertyName)
        {
            case nameof(ColoredCircleRuntime.StrokeWidth):
                asColoredCircleRuntime.StrokeWidth = (float)value;
                return true;
            case nameof(ColoredCircleRuntime.StrokeDashLength):
                asColoredCircleRuntime.StrokeDashLength = (float)value;
                return true;
            case nameof(ColoredCircleRuntime.StrokeGapLength):
                asColoredCircleRuntime.StrokeGapLength = (float)value;
                return true;
        }

        return false;
    }

    public static void SetPropertyOnRenderable(IRenderableIpso containedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value) =>
        SetPropertyOnRenderableFunc(containedObjectAsIpso, graphicalUiElement, propertyName, value);

    public static bool SetPropertyOnRenderableFunc(IRenderableIpso containedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;
        if(containedObjectAsIpso is Arc asArc)
        {
            // Mirror RoundedRectangle/Line/ColoredCircle: stroke values must land on the runtime so
            // AposShapeRuntime.PreRender (which pushes the runtime values to the renderable each
            // frame, applying ScreenPixel zoom) does not clobber a value written straight to the
            // renderable. See issue #2629.
            switch (propertyName)
            {
                case nameof(ArcRuntime.StrokeWidth):
                    if (graphicalUiElement is ArcRuntime arcStrokeRuntime)
                    {
                        arcStrokeRuntime.StrokeWidth = (float)value;
                    }
                    else
                    {
                        asArc.StrokeWidth = (float)value;
                    }
                    handled = true;
                    break;
                case nameof(ArcRuntime.StrokeDashLength):
                    if (graphicalUiElement is ArcRuntime arcDashRuntime)
                    {
                        arcDashRuntime.StrokeDashLength = (float)value;
                    }
                    else
                    {
                        asArc.StrokeDashLength = (float)value;
                    }
                    handled = true;
                    break;
                case nameof(ArcRuntime.StrokeGapLength):
                    if (graphicalUiElement is ArcRuntime arcGapRuntime)
                    {
                        arcGapRuntime.StrokeGapLength = (float)value;
                    }
                    else
                    {
                        asArc.StrokeGapLength = (float)value;
                    }
                    handled = true;
                    break;
                // #2949: the single isotropic DropshadowBlur variable must land on the runtime so
                // its DropshadowBlur setter fans out to both per-axis shims (which SkiaShapeRuntime
                // then pushes to the renderable in PreRender). Writing straight to the renderable
                // would be clobbered by that PreRender push.
                case nameof(ArcRuntime.DropshadowBlur):
                    if (graphicalUiElement is ArcRuntime arcDropshadowBlurRuntime)
                    {
                        arcDropshadowBlurRuntime.DropshadowBlur = (float)value;
                    }
                    else
                    {
                        asArc.DropshadowBlurX = (float)value;
                        asArc.DropshadowBlurY = (float)value;
                    }
                    handled = true;
                    break;
                // Issue #3009 — Arc maps the legacy gradient-start channels (Red1/Green1/Blue1/
                // Alpha1) onto the primary Color (the unified "gradient start = body color" model).
                // Old .gumx data set these independently; route them to the primary so they load
                // onto Color. Gum applies variables alphabetically, so the …1 channels apply after
                // the solid ones and win — the accepted lossy case (UseGradient = false with an
                // explicitly-different Color1) is pinned by ArcRuntimeTests. PreRender then mirrors
                // the primary into the renderable's gradient-start channels for the gradient pass.
                case nameof(RenderableShapeBase.Red1):
                    asArc.Red = (int)value;
                    handled = true;
                    break;
                case nameof(RenderableShapeBase.Green1):
                    asArc.Green = (int)value;
                    handled = true;
                    break;
                case nameof(RenderableShapeBase.Blue1):
                    asArc.Blue = (int)value;
                    handled = true;
                    break;
                case nameof(RenderableShapeBase.Alpha1):
                    asArc.Alpha = (int)value;
                    handled = true;
                    break;
            }
            if (!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asArc, graphicalUiElement, propertyName, value);
            }
            if(!handled)
            {
                handled = TrySetPropertyOnArc(asArc, graphicalUiElement, propertyName, value);
            }
        }
        else if(containedObjectAsIpso is RoundedRectangle asRoundedRectangle)
        {
            // RoundedRectangleRuntime is obsolete; its dispatch is isolated in a dedicated method,
            // checked first, so its eventual removal is a clean delete of that method + this call.
            handled = TrySetPropertyOnRoundedRectangleRuntime(graphicalUiElement, propertyName, value);

            // some properties have priority on the base shape itself:
            switch (propertyName)
            {
                // #2931: same as the Circle branch — IsFilled gates two-slot fill visibility
                // on the runtime; pushing to the renderable would flip Apos's shader mode on
                // the fill RoundedRectangle without actually hiding/showing the fill.
                case nameof(RectangleRuntime.IsFilled):
                    if (graphicalUiElement is RectangleRuntime rectIsFilled)
                    {
                        rectIsFilled.IsFilled = (bool)value;
                        handled = true;
                        break;
                    }
                    break;
                // #2931: same rationale as the Circle branch above.
                case nameof(RectangleRuntime.FillRed):
                    if (graphicalUiElement is RectangleRuntime rectFillRed)
                    {
                        rectFillRed.FillRed = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.FillGreen):
                    if (graphicalUiElement is RectangleRuntime rectFillGreen)
                    {
                        rectFillGreen.FillGreen = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.FillBlue):
                    if (graphicalUiElement is RectangleRuntime rectFillBlue)
                    {
                        rectFillBlue.FillBlue = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.FillAlpha):
                    if (graphicalUiElement is RectangleRuntime rectFillAlpha)
                    {
                        rectFillAlpha.FillAlpha = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.StrokeRed):
                    if (graphicalUiElement is RectangleRuntime rectStrokeRed)
                    {
                        rectStrokeRed.StrokeRed = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.StrokeGreen):
                    if (graphicalUiElement is RectangleRuntime rectStrokeGreen)
                    {
                        rectStrokeGreen.StrokeGreen = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.StrokeBlue):
                    if (graphicalUiElement is RectangleRuntime rectStrokeBlue)
                    {
                        rectStrokeBlue.StrokeBlue = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.StrokeAlpha):
                    if (graphicalUiElement is RectangleRuntime rectStrokeAlpha)
                    {
                        rectStrokeAlpha.StrokeAlpha = (int)value;
                        handled = true;
                    }
                    break;
                // Same rationale as the Circle branch above — see comment there.
                case nameof(RectangleRuntime.HasDropshadow):
                    if (graphicalUiElement is RectangleRuntime rectHasDs)
                    {
                        rectHasDs.HasDropshadow = (bool)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.DropshadowOffsetX):
                    if (graphicalUiElement is RectangleRuntime rectDsOffX)
                    {
                        rectDsOffX.DropshadowOffsetX = (float)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.DropshadowOffsetY):
                    if (graphicalUiElement is RectangleRuntime rectDsOffY)
                    {
                        rectDsOffY.DropshadowOffsetY = (float)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.DropshadowBlur):
                    if (graphicalUiElement is RectangleRuntime rectDsBlur)
                    {
                        rectDsBlur.DropshadowBlur = (float)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.DropshadowAlpha):
                    if (graphicalUiElement is RectangleRuntime rectDsA)
                    {
                        rectDsA.DropshadowAlpha = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.DropshadowRed):
                    if (graphicalUiElement is RectangleRuntime rectDsR)
                    {
                        rectDsR.DropshadowRed = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.DropshadowGreen):
                    if (graphicalUiElement is RectangleRuntime rectDsG)
                    {
                        rectDsG.DropshadowGreen = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.DropshadowBlue):
                    if (graphicalUiElement is RectangleRuntime rectDsB)
                    {
                        rectDsB.DropshadowBlue = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.StrokeWidth):
                    if (graphicalUiElement is RectangleRuntime rectStrokeWidth)
                    {
                        // #2931: plain RectangleRuntime now owns StrokeWidth; route through the
                        // runtime so PreRender's ScreenPixel-zoom scaling resolves against the
                        // latest user value.
                        rectStrokeWidth.StrokeWidth = (float)value;
                        handled = true;
                    }
                    else if (!handled)
                    {
                        asRoundedRectangle.StrokeWidth = (float)value;
                        handled = true;
                    }
                    break;
                // Mirror StrokeWidth routing: dashed-stroke values live on the runtime so the Apos
                // ScreenPixel-scaling pass in PreRender stays consistent with StrokeWidth. Skia's
                // runtime is a passthrough setter so this path produces the same end state there.
                case nameof(RectangleRuntime.StrokeDashLength):
                    if (graphicalUiElement is RectangleRuntime rectDash)
                    {
                        rectDash.StrokeDashLength = (float)value;
                        handled = true;
                    }
                    else if (!handled)
                    {
                        asRoundedRectangle.StrokeDashLength = (float)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.StrokeGapLength):
                    if (graphicalUiElement is RectangleRuntime rectGap)
                    {
                        rectGap.StrokeGapLength = (float)value;
                        handled = true;
                    }
                    else if (!handled)
                    {
                        asRoundedRectangle.StrokeGapLength = (float)value;
                        handled = true;
                    }
                    break;
                // Issue #2720: route CornerRadius and per-corner radii to RectangleRuntime when
                // that's the GUE. RectangleRuntime stores these on the runtime and mirrors to
                // fill+stroke slots in its setter (plus Skia re-pushes them each frame in
                // PreRender for ScreenPixel scaling). Writing them straight to the renderable
                // would be clobbered the next time the runtime touched the property — or, on
                // Skia, on the very next frame. RoundedRectangleRuntime is frozen and does not
                // need its own arm (no new functionality lands on it).
                case nameof(RectangleRuntime.CornerRadius):
                    if (graphicalUiElement is RectangleRuntime rectCornerRuntime)
                    {
                        rectCornerRuntime.CornerRadius = (float)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.CustomRadiusTopLeft):
                    if (graphicalUiElement is RectangleRuntime rectTLRuntime)
                    {
                        rectTLRuntime.CustomRadiusTopLeft = (float?)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.CustomRadiusTopRight):
                    if (graphicalUiElement is RectangleRuntime rectTRRuntime)
                    {
                        rectTRRuntime.CustomRadiusTopRight = (float?)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.CustomRadiusBottomLeft):
                    if (graphicalUiElement is RectangleRuntime rectBLRuntime)
                    {
                        rectBLRuntime.CustomRadiusBottomLeft = (float?)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.CustomRadiusBottomRight):
                    if (graphicalUiElement is RectangleRuntime rectBRRuntime)
                    {
                        rectBRRuntime.CustomRadiusBottomRight = (float?)value;
                        handled = true;
                    }
                    break;
            }
            // Issue #2956 follow-up — see the Circle branch above for the rationale.
            if (!handled)
            {
                handled = TrySetPropertyOnRuntime(graphicalUiElement, propertyName, value);
            }
            if (!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asRoundedRectangle, graphicalUiElement, propertyName, value);
            }
            if(!handled)
            {
                handled = TrySetPropertyOnRoundedRectangle(asRoundedRectangle, graphicalUiElement, propertyName, value);
            }
        }
        else if (containedObjectAsIpso is Line asLine)
        {
            switch (propertyName)
            {
                case nameof(LineRuntime.StrokeWidth):
                    if (graphicalUiElement is LineRuntime asLineRuntime)
                    {
                        asLineRuntime.StrokeWidth = (float)value;
                    }
                    else
                    {
                        asLine.StrokeWidth = (float)value;
                    }
                    handled = true;
                    break;
                case nameof(LineRuntime.StrokeDashLength):
                    if (graphicalUiElement is LineRuntime lineDashRuntime)
                    {
                        lineDashRuntime.StrokeDashLength = (float)value;
                    }
                    else
                    {
                        asLine.StrokeDashLength = (float)value;
                    }
                    handled = true;
                    break;
                case nameof(LineRuntime.StrokeGapLength):
                    if (graphicalUiElement is LineRuntime lineGapRuntime)
                    {
                        lineGapRuntime.StrokeGapLength = (float)value;
                    }
                    else
                    {
                        asLine.StrokeGapLength = (float)value;
                    }
                    handled = true;
                    break;
            }
            if (!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asLine, graphicalUiElement, propertyName, value);
            }
            if (!handled)
            {
                handled = TrySetPropertyOnLine(asLine, graphicalUiElement, propertyName, value);
            }
        }
        else if (containedObjectAsIpso is Circle asCircle)
        {
            handled = TrySetPropertyOnColoredCircleRuntime(graphicalUiElement, propertyName, value);

            if(graphicalUiElement is CircleRuntime circleRuntime)
            {
                switch (propertyName)
                {
                    case "Radius":
                        var radius = (float)value;
                        graphicalUiElement.Width = radius * 2;
                        graphicalUiElement.Height = radius * 2;
                        handled = true;
                        break;
                }
            }

            // Stroke values must land on the runtime (not the renderable) so PreRender's
            // ScreenPixel-zoom scaling resolves against the latest user value. Plain
            // CircleRuntime joined ColoredCircleRuntime in exposing these in #2931, so
            // dispatch on the actual GUE type rather than hard-casting.
            switch(propertyName)
            {
                // #2931: IsFilled on plain CircleRuntime gates the runtime's two-slot fill
                // visibility (fires the FillColor-or-transparent push). Setting the fill
                // renderable's own IsFilled instead — what TrySetPropertiesOnRenderableBase
                // does — flips Apos's shader mode on the fill Circle, which is a different
                // axis and does NOT toggle fill visibility. Intercept here.
                case nameof(CircleRuntime.IsFilled):
                    if (graphicalUiElement is CircleRuntime cIsFilled)
                    {
                        cIsFilled.IsFilled = (bool)value;
                        handled = true;
                    }
                    break;
                // #2931: FillRed/Green/Blue/Alpha + StrokeRed/Green/Blue/Alpha live on the
                // runtime, not on the Apos Circle renderable. Without these arms the dispatcher
                // would fall through to SetPropertyThroughReflection on the renderable, find
                // no matching property, and silently leave the fill at (0,0,0,0).
                case nameof(CircleRuntime.FillRed):
                    if (graphicalUiElement is CircleRuntime cFillRed)
                    {
                        cFillRed.FillRed = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.FillGreen):
                    if (graphicalUiElement is CircleRuntime cFillGreen)
                    {
                        cFillGreen.FillGreen = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.FillBlue):
                    if (graphicalUiElement is CircleRuntime cFillBlue)
                    {
                        cFillBlue.FillBlue = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.FillAlpha):
                    if (graphicalUiElement is CircleRuntime cFillAlpha)
                    {
                        cFillAlpha.FillAlpha = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.StrokeRed):
                    if (graphicalUiElement is CircleRuntime cStrokeRed)
                    {
                        cStrokeRed.StrokeRed = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.StrokeGreen):
                    if (graphicalUiElement is CircleRuntime cStrokeGreen)
                    {
                        cStrokeGreen.StrokeGreen = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.StrokeBlue):
                    if (graphicalUiElement is CircleRuntime cStrokeBlue)
                    {
                        cStrokeBlue.StrokeBlue = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.StrokeAlpha):
                    if (graphicalUiElement is CircleRuntime cStrokeAlpha)
                    {
                        cStrokeAlpha.StrokeAlpha = (int)value;
                        handled = true;
                    }
                    break;
                // Dropshadow names must route through the runtime so SyncDropshadowToTarget
                // can place the shadow on the active slot (fill when IsFilled = true, stroke
                // otherwise). Without this routing, state-load writes straight to the fill
                // renderable via TrySetPropertiesOnRenderableBase and the shadow is stranded
                // on the gated-transparent fill slot — EffectiveDropshadowColor scales alpha
                // by Color.A so a transparent fill produces an invisible shadow.
                case nameof(CircleRuntime.HasDropshadow):
                    if (graphicalUiElement is CircleRuntime cHasDs)
                    {
                        cHasDs.HasDropshadow = (bool)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.DropshadowOffsetX):
                    if (graphicalUiElement is CircleRuntime cDsOffX)
                    {
                        cDsOffX.DropshadowOffsetX = (float)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.DropshadowOffsetY):
                    if (graphicalUiElement is CircleRuntime cDsOffY)
                    {
                        cDsOffY.DropshadowOffsetY = (float)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.DropshadowBlur):
                    if (graphicalUiElement is CircleRuntime cDsBlur)
                    {
                        cDsBlur.DropshadowBlur = (float)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.DropshadowAlpha):
                    if (graphicalUiElement is CircleRuntime cDsA)
                    {
                        cDsA.DropshadowAlpha = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.DropshadowRed):
                    if (graphicalUiElement is CircleRuntime cDsR)
                    {
                        cDsR.DropshadowRed = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.DropshadowGreen):
                    if (graphicalUiElement is CircleRuntime cDsG)
                    {
                        cDsG.DropshadowGreen = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.DropshadowBlue):
                    if (graphicalUiElement is CircleRuntime cDsB)
                    {
                        cDsB.DropshadowBlue = (int)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.StrokeWidth):
                    if (graphicalUiElement is CircleRuntime cStrokeWidth)
                    {
                        cStrokeWidth.StrokeWidth = (float)value;
                        handled = true;
                    }
                    else if (!handled)
                    {
                        asCircle.StrokeWidth = (float)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.StrokeDashLength):
                    if (graphicalUiElement is CircleRuntime cDash)
                    {
                        cDash.StrokeDashLength = (float)value;
                        handled = true;
                    }
                    else if (!handled)
                    {
                        asCircle.StrokeDashLength = (float)value;
                        handled = true;
                    }
                    break;
                case nameof(CircleRuntime.StrokeGapLength):
                    if (graphicalUiElement is CircleRuntime cGap)
                    {
                        cGap.StrokeGapLength = (float)value;
                        handled = true;
                    }
                    else if (!handled)
                    {
                        asCircle.StrokeGapLength = (float)value;
                        handled = true;
                    }
                    break;
            }

            // Issue #2956 follow-up — try the runtime first so any runtime-declared property
            // (UseGradient, gradient endpoints, gradient color channels, etc.) routes through
            // the runtime's typed setter, which forwards to both slots. Falling straight to
            // TrySetPropertiesOnRenderableBase would write to the fill slot only.
            if (!handled)
            {
                handled = TrySetPropertyOnRuntime(graphicalUiElement, propertyName, value);
            }
            if(!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asCircle, graphicalUiElement, propertyName, value);
            }
            if (!handled)
            {
                // todo - if there end up being circle-specific properties, set them here
                //handled = TrySetPropertyOnRoundedRectangle(asRoundedRectangle, graphicalUiElement, propertyName, value);
            }
        }
#if SKIA
        else if (containedObjectAsIpso is Text asText)
        {
            handled = TrySetPropertyOnText(asText, graphicalUiElement, propertyName, value);
        }
        else if(containedObjectAsIpso is VectorSprite asSvg)
        {
            // VectorSprite is not a RenderableShapeBase, so it stays on its dedicated handler
            // plus the reflection fallback. No shared shape-base properties (Blend, etc.) apply.
            if(!handled)
            {
                handled = TrySetPropertyOnSvg(asSvg, graphicalUiElement, propertyName, value);
            }
        }
        else if(containedObjectAsIpso is Sprite asSprite)
        {
            // Route base-class properties (Blend, gradient/dropshadow channels, etc.) first.
            // These branches historically skipped TrySetPropertiesOnRenderableBase and relied
            // on the reflection fallback, which works for int/float properties but throws on
            // enum -> Nullable<enum> (the Blend case).
            if(!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asSprite, graphicalUiElement, propertyName, value);
            }
            if(!handled)
            {
                handled = TrySetPropertyOnSprite(asSprite, graphicalUiElement, propertyName, value);
            }
        }
        else if(containedObjectAsIpso is NineSlice asNineSlice)
        {
            if(!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asNineSlice, graphicalUiElement, propertyName, value);
            }
            if(!handled)
            {
                handled = TrySetPropertyOnNineSlice(asNineSlice, graphicalUiElement, propertyName, value);
            }
        }
        else if(containedObjectAsIpso is Polygon asPolygon)
        {
            if(!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asPolygon, graphicalUiElement, propertyName, value);
            }
            if(!handled)
            {
                handled = TrySetPropertyOnPolygon(asPolygon, graphicalUiElement, propertyName, value);
            }
        }
        // RenderableShapeBase derivatives with no dedicated branch above (SolidRectangle,
        // LineRectangle, LineGrid, etc.) fall through to the reflection helper below. Core
        // SetPropertyThroughReflection now converts enum -> Nullable<enum> (issue #2924), so the
        // #2923 catch-all that routed them through TrySetPropertiesOnRenderableBase purely to
        // handle Blend? is no longer needed.
        if (!handled && AdditionalPropertyOnRenderable != null)
        {
            handled = AdditionalPropertyOnRenderable(containedObjectAsIpso, graphicalUiElement, propertyName, value);
        }

        if (!handled)
        {
            GraphicalUiElement.SetPropertyThroughReflection(containedObjectAsIpso, graphicalUiElement, propertyName, value);
            //SetPropertyOnRenderable(mContainedObjectAsIpso, propertyName, value);
        }
#endif

        return handled;
    }

#if SKIA

    private static bool TrySetPropertyOnPolygon(Polygon asPolygon, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch(propertyName)
        {
            case "Points":
                if(value is List<System.Numerics.Vector2> vector2s)
                {
                    var toAssign = new List<SKPoint>();
                    toAssign.AddRange(vector2s.Select(item => new SKPoint(item.X, item.Y)));
                    asPolygon.Points = toAssign;
                    return true;
                }
                //asPolygon.Points = ;
                break;
        }
        return false;
    }

    private static bool TrySetPropertyOnNineSlice(NineSlice asNineSlice, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch(propertyName)
        {
            case "SourceFile":
                var asString = value as string;
                if (!string.IsNullOrEmpty(asString))
                {
                    var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                    var contentLoader = loaderManager.ContentLoader;
                    asNineSlice.Texture = contentLoader.LoadContent<SKBitmap>(asString);
                }
                else
                {
                    asNineSlice.Texture = null;
                }
                break;
        }
        return false;
    }

    private static bool TrySetPropertyOnSprite(Sprite asSprite, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch(propertyName)
        {
            case "SourceFile":
                var asString = value as string;
                if(graphicalUiElement is SpriteRuntime spriteRuntime)
                {
                    spriteRuntime.SourceFile = asString;
                }
                else if (!string.IsNullOrEmpty(asString))
                {
                    var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                    var contentLoader = loaderManager.ContentLoader;
                    var image = contentLoader.LoadContent<SKBitmap>(asString);
                    asSprite.Texture = image;
                }
                else
                {
                    asSprite.Texture = null;
                }
                break;
        }
        return false;
    }

    private static bool TrySetPropertyOnSvg(VectorSprite asSvg, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch(propertyName) 
        {
            case "SourceFile":
                var asString = value as string;

                if(!string.IsNullOrEmpty(asString))
                {
                    asSvg.Texture = SkiaResourceManager.GetSvg(asString);
                }
                else
                {
                    asSvg.Texture = null;
                }
                break;
        }
        return false;
    }

    private static bool TrySetPropertyOnText(Text text, GraphicalUiElement gue, string propertyName, object value)
    {
        bool handled = false;

        var gueAsTextRuntime = gue as TextRuntime;

        void ReactToFontValueChange()
        {
            gue.UpdateToFontValues();
            // we want to update if the text's size is based on its "children" (the letters it contains)
            if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                gue.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                gue.UpdateLayout();
            }
            handled = true;
        }

        if (propertyName == "Text" || propertyName == "TextNoTranslate")
        {
            if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                gue.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                // make it have no line wrap width before assignign the text:
                text.Width = 0;
            }

            var valueAsString = value as string;

            // Track the original (untranslated) value so a language switch can re-translate this
            // element via TryGetLocalizationKey; "TextNoTranslate" always clears the tracked key so
            // user input / explicit literals aren't re-translated later. Mirrors the MonoGame/Raylib
            // copies (Gum/Wireframe/CustomSetPropertyOnRenderable.cs).
            if (propertyName == "TextNoTranslate")
            {
                _localizationKeys.Remove(gue);
            }
            else if (LocalizationService != null && valueAsString != null)
            {
                _localizationKeys.AddOrUpdate(gue, valueAsString);
            }
            else
            {
                _localizationKeys.Remove(gue);
            }

            var rawText = valueAsString;
            if (LocalizationService != null && propertyName == "Text")
            {
                rawText = LocalizationService.Translate(rawText);
            }

            // SkiaGum honors BBCode inline styling too (issue #3679), but unlike the MonoGame copy
            // (which strips tags here via SetBbCodeText and stores the markup in StoredMarkupText),
            // the Skia Text renderable parses the markup lazily when it builds its RichTextKit
            // TextBlock. So the raw (possibly translated) value flows straight through; a value
            // containing [Color=..]/[FontSize=..]/bold/italic tags renders as mixed per-run styling.
            text.RawText = rawText;
            // we want to update if the text's size is based on its "children" (the letters it contains)
            if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                gue.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                gue.UpdateLayout();
            }
            handled = true;
        }
        else if (propertyName == "Font Scale" || propertyName == "FontScale")
        {
            // Redispatched onto TextRuntime (issue #3706/ADR 0010, mirroring the core dispatcher's
            // FontScale arm). TextRuntime.FontScale's own setter already calls UpdateLayout() on
            // change, so the manual RelativeToChildren check this used to duplicate is redundant.
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.FontScale = (float)value;
            }
            handled = true;
        }
        else if (propertyName == "Font")
        {
            if(gueAsTextRuntime != null)
            {
                gueAsTextRuntime.Font = value as string;
            }

            ReactToFontValueChange();
        }
        // #3670/#3703: these had no dispatch arm at all, so setting either by the SetProperty/string
        // path (state application, codegen, BBCode) was a silent no-op on Skia -- unlike XNA-like/raylib,
        // which have had this arm since CustomFontFile existed.
        else if (propertyName == nameof(gueAsTextRuntime.UseCustomFont))
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.UseCustomFont = (bool)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(gueAsTextRuntime.CustomFontFile))
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.CustomFontFile = value as string;
            }
            ReactToFontValueChange();
        }
        // Typeface (#3708): an explicit SKTypeface override. Unlike UseCustomFont/CustomFontFile
        // above, this does NOT call ReactToFontValueChange() -- that re-resolves via
        // UpdateToFontValues() (the normal FontName/FontSize path), which would immediately stomp
        // an explicit override. The property setter itself invalidates the cached RichTextKit
        // block; only a pending relative-to-children layout still needs an explicit nudge here.
        else if (propertyName == nameof(gueAsTextRuntime.Typeface))
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.Typeface = value as SkiaSharp.SKTypeface;
            }
            if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                gue.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                gue.UpdateLayout();
            }
            handled = true;
        }


        else if (propertyName == nameof(gueAsTextRuntime.FontSize))
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.FontSize = (int)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(gueAsTextRuntime.OutlineThickness))
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.OutlineThickness = (int)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(gueAsTextRuntime.IsItalic))
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.IsItalic = (bool)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(gueAsTextRuntime.IsBold))
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.IsBold = (bool)value;
            }
            ReactToFontValueChange();
        }
#if !SKIA
        else if (propertyName == nameof(gueAsTextRuntime.UseFontSmoothing))
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.UseFontSmoothing = (bool)value;
            }
            ReactToFontValueChange();
        }
#endif
        else if (propertyName == nameof(Blend))
        {
#if SKIA
            // Mirror of the XNALIKE Blend arm in Gum/Wireframe/CustomSetPropertyOnRenderable.cs,
            // which sets textRenderable.BlendState. Skia has no BlendState; it applies the Gum
            // Blend as an SKPaint.BlendMode at render time (see Text.GetRenderPaint), so the same
            // dispatch just assigns the nullable Blend property. (issue #3676)
            // Redispatched onto TextRuntime (issue #3706/ADR 0010): TextRuntime.Blend already
            // forwards to ContainedText.Blend, so this is a structural no-op, not a behavior change.
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.Blend = (Gum.RenderingLibrary.Blend)value;
            }
            handled = true;
#endif
        }
        else if (propertyName == "Alpha")
        {
#if MONOGAME || XNA4
            int valueAsInt = (int)value;
            ((Text)mContainedObjectAsIpso).Alpha = valueAsInt;
            handled = true;
#endif
            // Issue #3706/ADR 0010: previously had no live dispatch arm at all (the block above is
            // dead -- this method only compiles under SKIA). Worked by accident via the reflection
            // fallback (SkiaGum.Renderables.Text.Alpha is `int`, an exact type match), so this is a
            // consistency/perf fix, not a behavior change.
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.Alpha = (int)value;
            }
            handled = true;
        }
        else if (propertyName == "Red")
        {
            int valueAsInt = (int)value;
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.Red = valueAsInt;
            }
            handled = true;
        }
        else if (propertyName == "Green")
        {
            int valueAsInt = (int)value;
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.Green = valueAsInt;
            }
            handled = true;
        }
        else if (propertyName == "Blue")
        {
            int valueAsInt = (int)value;
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.Blue = valueAsInt;
            }
            handled = true;
        }
        else if (propertyName == "Color")
        {
#if MONOGAME || XNA4
            var valueAsColor = (Color)value;
            ((Text)mContainedObjectAsIpso).Color = valueAsColor;
            handled = true;
#endif
            // Issue #3706/ADR 0010: this dispatch arm was dead (see the Alpha comment above), and
            // unlike Alpha, Color was a genuine bug -- SkiaGum.Renderables.Text.Color is SKColor, and
            // SetProperty("Color", ...) passes a boxed System.Drawing.Color, so the reflection
            // fallback's Convert.ChangeType threw internally and silently swallowed the assignment.
            if (gueAsTextRuntime != null && value is System.Drawing.Color drawingColor)
            {
                gueAsTextRuntime.Color = drawingColor.ToSkia();
            }
            handled = true;
        }

        else if (propertyName == "HorizontalAlignment")
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.HorizontalAlignment = (RenderingLibrary.Graphics.HorizontalAlignment)value;
            }
            handled = true;
        }
        else if (propertyName == "VerticalAlignment")
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.VerticalAlignment = (VerticalAlignment)value;
            }
            handled = true;
        }
        else if (propertyName == "MaxLettersToShow")
        {
#if SKIA
            // Mirror of the XNALIKE MaxLettersToShow arm in Gum/Wireframe/CustomSetPropertyOnRenderable.cs.
            // Skia honors this as a paint-only typewriter reveal on the renderable (see Text.Render /
            // Text.GetVisibleWrappedText); the assignment is otherwise identical. (issue #3678)
            // Redispatched onto TextRuntime (issue #3706/ADR 0010) for consistency with core.
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.MaxLettersToShow = (int?)value;
            }
            handled = true;
#endif
        }
        else if (propertyName == nameof(gueAsTextRuntime.LineHeightMultiplier))
        {
            // Issue #3706/ADR 0010: had no dispatch arm at all. Worked by accident via the
            // reflection fallback (SkiaGum.Renderables.Text.LineHeightMultiplier is `float`, an
            // exact type match), so this is a consistency/perf fix, not a behavior change.
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.LineHeightMultiplier = (float)value;
            }
            handled = true;
        }
        else if (propertyName == nameof(gueAsTextRuntime.MaxNumberOfLines))
        {
            // Issue #3706/ADR 0010: same as LineHeightMultiplier above -- worked by accident via
            // the reflection fallback (SkiaGum.Renderables.Text.MaxNumberOfLines is `int?`, an
            // exact type match).
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.MaxNumberOfLines = (int?)value;
            }
            handled = true;
        }

        else if (propertyName == nameof(TextOverflowHorizontalMode))
        {
            // Issue #3706/ADR 0010: this arm never set handled = true (the same incidental bug
            // ADR 0009 fixed in the core dispatcher's copy of this property), so every assignment
            // redundantly fell through to reflection afterward. Redispatched onto TextRuntime, which
            // already implements the identical enum-to-bool mapping this used to duplicate.
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.TextOverflowHorizontalMode = (TextOverflowHorizontalMode)value;
            }
            handled = true;
        }
        else if (propertyName == nameof(TextOverflowVerticalMode))
        {
            var textOverflowMode = (TextOverflowVerticalMode)value;
#if SKIA
            // Skia honors vertical overflow directly on the renderable: TruncateLine caps the
            // RichTextKit TextBlock to the Text's Height (see Text.GetTextBlock), SpillOver renders
            // unbounded. The XNALIKE copy (Gum/Wireframe/CustomSetPropertyOnRenderable.cs) instead
            // routes through GraphicalUiElement.RefreshTextOverflowVerticalMode. (issue #3677)
            text.TextOverflowVerticalMode = textOverflowMode;
            handled = true;
#endif

        }
        // Standalone drop shadow (issue #3674). Set directly on the renderable rather than through
        // ReactToFontValueChange, since these aren't font-cascade inputs. Mirrors the shape
        // drop-shadow arms in TrySetPropertiesOnRenderableBase.
        else if (propertyName == nameof(text.HasDropshadow))
        {
            text.HasDropshadow = (bool)value;
            handled = true;
        }
        else if (propertyName == nameof(text.DropshadowOffsetX))
        {
            text.DropshadowOffsetX = (float)value;
            handled = true;
        }
        else if (propertyName == nameof(text.DropshadowOffsetY))
        {
            text.DropshadowOffsetY = (float)value;
            handled = true;
        }
        else if (propertyName == nameof(text.DropshadowBlurX))
        {
            text.DropshadowBlurX = (float)value;
            handled = true;
        }
        else if (propertyName == nameof(text.DropshadowBlurY))
        {
            text.DropshadowBlurY = (float)value;
            handled = true;
        }
        else if (propertyName == nameof(text.DropshadowRed))
        {
            text.DropshadowRed = (int)value;
            handled = true;
        }
        else if (propertyName == nameof(text.DropshadowGreen))
        {
            text.DropshadowGreen = (int)value;
            handled = true;
        }
        else if (propertyName == nameof(text.DropshadowBlue))
        {
            text.DropshadowBlue = (int)value;
            handled = true;
        }
        else if (propertyName == nameof(text.DropshadowAlpha))
        {
            text.DropshadowAlpha = (int)value;
            handled = true;
        }

        return handled;
    }

#endif

}
