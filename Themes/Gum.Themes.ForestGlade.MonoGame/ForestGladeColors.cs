#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Centralized color tokens for the Forest Glade theme. Values mirror the
/// CSS custom properties in the source mockup (forest-glade.css <c>.fg</c>
/// palette).
/// </summary>
public static class ForestGladeColors
{
    /// <summary>Deep shadow under the trees (<c>--canopy-deep</c>, <c>#053239</c>). Page background.</summary>
    public static readonly Color CanopyDeep = new Color(5, 50, 57);

    /// <summary>Mossy mid-ground green (<c>--canopy-mid</c>, <c>#005f41</c>).</summary>
    public static readonly Color CanopyMid = new Color(0, 95, 65);

    /// <summary>Sunlit leaves green (<c>--canopy-lit</c>, <c>#08b23b</c>). Mid-tone in Button gradients.</summary>
    public static readonly Color CanopyLit = new Color(8, 178, 59);

    /// <summary>Vibrant new growth (<c>--leaf-bright</c>, <c>#47f641</c>). Accent color; focus ring fill, slider track fill.</summary>
    public static readonly Color LeafBright = new Color(71, 246, 65);

    /// <summary>High sun pollen (<c>--sun-pale</c>, <c>#e8ff75</c>). Border tint base, caret, highlight ticks.</summary>
    public static readonly Color SunPale = new Color(232, 255, 117);

    /// <summary>Late-afternoon gold (<c>--sun-warm</c>, <c>#ecab11</c>). Optional warm accent.</summary>
    public static readonly Color SunWarm = new Color(236, 171, 17);

    /// <summary>Dappled warm light (<c>--sun-glow</c>, <c>#fbbe82</c>). Secondary light-beam tint.</summary>
    public static readonly Color SunGlow = new Color(251, 190, 130);

    /// <summary>Tree bark dark (<c>--bark</c>, <c>#461c14</c>). Disabled-well fill; Window border.</summary>
    public static readonly Color Bark = new Color(70, 28, 20);

    /// <summary>Wood midtone (<c>--bark-soft</c>, <c>#8a4926</c>). Window title-bar gradient mid stop.</summary>
    public static readonly Color BarkSoft = new Color(138, 73, 38);

    /// <summary>Wildflower pink (<c>--petal</c>, <c>#f78d8d</c>). Wax-seal close button on Window.</summary>
    public static readonly Color Petal = new Color(247, 141, 141);

    /// <summary>Primary text — pale leaf-white (<c>--txt</c>, <c>#f1fff0</c>).</summary>
    public static readonly Color Text = new Color(241, 255, 240);

    /// <summary>Muted / secondary text (<c>--mu</c>, <c>#9bbaa3</c>) — desaturated sage.</summary>
    public static readonly Color Muted = new Color(155, 186, 163);

    /// <summary>Disabled text / fills (<c>--dis</c>, <c>#4a6a58</c>) — moss undergrowth.</summary>
    public static readonly Color Disabled = new Color(74, 106, 88);

    /// <summary>Placeholder text (<c>--ph</c>, <c>#7d9c87</c>).</summary>
    public static readonly Color Placeholder = new Color(125, 156, 135);

    /// <summary>Default border tint — <c>rgba(232,255,117,.18)</c>, sun-pale at 18% alpha.</summary>
    public static readonly Color Border = new Color(232, 255, 117, 46);

    /// <summary>Hover border tint — <c>rgba(232,255,117,.42)</c>, sun-pale at 42% alpha.</summary>
    public static readonly Color BorderHover = new Color(232, 255, 117, 107);

    /// <summary>Translucent accent fill — <c>rgba(71,246,65,.18)</c>, leaf-bright at 18% alpha. Selected rows, pushed states.</summary>
    public static readonly Color AccentDim = new Color(71, 246, 65, 46);

    /// <summary>Translucent accent halo — <c>rgba(71,246,65,.30)</c>, leaf-bright at 30%. Focus ring around inputs.</summary>
    public static readonly Color AccentHalo = new Color(71, 246, 65, 76);

    /// <summary>Pure white — used for pressed-state text on a few controls.</summary>
    public static readonly Color White = Color.White;
}
