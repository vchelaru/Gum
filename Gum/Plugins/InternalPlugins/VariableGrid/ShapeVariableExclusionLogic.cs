using Gum.DataTypes;
using RenderingLibrary.Graphics;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Pure decision logic for hiding shape variables (gradient / dropshadow / stroke / fill
/// channels) in the variable grid for Circle / Rectangle / ColoredCircle / RoundedRectangle /
/// Arc / Line. Kept free of services (the caller supplies the resolved standard type name and
/// variable prefix) so it can be unit tested without a loaded project or selection state.
/// </summary>
internal class ShapeVariableExclusionLogic
{
    // Moved from ExclusionsPlugin (originally Gum/SvgPlugin/Managers/DefaultStateManager) — these
    // rules historically lived in MainSkiaPlugin because gradient/dropshadow/IsFilled were
    // Skia-only, but #2929/#2933/#2931 promoted them to plain Circle/Rectangle. The gating
    // is shape-agnostic now.
    public bool GetIfShapeVariableIsExcluded(string rootName, RecursiveVariableFinder finder,
        string? rootStandardTypeName, string prefix, out bool shouldExclude)
    {
        // Issue #3009 — Arc's gradient start is now its primary Color; the Red1/Green1/Blue1/Alpha1
        // surface is kept only as obsolete back-compat shims (mapping onto Color), so hide all four
        // from Arc's grid entirely, leaving Arc a single primary Color. Circle/Rectangle don't have
        // these variables at all (StandardElementsManager omits them); the legacy ColoredCircle/
        // RoundedRectangle keep their standalone Color1, gated by UseGradient in the branch below.
        if (rootStandardTypeName == "Arc" &&
            (rootName == "Red1" || rootName == "Green1" || rootName == "Blue1" || rootName == "Alpha1"))
        {
            shouldExclude = true;
            return true;
        }

        if (rootName == "Red" || rootName == "Green" || rootName == "Blue")
        {
            var usesGradients = finder.GetValue(prefix + "UseGradient");
            if (usesGradients is bool asBool && asBool)
            {
                shouldExclude = true;
                return true;
            }
        }
        else if (rootName == "Red1" || rootName == "Green1" || rootName == "Blue1" || rootName == "Alpha1" ||
            rootName == "GradientX1" || rootName == "GradientY1" ||
            rootName == "GradientX1Units" || rootName == "GradientY1Units" ||
            rootName == "Red2" || rootName == "Green2" || rootName == "Blue2" || rootName == "Alpha2" ||
            rootName == "GradientType")
        {
            var usesGradients = finder.GetValue(prefix + "UseGradient");
            var effectiveUsesGradient = usesGradients is bool asBool && asBool;
            shouldExclude = !effectiveUsesGradient;
            return true;
        }
        else if (rootName == "GradientX2" || rootName == "GradientY2" || rootName == "GradientX2Units" || rootName == "GradientY2Units")
        {
            var usesGradients = finder.GetValue(prefix + "UseGradient");
            var effectiveUsesGradient = usesGradients is bool asBool && asBool;

            var gradientTypeAsObject = finder.GetValue(prefix + "GradientType");
            GradientType? gradientType = gradientTypeAsObject as GradientType?;

            shouldExclude = effectiveUsesGradient == false || gradientType != GradientType.Linear;
            return true;
        }
        else if (rootName == "GradientInnerRadius" || rootName == "GradientOuterRadius" ||
            rootName == "GradientInnerRadiusUnits" || rootName == "GradientOuterRadiusUnits")
        {
            var usesGradients = finder.GetValue(prefix + "UseGradient");
            var effectiveUsesGradient = usesGradients is bool asBool && asBool;

            var gradientTypeAsObject = finder.GetValue(prefix + "GradientType");
            GradientType? gradientType = gradientTypeAsObject as GradientType?;

            shouldExclude = effectiveUsesGradient == false || gradientType != GradientType.Radial;
            return true;
        }

        if (rootName == "DropshadowOffsetX" || rootName == "DropshadowOffsetY" || rootName == "DropshadowBlur" ||
            rootName == "DropshadowAlpha" || rootName == "DropshadowRed" || rootName == "DropshadowGreen" || rootName == "DropshadowBlue")
        {
            var hasDropshadow = finder.GetValue(prefix + "HasDropshadow");
            var effectiveHasDropshadow = hasDropshadow is bool asBool && asBool;
            shouldExclude = !effectiveHasDropshadow;
            return true;
        }

        // #2931 / #2938: stroke vs. fill model is type-specific.
        //
        // Legacy shapes (ColoredCircle / RoundedRectangle / Arc) treat IsFilled as
        // "fill OR stroke" — when IsFilled is true the stroke vars are meaningless and
        // hidden. Plain Circle / Rectangle (#2938) expose fill and stroke as independent
        // surfaces; stroke vars stay visible regardless of IsFilled, gated only by
        // StrokeWidth = 0.
        //
        // Symmetric on the fill side: the channel-decomp FillRed/Green/Blue/Alpha exist
        // only on plain Circle / Rectangle (#2931) and are meaningless when IsFilled is
        // false.
        var hasSeparateFillAndStroke = rootStandardTypeName == "Circle" || rootStandardTypeName == "Rectangle";

        if (rootName == "StrokeWidth" || rootName == "StrokeDashLength" || rootName == "StrokeGapLength")
        {
            if (!hasSeparateFillAndStroke)
            {
                var isFilled = finder.GetValue(prefix + "IsFilled");
                if (isFilled is true)
                {
                    shouldExclude = true;
                    return true;
                }
            }

            // StrokeWidth <= 0 draws no stroke (rendering gates on StrokeWidth > 0), so the
            // dash pattern is meaningless. StrokeWidth itself stays visible so the user can
            // re-enable the stroke.
            if (rootName != "StrokeWidth" && IsStrokeAbsent(finder, prefix))
            {
                shouldExclude = true;
                return true;
            }
        }

        if (hasSeparateFillAndStroke &&
            (rootName == "StrokeRed" || rootName == "StrokeGreen" || rootName == "StrokeBlue" || rootName == "StrokeAlpha"))
        {
            // No stroke drawn means its color channels are meaningless.
            shouldExclude = IsStrokeAbsent(finder, prefix);
            return true;
        }

        if (hasSeparateFillAndStroke &&
            (rootName == "FillRed" || rootName == "FillGreen" || rootName == "FillBlue" || rootName == "FillAlpha"))
        {
            // Hidden when IsFilled = false (no fill drawn) or when UseGradient = true
            // (gradient paints the fill slot, the solid fill channels are unused). Stroke
            // channels stay visible regardless of UseGradient — gradient targets fill only.
            var isFilled = finder.GetValue(prefix + "IsFilled");
            if (isFilled is false)
            {
                shouldExclude = true;
                return true;
            }
            var usesGradient = finder.GetValue(prefix + "UseGradient");
            if (usesGradient is true)
            {
                shouldExclude = true;
                return true;
            }
            shouldExclude = false;
            return true;
        }

        shouldExclude = false;
        return false;
    }

    private bool IsStrokeAbsent(RecursiveVariableFinder finder, string prefix)
    {
        var strokeWidth = finder.GetValue(prefix + "StrokeWidth");
        return strokeWidth is float asFloat && asFloat <= 0f;
    }
}
