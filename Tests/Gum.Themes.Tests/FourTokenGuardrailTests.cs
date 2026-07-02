using System.Reflection;
using Gum.Themes.Bubblegum;
using Gum.Themes.DarkPro;
using Gum.Themes.Editor;
using Gum.Themes.ForestGlade;
using Gum.Themes.Retro95;
using Microsoft.Xna.Framework;
using Shouldly;
using V3Styling = Gum.Forms.DefaultVisuals.V3.Styling;

namespace Gum.Themes.Tests;

/// <summary>
/// Every shipped theme's <c>XyzColors</c> must expose TextPrimary/TextMuted/Primary/Accent,
/// because every theme's Apply()/ConfigureStyling() pushes exactly these 4 tokens into
/// <see cref="V3Styling.ActiveStyle"/>'s Colors for the stock, un-subclassed V3 visuals
/// (e.g. Label) a theme leaves in place. This is the one shared contract across themes —
/// see the "4-token guardrail" section of the theme styling design.
/// </summary>
public class FourTokenGuardrailTests
{
    public static IEnumerable<object[]> ThemeColorsTypes()
    {
        yield return new object[] { typeof(DarkProColors), "DarkProColors" };
        yield return new object[] { typeof(BubblegumColors), "BubblegumColors" };
        yield return new object[] { typeof(EditorColors), "EditorColors" };
        yield return new object[] { typeof(Retro95Colors), "Retro95Colors" };
        yield return new object[] { typeof(ForestGladeColors), "ForestGladeColors" };
    }

    [Theory]
    [MemberData(nameof(ThemeColorsTypes))]
    public void ColorsType_ExposesFourGuardrailTokensAsColorProperties(Type colorsType, string themeName)
    {
        string[] requiredTokenNames = { "TextPrimary", "TextMuted", "Primary", "Accent" };

        foreach (string tokenName in requiredTokenNames)
        {
            PropertyInfo? property = colorsType.GetProperty(tokenName, BindingFlags.Public | BindingFlags.Instance);

            property.ShouldNotBeNull($"{themeName} is missing the guardrail token '{tokenName}'.");
            property!.PropertyType.ShouldBe(typeof(Color),
                $"{themeName}.{tokenName} should be a Color-typed property.");
        }
    }

    [Fact]
    public void DarkProTheme_ConfigureStyling_SyncsFourGuardrailTokensIntoV3Styling()
    {
        Color textPrimary = new Color(11, 22, 33);
        Color textMuted = new Color(44, 55, 66);
        Color primary = new Color(77, 88, 99);
        Color accent = new Color(100, 110, 120);

        DarkProStyling.ActiveStyle.Colors.Text = textPrimary;
        DarkProStyling.ActiveStyle.Colors.Muted = textMuted;
        DarkProStyling.ActiveStyle.Colors.Surface1 = primary;
        DarkProStyling.ActiveStyle.Colors.Accent = accent;

        DarkProTheme.ConfigureStyling();

        V3Styling.ActiveStyle.Colors.TextPrimary.ShouldBe(textPrimary);
        V3Styling.ActiveStyle.Colors.TextMuted.ShouldBe(textMuted);
        V3Styling.ActiveStyle.Colors.Primary.ShouldBe(primary);
        V3Styling.ActiveStyle.Colors.Accent.ShouldBe(accent);
    }

    [Fact]
    public void BubblegumTheme_ConfigureStyling_SyncsFourGuardrailTokensIntoV3Styling()
    {
        Color textPrimary = new Color(12, 23, 34);
        Color textMuted = new Color(45, 56, 67);
        Color primary = new Color(78, 89, 100);
        Color accent = new Color(101, 111, 121);

        BubblegumStyling.ActiveStyle.Colors.Text = textPrimary;
        BubblegumStyling.ActiveStyle.Colors.Muted = textMuted;
        BubblegumStyling.ActiveStyle.Colors.Surface1 = primary;
        BubblegumStyling.ActiveStyle.Colors.Accent = accent;

        BubblegumTheme.ConfigureStyling();

        V3Styling.ActiveStyle.Colors.TextPrimary.ShouldBe(textPrimary);
        V3Styling.ActiveStyle.Colors.TextMuted.ShouldBe(textMuted);
        V3Styling.ActiveStyle.Colors.Primary.ShouldBe(primary);
        V3Styling.ActiveStyle.Colors.Accent.ShouldBe(accent);
    }

    [Fact]
    public void EditorTheme_ConfigureStyling_SyncsFourGuardrailTokensIntoV3Styling()
    {
        Color textPrimary = new Color(13, 24, 35);
        Color textMuted = new Color(46, 57, 68);
        Color primary = new Color(79, 90, 101);
        Color accent = new Color(102, 112, 122);

        EditorStyling.ActiveStyle.Colors.TextPrimary = textPrimary;
        EditorStyling.ActiveStyle.Colors.TextMuted = textMuted;
        EditorStyling.ActiveStyle.Colors.Primary = primary;
        EditorStyling.ActiveStyle.Colors.Accent = accent;

        EditorTheme.ConfigureStyling();

        V3Styling.ActiveStyle.Colors.TextPrimary.ShouldBe(textPrimary);
        V3Styling.ActiveStyle.Colors.TextMuted.ShouldBe(textMuted);
        V3Styling.ActiveStyle.Colors.Primary.ShouldBe(primary);
        V3Styling.ActiveStyle.Colors.Accent.ShouldBe(accent);
    }

    [Fact]
    public void Retro95Theme_ConfigureStyling_SyncsFourGuardrailTokensIntoV3Styling()
    {
        Color textPrimary = new Color(14, 25, 36);
        Color textMuted = new Color(47, 58, 69);
        Color primary = new Color(80, 91, 102);
        Color accent = new Color(103, 113, 123);

        Retro95Styling.ActiveStyle.Colors.Text = textPrimary;
        Retro95Styling.ActiveStyle.Colors.DisabledText = textMuted;
        Retro95Styling.ActiveStyle.Colors.Surface = primary;
        Retro95Styling.ActiveStyle.Colors.Selection = accent;

        Retro95Theme.ConfigureStyling();

        V3Styling.ActiveStyle.Colors.TextPrimary.ShouldBe(textPrimary);
        V3Styling.ActiveStyle.Colors.TextMuted.ShouldBe(textMuted);
        V3Styling.ActiveStyle.Colors.Primary.ShouldBe(primary);
        V3Styling.ActiveStyle.Colors.Accent.ShouldBe(accent);
    }

    [Fact]
    public void ForestGladeTheme_ConfigureStyling_SyncsFourGuardrailTokensIntoV3Styling()
    {
        Color textPrimary = new Color(15, 26, 37);
        Color textMuted = new Color(48, 59, 70);
        Color primary = new Color(81, 92, 103);
        Color accent = new Color(104, 114, 124);

        ForestGladeStyling.ActiveStyle.Colors.Text = textPrimary;
        ForestGladeStyling.ActiveStyle.Colors.Muted = textMuted;
        ForestGladeStyling.ActiveStyle.Colors.CanopyDeep = primary;
        ForestGladeStyling.ActiveStyle.Colors.LeafBright = accent;

        ForestGladeTheme.ConfigureStyling();

        V3Styling.ActiveStyle.Colors.TextPrimary.ShouldBe(textPrimary);
        V3Styling.ActiveStyle.Colors.TextMuted.ShouldBe(textMuted);
        V3Styling.ActiveStyle.Colors.Primary.ShouldBe(primary);
        V3Styling.ActiveStyle.Colors.Accent.ShouldBe(accent);
    }
}
