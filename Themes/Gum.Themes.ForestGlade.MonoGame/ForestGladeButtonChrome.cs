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
/// drop shadow, focus ring, and 1 px text drop-shadow live here. Each visual
/// owns its own state callbacks and calls <see cref="Apply"/> with state-
/// specific colors. The on-frame text-shadow Text mirror is done by calling
/// <see cref="SyncTextShadow"/> from the host's <c>PreRender</c> override.
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
    public TextRuntime TextShadow { get; }

    private readonly TextRuntime _textInstance;

    /// <summary>
    /// Builds the four chrome layers, parents them to <paramref name="host"/>
    /// in render order (fill → border → focus ring → text shadow), then
    /// re-parents <paramref name="textInstance"/> last so the live text
    /// renders on top of its shadow.
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

        TextShadow = CreateTextShadow(textInstance);
        host.AddChild(TextShadow);

        // Re-parent the primary text last so it paints over the shadow.
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

    private static TextRuntime CreateTextShadow(TextRuntime primary)
    {
        // Duplicate text drawn 1 px down and 1 px right with a dark colour
        // and modest alpha — the cheapest text-shadow that reads as depth
        // against a saturated fill. Position/size/font are mirrored on every
        // frame by SyncTextShadow because the primary text's content can
        // change at runtime (data binding, edit-time text edits, etc).
        TextRuntime shadow = new TextRuntime();
        shadow.Name = "ForestGladeButtonTextShadow";
        shadow.XUnits = GeneralUnitType.PixelsFromMiddle;
        shadow.YUnits = GeneralUnitType.PixelsFromMiddle;
        shadow.XOrigin = HorizontalAlignment.Center;
        shadow.YOrigin = VerticalAlignment.Center;
        shadow.X = 1f;
        shadow.Y = 1f;
        shadow.Width = 0;
        shadow.Height = 0;
        shadow.WidthUnits = DimensionUnitType.RelativeToParent;
        shadow.HeightUnits = DimensionUnitType.RelativeToParent;
        shadow.HorizontalAlignment = primary.HorizontalAlignment;
        shadow.VerticalAlignment = primary.VerticalAlignment;
        shadow.Color = new Color(0, 0, 0, 130);
        return shadow;
    }

    /// <summary>
    /// Mirrors the primary text's content, font, and size onto the shadow
    /// each frame. Call from the host's PreRender override so live edits
    /// (data binding, programmatic Text changes) don't desync.
    /// </summary>
    public void SyncTextShadow()
    {
        if (TextShadow.Text != _textInstance.Text)
        {
            TextShadow.Text = _textInstance.Text;
        }
        TextShadow.Font = _textInstance.Font;
        TextShadow.FontSize = _textInstance.FontSize;
        TextShadow.UseCustomFont = _textInstance.UseCustomFont;
        TextShadow.CustomFontFile = _textInstance.CustomFontFile;
        TextShadow.HorizontalAlignment = _textInstance.HorizontalAlignment;
        TextShadow.VerticalAlignment = _textInstance.VerticalAlignment;
        TextShadow.Visible = _textInstance.Visible;
    }

    /// <summary>
    /// Apply a state's palette to the chrome layers. Shadow is drawn only
    /// when <paramref name="shadowBlur"/> &gt; 0 — pushed/disabled states
    /// pass 0 to suppress it entirely.
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
        TextShadow.Color = textShadow;
        FocusRing.Visible = ring;
    }
}
