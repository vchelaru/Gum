using Gum.DataTypes;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using BaseToggleButtonVisual = Gum.Forms.DefaultVisuals.V3.ToggleButtonVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade-styled ToggleButton. Shares the same chrome stack as
/// <see cref="ButtonVisual"/> (gradient fill, leaf border, drop shadow, focus
/// ring, text drop shadow) via <see cref="ForestGladeButtonChrome"/>. Off
/// states paint a muted moss canopy fill; On states paint the leaf-bright
/// accent gradient with dark text for contrast.
/// </summary>
public class ToggleButtonVisual : BaseToggleButtonVisual
{
    private readonly ForestGladeButtonChrome _chrome;

    public ToggleButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;

        Width = 96;
        Height = 32;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        // ApplyState BEFORE constructing the chrome so the text-shadow's
        // seeded font is the theme's font, not TextRuntime's Arial-18 default.
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);

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
        Color onText = new Color(5, 63, 31);
        Color onTextShadow = new Color(232, 255, 117, 90);
        Color pushedOnText = new Color(214, 245, 176);
        Color pushedOnTextShadow = new Color(0, 0, 0, 90);
        Color disabledBorder = new Color(232, 255, 117, 26);

        // -------- Off (muted moss canopy fill, sun-pale text) --------
        States.EnabledOff.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.Border,
            shadow: ForestGladeStyling.ActiveStyle.Colors.DarkShadow,
            shadowOffsetY: ForestGladeButtonChrome.RestShadowOffsetY,
            shadowBlur: ForestGladeButtonChrome.RestShadowBlur,
            text: ForestGladeStyling.ActiveStyle.Colors.Muted, textShadow: textShadow, ring: false);

        States.HighlightedOff.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.BorderHover,
            shadow: ForestGladeStyling.ActiveStyle.Colors.GlowMedium,
            shadowOffsetY: 0f, shadowBlur: ForestGladeButtonChrome.HoverGlowBlur,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, textShadow: textShadow, ring: false);

        States.PushedOff.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillBottom,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.BorderHover,
            shadow: ForestGladeStyling.ActiveStyle.Colors.DarkShadow,
            shadowOffsetY: 0f, shadowBlur: 0f,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, textShadow: textShadow, ring: false);

        States.FocusedOff.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.BorderHover,
            shadow: ForestGladeStyling.ActiveStyle.Colors.GlowStrong,
            shadowOffsetY: 0f, shadowBlur: ForestGladeButtonChrome.HoverGlowBlur,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, textShadow: textShadow, ring: true);

        States.HighlightedFocusedOff.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.BorderHover,
            shadow: ForestGladeStyling.ActiveStyle.Colors.GlowStrong,
            shadowOffsetY: 0f, shadowBlur: ForestGladeButtonChrome.HoverGlowBlur,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, textShadow: textShadow, ring: true);

        States.DisabledOff.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillBottom,
            border: disabledBorder,
            shadow: ForestGladeStyling.ActiveStyle.Colors.DarkShadow,
            shadowOffsetY: 0f, shadowBlur: 0f,
            text: ForestGladeStyling.ActiveStyle.Colors.Disabled, textShadow: textShadow, ring: false);

        States.DisabledFocusedOff.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillBottom,
            border: disabledBorder,
            shadow: ForestGladeStyling.ActiveStyle.Colors.DarkShadow,
            shadowOffsetY: 0f, shadowBlur: 0f,
            text: ForestGladeStyling.ActiveStyle.Colors.Disabled, textShadow: textShadow, ring: true);

        // -------- On (bright leaf-green gradient, dark text) --------
        States.EnabledOn.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonHoverFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonRestFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.SunPale,
            shadow: ForestGladeStyling.ActiveStyle.Colors.GlowMedium,
            shadowOffsetY: 0f, shadowBlur: ForestGladeButtonChrome.RestShadowBlur,
            text: onText, textShadow: onTextShadow, ring: false);

        States.HighlightedOn.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonHoverFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonHoverFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.SunPale,
            shadow: ForestGladeStyling.ActiveStyle.Colors.GlowStrong,
            shadowOffsetY: 0f, shadowBlur: ForestGladeButtonChrome.HoverGlowBlur,
            text: onText, textShadow: onTextShadow, ring: false);

        States.PushedOn.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonPushedFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonPushedFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.SunPale,
            shadow: ForestGladeStyling.ActiveStyle.Colors.DarkShadow,
            shadowOffsetY: 0f, shadowBlur: ForestGladeButtonChrome.PushedGlowBlur,
            text: pushedOnText, textShadow: pushedOnTextShadow, ring: false);

        States.FocusedOn.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonHoverFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonRestFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.SunPale,
            shadow: ForestGladeStyling.ActiveStyle.Colors.GlowStrong,
            shadowOffsetY: 0f, shadowBlur: ForestGladeButtonChrome.HoverGlowBlur,
            text: onText, textShadow: onTextShadow, ring: true);

        States.HighlightedFocusedOn.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonHoverFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonHoverFillBottom,
            border: ForestGladeStyling.ActiveStyle.Colors.SunPale,
            shadow: ForestGladeStyling.ActiveStyle.Colors.GlowStrong,
            shadowOffsetY: 0f, shadowBlur: ForestGladeButtonChrome.HoverGlowBlur,
            text: onText, textShadow: onTextShadow, ring: true);

        States.DisabledOn.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillBottom,
            border: disabledBorder,
            shadow: ForestGladeStyling.ActiveStyle.Colors.DarkShadow,
            shadowOffsetY: 0f, shadowBlur: 0f,
            text: ForestGladeStyling.ActiveStyle.Colors.Disabled, textShadow: textShadow, ring: false);

        States.DisabledFocusedOn.Apply = () => _chrome.Apply(
            fillTop: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillTop,
            fillBottom: ForestGladeStyling.ActiveStyle.Colors.ButtonDisabledFillBottom,
            border: disabledBorder,
            shadow: ForestGladeStyling.ActiveStyle.Colors.DarkShadow,
            shadowOffsetY: 0f, shadowBlur: 0f,
            text: ForestGladeStyling.ActiveStyle.Colors.Disabled, textShadow: textShadow, ring: true);
    }
}
