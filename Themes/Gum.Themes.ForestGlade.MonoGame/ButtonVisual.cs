using Gum.DataTypes;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using BaseButtonVisual = Gum.Forms.DefaultVisuals.V3.ButtonVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade-styled Button visual. Leaf-large per-corner radii silhouette
/// (sharp top-left/bottom-right, rounded top-right/bottom-left), saturated
/// canopy-green gradient fill, sun-pale tinted border, dark depth shadow at
/// rest swapped for a leaf-bright halo on hover/focus. The actual shape
/// stack and per-frame text-shadow sync live in <see cref="ForestGladeButtonChrome"/>
/// so <see cref="ToggleButtonVisual"/> can reuse the same layers.
/// </summary>
public class ButtonVisual : BaseButtonVisual
{
    private readonly ForestGladeButtonChrome _chrome;

    public ButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;

        Width = 120;
        Height = 32;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        // ApplyState BEFORE constructing the chrome so TextInstance.Font /
        // FontSize are already set to the theme's font when the chrome seeds
        // the text-shadow's font fields. Otherwise the shadow's first bake
        // would hit TextRuntime's Arial-18 default and throw.
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);
        TextInstance.Color = ForestGladeStyling.ActiveStyle.Colors.Text;

        _chrome = new ForestGladeButtonChrome(this, TextInstance);

        WireStates();
    }

    public override void PreRender()
    {
        base.PreRender();
        _chrome.SyncTextShadow();
    }

    private void WireStates()
    {
        Color textShadow = new Color(0, 0, 0, 130);
        Color pushedText = new Color(214, 245, 176);
        Color pushedTextShadow = new Color(0, 0, 0, 90);
        Color disabledTextShadow = new Color(0, 0, 0, 60);

        States.Enabled.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonRestFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonRestFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.Border,
            shadow: ForestGladeStyling.ActiveStyle.Colors.DarkShadow,
            shadowOffsetY: ForestGladeButtonChrome.RestShadowOffsetY,
            shadowBlur: ForestGladeButtonChrome.RestShadowBlur,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, textShadow: textShadow, ring: false);

        States.Highlighted.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonHoverFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonHoverFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.BorderHover,
            shadow: ForestGladeStyling.ActiveStyle.Colors.GlowStrong,
            shadowOffsetY: 0f, shadowBlur: ForestGladeButtonChrome.HoverGlowBlur,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, textShadow: textShadow, ring: false);

        States.Focused.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonRestFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonRestFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.BorderHover,
            shadow: ForestGladeStyling.ActiveStyle.Colors.GlowStrong,
            shadowOffsetY: 0f, shadowBlur: ForestGladeButtonChrome.HoverGlowBlur,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, textShadow: textShadow, ring: true);

        States.HighlightedFocused.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonHoverFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonHoverFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.BorderHover,
            shadow: ForestGladeStyling.ActiveStyle.Colors.GlowStrong,
            shadowOffsetY: 0f, shadowBlur: ForestGladeButtonChrome.HoverGlowBlur,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, textShadow: textShadow, ring: true);

        States.Pushed.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonPushedFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonPushedFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.Border,
            shadow: ForestGladeStyling.ActiveStyle.Colors.DarkShadow,
            shadowOffsetY: 0f, shadowBlur: ForestGladeButtonChrome.PushedGlowBlur,
            text: pushedText, textShadow: pushedTextShadow, ring: false);

        States.Disabled.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillBottom,
            border: new Color(232, 255, 117, 26),
            shadow: ForestGladeStyling.ActiveStyle.Colors.DarkShadow,
            shadowOffsetY: 0f, shadowBlur: 0f,
            text: ForestGladeStyling.ActiveStyle.Colors.Disabled, textShadow: disabledTextShadow, ring: false);

        States.DisabledFocused.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillBottom,
            border: new Color(232, 255, 117, 26),
            shadow: ForestGladeStyling.ActiveStyle.Colors.DarkShadow,
            shadowOffsetY: 0f, shadowBlur: 0f,
            text: ForestGladeStyling.ActiveStyle.Colors.Disabled, textShadow: disabledTextShadow, ring: true);
    }
}
