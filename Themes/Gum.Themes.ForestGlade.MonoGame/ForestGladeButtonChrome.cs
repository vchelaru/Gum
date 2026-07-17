using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Shared chrome stack for Forest Glade Button and ToggleButton — they share
/// the same silhouette and visual layers, so the gradient fill, leaf border,
/// drop shadow, focus ring, and 1 px text drop shadow live here. Each visual
/// owns its own state callbacks and calls <see cref="Apply"/> with state-
/// specific colors. The text shadow's content/font/size are mirrored by
/// <see cref="SyncTextShadow"/> from the host's <c>PreRender</c> override;
/// it short-circuits when nothing changed because
/// <see cref="TextRuntime"/> font setters unconditionally rebuild the font
/// atlas and would otherwise spam KernSmith with rebakes every frame. A
/// proper glyph-baked shadow via KernSmith's <c>WithShadow</c> API is
/// tracked in issue #2724.
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

    public RectangleRuntime Fill { get; }
    public RectangleRuntime Border { get; }
    public RectangleRuntime FocusRing { get; }
    public TextRuntime TextShadow { get; }

    private readonly TextRuntime _textInstance;

    /// <summary>
    /// Builds the chrome layers, parents them to <paramref name="host"/> in
    /// render order (fill → border → focus ring → text shadow → primary
    /// text). The caller MUST have already configured <paramref name="textInstance"/>'s
    /// font (e.g. via ApplyState) before invoking this — the shadow's font
    /// is seeded from the primary up front so its first bake doesn't hit
    /// the TextRuntime Arial/18 default and throw a FontParsingException.
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

        textInstance.Parent = null;
        host.AddChild(textInstance);
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
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
        fill.StrokeWidth = 0;

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
        fill.DropshadowColor = ForestGladeStyling.ActiveStyle.Colors.DarkShadow;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = RestShadowOffsetY;
        fill.DropshadowBlur = RestShadowBlur;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
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
        border.StrokeColor = ForestGladeStyling.ActiveStyle.Colors.Border;
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        RectangleRuntime ring = new RectangleRuntime();
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
        // 0.45 scalar dim of SunPale, written channel-wise so it compiles on XNA and raylib (which
        // have no Color * float operator) and Skia (whose SKColor exposes Red/Green/Blue/Alpha
        // instead of R/G/B/A).
#if SKIA
        ring.StrokeColor = new Color(
            (byte)(ForestGladeStyling.ActiveStyle.Colors.SunPale.Red * 0.45f),
            (byte)(ForestGladeStyling.ActiveStyle.Colors.SunPale.Green * 0.45f),
            (byte)(ForestGladeStyling.ActiveStyle.Colors.SunPale.Blue * 0.45f),
            (byte)(ForestGladeStyling.ActiveStyle.Colors.SunPale.Alpha * 0.45f));
#else
        ring.StrokeColor = new Color(
            (int)(ForestGladeStyling.ActiveStyle.Colors.SunPale.R * 0.45f),
            (int)(ForestGladeStyling.ActiveStyle.Colors.SunPale.G * 0.45f),
            (int)(ForestGladeStyling.ActiveStyle.Colors.SunPale.B * 0.45f),
            (int)(ForestGladeStyling.ActiveStyle.Colors.SunPale.A * 0.45f));
#endif
        ring.Visible = false;
        return ring;
    }

    private static TextRuntime CreateTextShadow(TextRuntime primary)
    {
        // Seed font / size / file fields from the primary BEFORE adding to
        // the host. TextRuntime's ctor unconditionally assigns Arial-18,
        // which would trigger a FontParsingException on the first bake if
        // we left it. Caller contract: configure primary's font before
        // building the chrome.
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
        shadow.UseCustomFont = primary.UseCustomFont;
        shadow.CustomFontFile = primary.CustomFontFile;
        shadow.Font = primary.Font;
        shadow.FontSize = primary.FontSize;
        shadow.Text = primary.Text;
        shadow.Color = new Color(0, 0, 0, 130);
        return shadow;
    }

    /// <summary>
    /// Mirrors content / font / size from the primary to the shadow, but
    /// only when a value actually changed. <see cref="TextRuntime"/> font
    /// setters unconditionally call <c>UpdateToFontValues</c>, which
    /// rebuilds the KernSmith atlas — so a naive "write through every
    /// frame" sync produces a flood of <c>FontParsingException</c>s on
    /// any transitional state (and is expensive even when it succeeds).
    /// Call from the host's PreRender so live text edits / data binding
    /// stay in step.
    /// </summary>
    public void SyncTextShadow()
    {
        if (TextShadow.Text != _textInstance.Text)
        {
            TextShadow.Text = _textInstance.Text;
        }
        if (TextShadow.UseCustomFont != _textInstance.UseCustomFont)
        {
            TextShadow.UseCustomFont = _textInstance.UseCustomFont;
        }
        if (TextShadow.CustomFontFile != _textInstance.CustomFontFile)
        {
            TextShadow.CustomFontFile = _textInstance.CustomFontFile;
        }
        if (TextShadow.Font != _textInstance.Font)
        {
            TextShadow.Font = _textInstance.Font;
        }
        if (TextShadow.FontSize != _textInstance.FontSize)
        {
            TextShadow.FontSize = _textInstance.FontSize;
        }
        if (TextShadow.HorizontalAlignment != _textInstance.HorizontalAlignment)
        {
            TextShadow.HorizontalAlignment = _textInstance.HorizontalAlignment;
        }
        if (TextShadow.VerticalAlignment != _textInstance.VerticalAlignment)
        {
            TextShadow.VerticalAlignment = _textInstance.VerticalAlignment;
        }
        if (TextShadow.Visible != _textInstance.Visible)
        {
            TextShadow.Visible = _textInstance.Visible;
        }
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
        Fill.FillColor = fillTop;
        Fill.Color2 = fillBottom;
        Border.StrokeColor = border;
        Fill.DropshadowColor = shadow;
        Fill.DropshadowOffsetY = shadowOffsetY;
        Fill.DropshadowBlur = shadowBlur;
        Fill.HasDropshadow = shadowBlur > 0f;
        _textInstance.Color = text;
        TextShadow.Color = textShadow;
        FocusRing.Visible = ring;
    }
}
