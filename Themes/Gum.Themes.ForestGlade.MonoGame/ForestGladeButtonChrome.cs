using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Shared chrome stack for Forest Glade Button and ToggleButton — they share
/// the same silhouette and visual layers, so the gradient fill, leaf border,
/// drop shadow, and focus ring all live here. Each visual owns its own state
/// callbacks and calls <see cref="Apply"/> with state-specific colors.
/// <para>
/// A glyph-shaped text drop shadow was attempted as a duplicate
/// <see cref="TextRuntime"/> mirrored each frame; it was removed because
/// <see cref="TextRuntime"/> property setters unconditionally call
/// <c>UpdateToFontValues</c>, so syncing four font properties per frame
/// re-triggered the KernSmith bake every frame and produced a flood of
/// <c>FontParsingException</c> output. The right fix is to bake the shadow
/// into the glyph atlas via KernSmith's <c>WithShadow</c> API; tracked in
/// issue #2724.
/// </para>
/// </summary>
internal sealed class ForestGladeButtonChrome
{
    public const float BorderThickness = 1f;
    public const float FocusRingThickness = 3f;
    public const float FocusRingInset = 3f;

    // CSS .fg-btn carries two stacked drop shadows: a dark offset-down depth
    // shadow at rest and a green halo on hover/focus. Apos.Shapes draws only
    // one shadow per shape, so the state callbacks pick whichever reads as
    // dominant for that state.
    public const float RestShadowOffsetY = 4f;
    public const float RestShadowBlur = 14f;
    public const float HoverGlowBlur = 24f;
    public const float PushedGlowBlur = 0f;

    public RoundedRectangleRuntime Fill { get; }
    public RoundedRectangleRuntime Border { get; }
    public RoundedRectangleRuntime FocusRing { get; }

    private readonly TextRuntime _textInstance;

    /// <summary>
    /// Builds the chrome layers, parents them to <paramref name="host"/> in
    /// render order (fill → border → focus ring), then re-parents
    /// <paramref name="textInstance"/> last so the live text paints on top.
    /// </summary>
    public ForestGladeButtonChrome(GraphicalUiElement host, TextRuntime textInstance)
    {
        _textInstance = textInstance;

        Fill = CreateFill();
        host.AddChild(Fill);

        Border = CreateBorder();
        host.AddChild(Border);

        FocusRing = CreateFocusRing();
        host.AddChild(FocusRing);

        textInstance.Parent = null;
        host.AddChild(textInstance);
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "ForestGladeButtonFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyLarge(fill);
        fill.IsFilled = true;

        // Vertical 2-stop linear gradient. Endpoints use PixelsFromSmall /
        // PixelsFromLarge (with value 0) rather than Percentage because the
        // Percentage branch in RenderableShapeBase.GetGradient currently
        // overwrites the world position with a local coord (issue #2723),
        // making a Percentage-based gradient render far from the button.
        fill.UseGradient = true;
        fill.GradientType = GradientType.Linear;
        fill.GradientX1Units = GeneralUnitType.PixelsFromMiddle;
        fill.GradientY1Units = GeneralUnitType.PixelsFromSmall;
        fill.GradientX1 = 0f;
        fill.GradientY1 = 0f;
        fill.GradientX2Units = GeneralUnitType.PixelsFromMiddle;
        fill.GradientY2Units = GeneralUnitType.PixelsFromLarge;
        fill.GradientX2 = 0f;
        fill.GradientY2 = 0f;

        fill.HasDropshadow = true;
        fill.DropshadowColor = ForestGladePalette.DarkShadow;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = RestShadowOffsetY;
        fill.DropshadowBlurX = RestShadowBlur;
        fill.DropshadowBlurY = RestShadowBlur;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "ForestGladeButtonBorder";
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyLarge(border);
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.Color = ForestGladeColors.Border;
        return border;
    }

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
        ring.Name = "ForestGladeButtonFocusRing";
        ring.XUnits = GeneralUnitType.PixelsFromMiddle;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Center;
        ring.YOrigin = VerticalAlignment.Center;
        ring.Width = FocusRingInset * 2f;
        ring.Height = FocusRingInset * 2f;
        ring.WidthUnits = DimensionUnitType.RelativeToParent;
        ring.HeightUnits = DimensionUnitType.RelativeToParent;
        ring.CornerRadius = 4f + FocusRingInset;
        ring.CustomRadiusTopLeft = 4f + FocusRingInset;
        ring.CustomRadiusTopRight = 18f + FocusRingInset;
        ring.CustomRadiusBottomRight = 4f + FocusRingInset;
        ring.CustomRadiusBottomLeft = 18f + FocusRingInset;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.Color = ForestGladeColors.SunPale * 0.45f;
        ring.Visible = false;
        return ring;
    }

    /// <summary>
    /// Apply a state's palette to the chrome layers. Shadow is drawn only
    /// when <paramref name="shadowBlur"/> &gt; 0 — pushed/disabled states
    /// pass 0 to suppress it entirely. <paramref name="textShadow"/> is
    /// accepted for source-compat with callers and currently unused (see
    /// the class-level comment about issue #2724).
    /// </summary>
    public void Apply(Color fillTop, Color fillBottom, Color border,
        Color shadow, float shadowOffsetY, float shadowBlur,
        Color text, Color textShadow, bool ring)
    {
        Fill.Color1 = fillTop;
        Fill.Color2 = fillBottom;
        Border.Color = border;
        Fill.DropshadowColor = shadow;
        Fill.DropshadowOffsetY = shadowOffsetY;
        Fill.DropshadowBlurX = shadowBlur;
        Fill.DropshadowBlurY = shadowBlur;
        Fill.HasDropshadow = shadowBlur > 0f;
        _textInstance.Color = text;
        _ = textShadow;
        FocusRing.Visible = ring;
    }
}
